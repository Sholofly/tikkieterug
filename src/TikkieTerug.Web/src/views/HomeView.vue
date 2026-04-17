<template>
  <div class="page-header">
    <h1>Dashboard</h1>
  </div>

  <div class="content">
    <!-- No favorites -->
    <div v-if="!favoritesStore.hasFavorites()" class="empty">
      <p>Je hebt nog geen favoriete competities of clubs toegevoegd.</p>
      <router-link to="/favorites" class="btn btn-primary" style="display: inline-block; margin-top: 12px;">Voeg favorieten toe</router-link>
    </div>

    <template v-else>
      <!-- Favorite clubs -->
      <div v-if="favoritesStore.clubs.length > 0" class="card" style="margin-bottom: 10px;">
        <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;">Mijn clubs</div>
        <div class="dashboard-icons">
          <router-link
            v-for="club in favoritesStore.clubs"
            :key="club.id"
            :to="`/club/${club.id}`"
            class="dashboard-icon-link"
          >
            <img :src="club.logo" :alt="club.name" class="club-logo" />
          </router-link>
        </div>
      </div>

      <!-- Favorite competitions -->
      <div v-if="favoritesStore.competitions.length > 0" class="card" style="margin-bottom: 10px;">
        <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;">Mijn competities</div>
        <div class="dashboard-icons">
          <router-link
            v-for="comp in favoritesStore.competitions"
            :key="comp.id"
            :to="`/competition/${comp.id}`"
            class="dashboard-comp-link"
          >
            {{ comp.name }}
          </router-link>
        </div>
      </div>

      <!-- Today's matches -->
      <div style="margin-top: 16px;">
        <div class="text-xs text-muted font-bold" style="margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;">Vandaag</div>
        <div v-if="loading" class="loading">Laden...</div>
        <div v-else-if="todayMatches.length === 0" class="card text-muted text-sm" style="text-align: center;">
          Geen wedstrijden vandaag.
        </div>
        <div v-else class="card">
          <div
            v-for="match in todayMatches"
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
            <div v-if="match._compName" class="fixture-comp-name" @click.stop="router.push(`/competition/${match._compId}`)">{{ match._compName }}</div>
          </div>
        </div>
      </div>

      <!-- Upcoming club matches -->
      <div v-if="clubUpcomingGrouped.length > 0" style="margin-top: 16px;">
        <div class="collapsible-header" @click="toggleSection('clubUpcoming')" style="padding: 0 4px; cursor: pointer; display: flex; align-items: center; gap: 6px; margin-bottom: 8px;">
          <span class="collapse-chevron" :class="{ open: sections.clubUpcoming }">▶</span>
          <span class="text-xs text-muted font-bold" style="text-transform: uppercase; letter-spacing: 0.5px;">Komende wedstrijden clubs</span>
        </div>
        <template v-if="sections.clubUpcoming">
          <div v-for="group in clubUpcomingGrouped" :key="group.date" class="card" style="margin-bottom: 8px;">
            <div class="text-xs text-muted font-bold" style="margin-bottom: 6px;">{{ formatDateShort(group.date) }}</div>
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row-wrap"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="fixture-row" style="cursor: pointer;">
                <div class="fixture-home">
                  <span>{{ match.homeClub }}</span>
                  <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
                </div>
                <div class="fixture-center">
                  <span class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                </div>
                <div class="fixture-away">
                  <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                  <span>{{ match.awayClub }}</span>
                </div>
              </div>
              <div v-if="match._compName" class="fixture-comp-name" @click.stop="router.push(`/competition/${match._compId}`)">{{ match._compName }}</div>
            </div>
          </div>
        </template>
      </div>

      <!-- Upcoming competition matches -->
      <div v-if="competitionUpcomingGrouped.length > 0" style="margin-top: 16px;">
        <div class="collapsible-header" @click="toggleSection('compUpcoming')" style="padding: 0 4px; cursor: pointer; display: flex; align-items: center; gap: 6px; margin-bottom: 8px;">
          <span class="collapse-chevron" :class="{ open: sections.compUpcoming }">▶</span>
          <span class="text-xs text-muted font-bold" style="text-transform: uppercase; letter-spacing: 0.5px;">Komende wedstrijden competities</span>
        </div>
        <template v-if="sections.compUpcoming">
          <div v-for="group in competitionUpcomingGrouped" :key="group.date" class="card" style="margin-bottom: 8px;">
            <div class="text-xs text-muted font-bold" style="margin-bottom: 6px;">{{ formatDateShort(group.date) }}</div>
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row-wrap"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="fixture-row" style="cursor: pointer;">
                <div class="fixture-home">
                  <span>{{ match.homeClub }}</span>
                  <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
                </div>
                <div class="fixture-center">
                  <span class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                </div>
                <div class="fixture-away">
                  <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                  <span>{{ match.awayClub }}</span>
                </div>
              </div>
              <div v-if="match._compName" class="fixture-comp-name" @click.stop="router.push(`/competition/${match._compId}`)">{{ match._compName }}</div>
            </div>
          </div>
        </template>
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
const allUpcomingMatches = ref([])
let refreshInterval = null

