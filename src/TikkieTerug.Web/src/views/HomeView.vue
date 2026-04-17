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

        <!-- Match rows grouped by date -->
        <template v-if="resultsByCompetition[competition.id]?.length">
          <template
            v-for="group in resultsByCompetition[competition.id]"
            :key="group.date"
          >
            <div class="date-group">{{ formatDate(group.date) }}</div>
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="match-row"
              @click="router.push({ name: 'match', params: { id: match.matchId } })"
            >
            <span
              class="badge match-row-badge"
              :class="{
                'badge-live': match.status === 'live',
                'badge-halftime': match.status === 'halftime',
                'badge-ended': match.status === 'ended',
                'badge-scheduled': match.status === 'scheduled',
              }"
            >{{ statusLabel(match.status) }}</span>
            <!-- Home team -->
            <div class="match-teams">
              <div class="match-team flex items-center gap-2">
                <img
                  v-if="match.homeLogo"
                  :src="match.homeLogo"
                  :alt="match.homeClub"
                  class="club-logo-sm"
                  style="cursor: pointer;"
                  @click.stop="router.push(`/club/${match.homeClubId}`)"
                />
                <span :class="{ 'font-bold': match.homeScore > match.awayScore && match.status === 'ended' }">
                  {{ match.homeClub }}
                </span>
              </div>

              <!-- Score -->
              <div class="match-score">
                <span v-if="match.status !== 'scheduled'" class="score-line font-bold">{{ match.homeScore }} – {{ match.awayScore }}</span>
                <span v-else class="score-line text-muted">{{ match.time }}</span>
              </div>

              <!-- Away team -->
              <div class="match-team flex items-center gap-2">
                <img
                  v-if="match.awayLogo"
                  :src="match.awayLogo"
                  :alt="match.awayClub"
                  class="club-logo-sm"
                  style="cursor: pointer;"
                  @click.stop="router.push(`/club/${match.awayClubId}`)"
                />
                <span :class="{ 'font-bold': match.awayScore > match.homeScore && match.status === 'ended' }">
                  {{ match.awayClub }}
                </span>
              </div>
            </div>
          </div>
          </template>
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

const today = new Date().toISOString().slice(0, 10)

function statusLabel(status) {
  switch (status) {
    case 'live': return 'Live'
    case 'halftime': return 'Rust'
    case 'ended': return 'Afgelopen'
    case 'scheduled': return 'Gepland'
    default: return status ?? ''
  }
}

function formatDate(dateStr) {
  if (dateStr === today) return 'Vandaag'
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' })
}

let refreshInterval = null

async function fetchAllResults() {
  if (!favoritesStore.hasFavorites()) return

  loading.value = true
  try {
    const entries = await Promise.all(
      favoritesStore.competitions.map(async (competition) => {
        const grouped = await api.getResults(competition.id)
        return [competition.id, grouped ?? []]
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
