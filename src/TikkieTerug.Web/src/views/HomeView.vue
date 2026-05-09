<template>
  <div class="page-header">
    <h1>Wedstrijden</h1>
  </div>

  <div class="content">
      <div v-if="loading" class="loading" style="margin-top: 16px;">Laden...</div>

      <template v-else>
        <!-- Today's matches: followed clubs -->
        <div v-if="todayClubMatches.length > 0" style="margin-top: 16px;">
          <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;">Mijn clubs — vandaag</div>
          <div class="card">
            <div
              v-for="match in todayClubMatches"
              :key="match.matchId"
              class="fixture-row-wrap"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="fixture-row" style="cursor: pointer;">
                <div class="fixture-home">
                  <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
                  <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
                </div>
                <div class="fixture-center">
                  <span v-if="match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                  <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                  <span
                    v-if="match.status !== 'scheduled'"
                    class="badge"
                    :class="{
                      'badge-live': match.status === 'live',
                      'badge-halftime': match.status === 'halftime',
                      'badge-ended': match.status === 'ended',
                    }"
                  >{{ statusLabel(match.status) }}</span>
                </div>
                <div class="fixture-away">
                  <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                  <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
                </div>
              </div>
              <div v-if="match.homeRedCards || match.awayRedCards" class="fixture-red-row">
                <div style="display: flex; justify-content: flex-end; gap: 2px; padding-right: 26px;">
                  <span v-for="n in (match.homeRedCards || 0)" :key="'hr'+n" class="red-stripe"></span>
                </div>
                <div></div>
                <div style="display: flex; gap: 2px; padding-left: 26px;">
                  <span v-for="n in (match.awayRedCards || 0)" :key="'ar'+n" class="red-stripe"></span>
                </div>
              </div>
              <div v-if="match._compName" class="fixture-comp-name">{{ match._compName }}</div>
            </div>
          </div>
        </div>

        <!-- Today's matches: per followed competition -->
        <div v-for="group in todayCompetitionGroups" :key="group.compId" style="margin-top: 16px;">
          <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;">
            <router-link :to="`/competition/${group.compId}`" style="text-decoration: none; color: inherit;">{{ group.compName }}</router-link> — vandaag
          </div>
          <div class="card">
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row-wrap"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="fixture-row" style="cursor: pointer;">
                <div class="fixture-home">
                  <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
                  <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
                </div>
                <div class="fixture-center">
                  <span v-if="match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                  <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                  <span
                    v-if="match.status !== 'scheduled'"
                    class="badge"
                    :class="{
                      'badge-live': match.status === 'live',
                      'badge-halftime': match.status === 'halftime',
                      'badge-ended': match.status === 'ended',
                    }"
                  >{{ statusLabel(match.status) }}</span>
                </div>
                <div class="fixture-away">
                  <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                  <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
                </div>
              </div>
              <div v-if="match.homeRedCards || match.awayRedCards" class="fixture-red-row">
                <div style="display: flex; justify-content: flex-end; gap: 2px; padding-right: 26px;">
                  <span v-for="n in (match.homeRedCards || 0)" :key="'hr'+n" class="red-stripe"></span>
                </div>
                <div></div>
                <div style="display: flex; gap: 2px; padding-left: 26px;">
                  <span v-for="n in (match.awayRedCards || 0)" :key="'ar'+n" class="red-stripe"></span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty state -->
        <div v-if="todayClubMatches.length === 0 && todayCompetitionGroups.length === 0" style="margin-top: 16px;">
          <div class="card text-muted text-sm" style="text-align: center;">
            Geen wedstrijden vandaag.
          </div>
        </div>
      </template>


  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi'
import { useFavoritesStore } from '../stores/favorites'

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const loading = ref(false)
const allTodayMatches = ref([])
let refreshInterval = null

const today = new Date().toISOString().slice(0, 10)

function statusLabel(status) {
  return { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[status] || status
}

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

const todayMatches = computed(() => sortMatches(allTodayMatches.value))

// Today's matches for followed clubs
const todayClubMatches = computed(() => {
  const favClubIds = new Set(favoritesStore.clubs.map(c => c.id))
  return sortMatches(
    allTodayMatches.value.filter(m => favClubIds.has(m.homeClubId) || favClubIds.has(m.awayClubId))
  )
})

// Today's matches grouped per followed competition
const todayCompetitionGroups = computed(() => {
  const favComps = favoritesStore.competitions
  if (favComps.length === 0) return []
  const favCompIds = new Set(favComps.map(c => c.id))
  const groups = []
  for (const comp of favComps) {
    const matches = sortMatches(
      allTodayMatches.value.filter(m => m._compId === comp.id)
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

  loading.value = allTodayMatches.value.length === 0
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
        if (group.date !== today) continue
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
        if (group.date !== today) continue
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
        if (group.date !== today) continue
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
        if (group.date !== today) continue
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

    // 4. Enrich today's non-ended matches with real-time data
    const toEnrich = todayList.filter(m => m.status !== 'ended')
    if (toEnrich.length > 0) {
      const enriched = await Promise.all(
        toEnrich.map(m =>
          api.getMatch(m.matchId)
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

    allTodayMatches.value = todayList
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
