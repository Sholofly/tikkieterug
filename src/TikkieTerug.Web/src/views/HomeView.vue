<template>
  <div class="page-header">
    <h1>Wedstrijden</h1>
  </div>

  <!-- Day tabs: today + the coming days that have matches -->
  <div v-if="dayTabs.length > 1" class="day-scroll">
    <button
      v-for="tab in dayTabs"
      :key="tab.date"
      class="day-pill"
      :class="{ active: activeDate === tab.date }"
      @click="selectedDate = tab.date"
    >
      {{ tab.label }}
    </button>
  </div>

  <div class="content">
    <div v-if="loading" class="loading" style="margin-top: 16px;">Laden...</div>

    <template v-else>
      <!-- Followed clubs first -->
      <div v-if="dayClubMatches.length > 0" style="margin-top: 16px;">
        <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;">Mijn clubs — {{ activeDayName }}</div>
        <div class="card">
          <MatchRow v-for="match in dayClubMatches" :key="match.matchId" :match="match" show-competition />
        </div>
      </div>

      <!-- Then per followed competition -->
      <div v-for="group in dayCompetitionGroups" :key="group.compId" style="margin-top: 16px;">
        <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;">
          <router-link :to="`/competition/${group.compId}`" style="text-decoration: none; color: inherit;">{{ group.compName }}</router-link> — {{ activeDayName }}
        </div>
        <div class="card">
          <MatchRow v-for="match in group.matches" :key="match.matchId" :match="match" />
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="dayClubMatches.length === 0 && dayCompetitionGroups.length === 0" style="margin-top: 16px;">
        <div class="card text-muted text-sm" style="text-align: center;">
          Geen wedstrijden {{ activeDayName }}.
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useApi } from '../composables/useApi'
import { useFavoritesStore } from '../stores/favorites'
import MatchRow from '../components/MatchRow.vue'

const api = useApi()
const favoritesStore = useFavoritesStore()

const loading = ref(false)
const allMatches = ref([])
let refreshInterval = null

const UPCOMING_DAYS = 7

// Local (not UTC) dates, matching the yyyy-MM-dd dates the API returns
function localDate(d) {
  const pad = n => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

const today = localDate(new Date())
const maxDate = (() => {
  const d = new Date()
  d.setDate(d.getDate() + UPCOMING_DAYS)
  return localDate(d)
})()

const tomorrow = (() => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  return localDate(d)
})()

function dayLabel(date, short) {
  if (date === today) return 'Vandaag'
  if (date === tomorrow) return 'Morgen'
  const d = new Date(`${date}T00:00:00`)
  return short
    ? d.toLocaleDateString('nl-NL', { weekday: 'short', day: 'numeric', month: 'short' })
    : d.toLocaleDateString('nl-NL', { weekday: 'long', day: 'numeric', month: 'long' })
}

const selectedDate = ref(today)

// Sort: live first, halftime, scheduled, ended last
const statusOrder = { live: 0, halftime: 1, scheduled: 2, ended: 3 }

function sortMatches(matches) {
  return [...matches].sort((a, b) => {
    const oa = statusOrder[a.status] ?? 9
    const ob = statusOrder[b.status] ?? 9
    if (oa !== ob) return oa - ob
    return a.time.localeCompare(b.time)
  })
}

// Today plus the coming days that have matches
const dayTabs = computed(() => {
  const upcoming = [...new Set(allMatches.value.map(m => m._date).filter(d => d > today))].sort()
  return [
    { date: today, label: 'Vandaag' },
    ...upcoming.map(date => ({ date, label: dayLabel(date, true) })),
  ]
})

// Fall back to today when the selected day no longer has matches after a refresh
const activeDate = computed(() =>
  dayTabs.value.some(t => t.date === selectedDate.value) ? selectedDate.value : today
)

// Used in headings: "vandaag", "morgen" or "zaterdag 21 november"
const activeDayName = computed(() => {
  if (activeDate.value === today) return 'vandaag'
  if (activeDate.value === tomorrow) return 'morgen'
  return dayLabel(activeDate.value, false)
})

const dayMatches = computed(() => allMatches.value.filter(m => m._date === activeDate.value))

// The selected day's matches for followed clubs
const dayClubMatches = computed(() => {
  const favClubIds = new Set(favoritesStore.clubs.map(c => c.id))
  return sortMatches(
    dayMatches.value.filter(m => favClubIds.has(m.homeClubId) || favClubIds.has(m.awayClubId))
  )
})

// The selected day's matches grouped per followed competition
const dayCompetitionGroups = computed(() => {
  const favComps = favoritesStore.competitions
  if (favComps.length === 0) return []
  const groups = []
  for (const comp of favComps) {
    const matches = sortMatches(
      dayMatches.value.filter(m => m._compId === comp.id)
    )
    if (matches.length > 0) {
      groups.push({
        compId: comp.id,
        compName: matches[0]._compName || comp.name || `Competitie ${comp.id}`,
        matches,
      })
    }
  }
  return groups
})

