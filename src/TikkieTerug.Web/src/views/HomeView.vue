<template>
  <div class="page-header">
    <h1>Wedstrijden</h1>
  </div>

  <div class="content">
    <!-- No favorites state -->
    <div v-if="!favoritesStore.hasFavorites()" class="empty">
      <p>Je hebt nog geen favoriete competities of clubs toegevoegd.</p>
      <router-link to="/favorites" class="badge">Voeg favorieten toe</router-link>
    </div>

    <!-- Loading state -->
    <div v-else-if="loading" class="loading">
      <p class="text-muted">Scores laden...</p>
    </div>

    <!-- Competition cards -->
    <template v-else>
      <div
        v-for="competition in favoritesStore.competitions"
        :key="competition.id"
        class="card"
      >
        <!-- Competition header -->
        <div class="flex items-center gap-2">
          <router-link
            :to="`/competition/${competition.id}`"
            class="font-bold"
          >
            {{ competition.name }}
          </router-link>
        </div>

        <!-- Match rows for most recent match day -->
        <template v-if="resultsByCompetition[competition.id]?.length">
          <div
            v-for="match in resultsByCompetition[competition.id]"
            :key="match.matchId"
            class="match-row"
            @click="router.push({ name: 'match', params: { id: match.matchId } })"
          >
            <!-- Home team -->
            <div class="match-teams">
              <div class="match-team flex items-center gap-2">
                <img
                  v-if="match.homeLogo"
                  :src="match.homeLogo"
                  :alt="match.homeClub"
                  class="club-logo-sm"
                />
                <span :class="{ 'font-bold': match.homeScore > match.awayScore && match.status === 'ended' }">
                  {{ match.homeClub }}
                </span>
              </div>

              <!-- Score / status -->
              <div class="match-score">
                <template v-if="match.status === 'live'">
                  <div class="status-indicator live"></div>
                  <span class="score-line text-live">{{ match.homeScore }} – {{ match.awayScore }}</span>
                </template>
                <template v-else-if="match.status === 'halftime'">
                  <div class="status-indicator halftime"></div>
                  <span class="score-line text-halftime">{{ match.homeScore }} – {{ match.awayScore }}</span>
                </template>
                <template v-else-if="match.status === 'ended'">
                  <div class="status-indicator ended"></div>
                  <span class="score-line font-bold">{{ match.homeScore }} – {{ match.awayScore }}</span>
                </template>
                <template v-else>
                  <!-- scheduled -->
                  <div class="status-indicator scheduled"></div>
                  <span class="score-line text-muted">{{ match.time }}</span>
                </template>
              </div>

              <!-- Away team -->
              <div class="match-team flex items-center gap-2">
                <img
                  v-if="match.awayLogo"
                  :src="match.awayLogo"
                  :alt="match.awayClub"
                  class="club-logo-sm"
                />
                <span :class="{ 'font-bold': match.awayScore > match.homeScore && match.status === 'ended' }">
                  {{ match.awayClub }}
                </span>
              </div>
            </div>
          </div>
        </template>

        <p v-else class="text-muted text-sm">Geen recente wedstrijden gevonden.</p>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi'
import { useFavoritesStore } from '../stores/favorites'

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const loading = ref(false)
const resultsByCompetition = ref({})

let refreshInterval = null

async function fetchAllResults() {
  if (!favoritesStore.hasFavorites()) return

  loading.value = true
  try {
    const entries = await Promise.all(
      favoritesStore.competitions.map(async (competition) => {
        const grouped = await api.getResults(competition.id)
        // grouped is sorted descending by date — take the first group (most recent match day)
        const mostRecent = grouped?.[0]?.matches ?? []
        return [competition.id, mostRecent]
      })
    )
    resultsByCompetition.value = Object.fromEntries(entries)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchAllResults()
  refreshInterval = setInterval(fetchAllResults, 60_000)
})

onUnmounted(() => {
  clearInterval(refreshInterval)
})
</script>