// Collapsible section state, persisted in localStorage
const SECTIONS_KEY = 'dashboard-sections'
const sections = ref(JSON.parse(localStorage.getItem(SECTIONS_KEY) || '{"clubUpcoming":false,"compUpcoming":false}'))

function toggleSection(key) {
  sections.value[key] = !sections.value[key]
  localStorage.setItem(SECTIONS_KEY, JSON.stringify(sections.value))
}

const today = new Date().toISOString().slice(0, 10)

function statusLabel(status) {
  return { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[status] || status
}

function formatDateShort(dateStr) {
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

// Collect unique competition IDs from both favorite clubs and competitions
function getCompetitionIds() {
  const ids = new Set()
  for (const comp of favoritesStore.competitions) ids.add(comp.id)
  for (const club of favoritesStore.clubs) {
    if (club.competitionId) ids.add(club.competitionId)
  }
  return [...ids]
}

// Sort: live first, halftime, scheduled, ended last
const statusOrder = { live: 0, halftime: 1, scheduled: 2, ended: 3 }

const todayMatches = computed(() => {
  return [...allTodayMatches.value].sort((a, b) => {
    const oa = statusOrder[a.status] ?? 9
    const ob = statusOrder[b.status] ?? 9
    if (oa !== ob) return oa - ob
    return a.time.localeCompare(b.time)
  })
})

function groupByDate(matches) {
  const groups = []
  let current = null
  for (const m of matches) {
    if (!current || current.date !== m._date) {
      current = { date: m._date, matches: [] }
      groups.push(current)
    }
    current.matches.push(m)
  }
  return groups
}

const clubUpcomingGrouped = computed(() => {
  const favClubIds = new Set(favoritesStore.clubs.map(c => c.id))
  const sorted = allUpcomingMatches.value
    .filter(m => favClubIds.has(m.homeClubId) || favClubIds.has(m.awayClubId))
    .sort((a, b) => a._date.localeCompare(b._date) || a.time.localeCompare(b.time))
    .slice(0, 10)
  return groupByDate(sorted)
})

const competitionUpcomingGrouped = computed(() => {
  const favCompIds = new Set(favoritesStore.competitions.map(c => c.id))
  const sorted = allUpcomingMatches.value
    .filter(m => favCompIds.has(m._compId))
    .sort((a, b) => a._date.localeCompare(b._date) || a.time.localeCompare(b.time))
    .slice(0, 15)
  return groupByDate(sorted)
})

async function fetchDashboardData() {
  if (!favoritesStore.hasFavorites()) return

  loading.value = allTodayMatches.value.length === 0
  try {
    const compIds = getCompetitionIds()

    // Fetch uitslagen, programma, and competition names in parallel
    const [uitslResults, programmaResults, nameResults] = await Promise.all([
      Promise.all(compIds.map(id => api.getResults(id).catch(() => []))),
      Promise.all(compIds.map(id =>
        api.getFixtures(id).then(data => ({ compId: id, data })).catch(() => ({ compId: id, data: [] }))
      )),
      Promise.all(compIds.map(id =>
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
    const upcomingList = []

    // Process uitslagen (only today) — tag with compId from the fetch index
    for (let i = 0; i < uitslResults.length; i++) {
      const grouped = uitslResults[i]
      if (!Array.isArray(grouped)) continue
      for (const group of grouped) {
        if (group.date !== today) continue
        for (const match of (group.matches || [])) {
          if (!seen.has(match.matchId)) {
            seen.add(match.matchId)
            match._compId = compIds[i]
            match._compName = compNames[compIds[i]] || null
            todayList.push(match)
          }
        }
      }
    }

    // Process programma — split into today vs upcoming
    for (const { compId, data } of programmaResults) {
      if (!Array.isArray(data)) continue
      for (const group of data) {
        for (const match of (group.matches || [])) {
          if (seen.has(match.matchId)) continue
          seen.add(match.matchId)
          match._compId = compId
          match._compName = compNames[compId] || null
          match._date = group.date
          if (group.date === today) {
            todayList.push(match)
          } else {
            upcomingList.push(match)
          }
        }
      }
    }

    allTodayMatches.value = todayList
    allUpcomingMatches.value = upcomingList
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