async function fetchDashboardData() {
  if (!favoritesStore.hasFavorites()) return

  loading.value = allMatches.value.length === 0
  try {
    const favClubs = favoritesStore.clubs
    const favComps = favoritesStore.competitions

    // Collect all competition IDs we need names for
    const allCompIds = new Set()
    for (const club of favClubs) { if (club.competitionId) allCompIds.add(club.competitionId) }
    for (const comp of favComps) { allCompIds.add(comp.id) }
    const compIdArr = [...allCompIds]

    // Fetch in parallel:
    // 1. Per favorite club: team_programma1 (has live status)
    // 2. Per favorite competition: uitslagen + programma (for today's matches)
    // 3. Competition names
    const favCompIds = favComps.map(c => c.id)
    const [clubProgrammaResults, clubUitslagenResults, compUitslResults, compProgrammaResults, nameResults] = await Promise.all([
      Promise.all(favClubs.map(club =>
        api.getClubProgramma(club.id)
          .then(data => ({ clubId: club.id, compId: club.competitionId, data }))
          .catch(() => ({ clubId: club.id, compId: club.competitionId, data: [] }))
      )),
      Promise.all(favClubs.map(club =>
        api.getClubUitslagen(club.id)
          .then(data => ({ clubId: club.id, compId: club.competitionId, data }))
          .catch(() => ({ clubId: club.id, compId: club.competitionId, data: [] }))
      )),
      Promise.all(favCompIds.map(id => api.getResults(id).catch(() => []))),
      Promise.all(favComps.map(comp =>
        api.getFixtures(comp.id).then(data => ({ compId: comp.id, data })).catch(() => ({ compId: comp.id, data: [] }))
      )),
      Promise.all(compIdArr.map(id =>
        api.getCompetitionName(id).then(res => ({ id, name: res.name })).catch(() => ({ id, name: null }))
      )),
    ])

    // Build competition name lookup
    const compNames = {}
    for (const { id, name } of nameResults) {
      if (name) compNames[id] = name
    }

    const seen = new Set()
    const todayList = []

    // 1. Process club programma (team_programma1) — has real-time status
    for (const { compId, data } of clubProgrammaResults) {
      if (!Array.isArray(data)) continue
      for (const group of data) {
        if (group.date < today || group.date > maxDate) continue
        for (const match of (group.matches || [])) {
          if (seen.has(match.matchId)) continue
          seen.add(match.matchId)
          match._compId = match.competitionId || compId
          match._compName = compNames[match._compId] || null
          match._date = group.date
          todayList.push(match)
        }
      }
    }

    // 1b. Process club uitslagen (team_uitslagen1) — today's ended matches
    for (const { compId, data } of clubUitslagenResults) {
      if (!Array.isArray(data)) continue
      for (const group of data) {
        if (group.date < today || group.date > maxDate) continue
        for (const match of (group.matches || [])) {
          if (seen.has(match.matchId)) continue
          seen.add(match.matchId)
          match._compId = match.competitionId || compId
          match._compName = compNames[match._compId] || null
          match._date = group.date
          todayList.push(match)
        }
      }
    }

    // 2. Process competition uitslagen (only for explicitly favorited competitions)
    for (let i = 0; i < compUitslResults.length; i++) {
      const grouped = compUitslResults[i]
      if (!Array.isArray(grouped)) continue
      for (const group of grouped) {
        if (group.date < today || group.date > maxDate) continue
        for (const match of (group.matches || [])) {
          if (seen.has(match.matchId)) continue
          seen.add(match.matchId)
          match._compId = favCompIds[i]
          match._compName = compNames[favCompIds[i]] || null
          match._date = group.date
          todayList.push(match)
        }
      }
    }

    // 3. Process competition programma (for today's matches not already seen)
    for (const { compId, data } of compProgrammaResults) {
      if (!Array.isArray(data)) continue
      for (const group of data) {
        if (group.date < today || group.date > maxDate) continue
        for (const match of (group.matches || [])) {
          if (seen.has(match.matchId)) continue
          seen.add(match.matchId)
          match._compId = compId
          match._compName = compNames[compId] || null
          match._date = group.date
          todayList.push(match)
        }
      }
    }

    // 4. Enrich today's non-ended matches with real-time data (future days stay as scheduled)
    const toEnrich = todayList.filter(m => m._date === today && m.status !== 'ended')
    if (toEnrich.length > 0) {
      const enriched = await Promise.all(
        toEnrich.map(m =>
          api.getMatch(m.matchId, m.source)
            .then(detail => ({ matchId: m.matchId, status: detail.status, homeScore: detail.homeScore, awayScore: detail.awayScore, homeRedCards: detail.homeRedCards, awayRedCards: detail.awayRedCards }))
            .catch(() => null)
        )
      )
      for (const detail of enriched) {
        if (!detail) continue
        const match = todayList.find(m => m.matchId === detail.matchId)
        if (match) {
          match.status = detail.status
          match.homeScore = detail.homeScore
          match.awayScore = detail.awayScore
          match.homeRedCards = detail.homeRedCards
          match.awayRedCards = detail.awayRedCards
        }
      }
    }

    allMatches.value = todayList
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchDashboardData()
  refreshInterval = setInterval(fetchDashboardData, 60_000)
})

onUnmounted(() => {
  clearInterval(refreshInterval)
})
</script>

<style scoped>
.day-scroll {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding: 12px 16px 6px;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
  max-width: 100%;
}

.day-scroll::-webkit-scrollbar {
  display: none;
}

/* Touch-friendly: at least 44px tall, readable text */
.day-pill {
  min-height: 44px;
  min-width: 72px;
  padding: 10px 18px;
  border-radius: 999px;
  font-size: 0.95rem;
  font-weight: 600;
  touch-action: manipulation;
  -webkit-tap-highlight-color: transparent;
  white-space: nowrap;
  cursor: pointer;
  border: 1px solid var(--border);
  background: var(--bg-card);
  color: var(--text-secondary);
  transition: background 0.15s, color 0.15s, border-color 0.15s;
  flex-shrink: 0;
}

.day-pill.active {
  background: var(--accent);
  color: var(--bg-primary);
  border-color: var(--accent);
}
</style>
