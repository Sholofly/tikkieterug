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
            class="fixture-row"
            @click="router.push(`/match/${match.matchId}`)"
          >
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
const allMatches = ref([])
let refreshInterval = null

const today = new Date().toISOString().slice(0, 10)

function statusLabel(status) {
  return { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[status] || status
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
  return [...allMatches.value].sort((a, b) => {
    const oa = statusOrder[a.status] ?? 9
    const ob = statusOrder[b.status] ?? 9
    if (oa !== ob) return oa - ob
    return a.time.localeCompare(b.time)
  })
})

async function fetchTodayMatches() {
  if (!favoritesStore.hasFavorites()) return

  loading.value = allMatches.value.length === 0
  try {
    const compIds = getCompetitionIds()
    // Fetch uitslagen + programma for each competition in parallel
    const fetches = compIds.flatMap(id => [
      api.getResults(id).catch(() => []),
      api.getFixtures(id).catch(() => []),
    ])
    const results = await Promise.all(fetches)

    // Extract today's matches from grouped results, dedupe by matchId
    const seen = new Set()
    const matches = []
    for (const grouped of results) {
      if (!Array.isArray(grouped)) continue
      for (const group of grouped) {
        if (group.date !== today) continue
        for (const match of (group.matches || [])) {
          if (!seen.has(match.matchId)) {
            seen.add(match.matchId)
            matches.push(match)
          }
        }
      }
    }
    allMatches.value = matches
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchTodayMatches()
  refreshInterval = setInterval(fetchTodayMatches, 60_000)
})

onUnmounted(() => {
  clearInterval(refreshInterval)
})
</script>
