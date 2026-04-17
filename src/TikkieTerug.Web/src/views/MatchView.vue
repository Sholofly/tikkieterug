<template>
  <div class="content">
    <div v-if="loading" class="loading text-center">Loading match details...</div>

    <div v-else-if="match">
      <!-- Match Header -->
      <div class="card" style="margin-bottom: 1.5rem;">
        <div class="flex items-center justify-between gap-3" style="padding: 1.5rem 1rem 0.5rem;">
          <!-- Home Club -->
          <div class="text-center" style="flex: 1;">
            <img
              v-if="match.homeLogo"
              :src="match.homeLogo"
              :alt="match.homeClub"
              class="club-logo-lg"
              style="display: block; margin: 0 auto 0.5rem;"
            />
            <div v-else class="club-logo-lg" style="display: flex; align-items: center; justify-content: center; margin: 0 auto 0.5rem; background: #eee; border-radius: 50%; font-size: 1.5rem;">⚽</div>
            <div class="font-bold">{{ match.homeClub }}</div>
            <div v-if="match.homeRedCards > 0" style="color: #dc2626; margin-top: 0.25rem;">
              <span v-for="n in match.homeRedCards" :key="n" style="margin-right: 2px;">🟥</span>
            </div>
          </div>

          <!-- Score -->
          <div class="text-center" style="flex: 0 0 auto; min-width: 7rem;">
            <div class="font-bold" style="font-size: 2.5rem; line-height: 1.1; letter-spacing: 2px;">
              {{ match.homeScore ?? '-' }} – {{ match.awayScore ?? '-' }}
            </div>
          </div>

          <!-- Away Club -->
          <div class="text-center" style="flex: 1;">
            <img
              v-if="match.awayLogo"
              :src="match.awayLogo"
              :alt="match.awayClub"
              class="club-logo-lg"
              style="display: block; margin: 0 auto 0.5rem;"
            />
            <div v-else class="club-logo-lg" style="display: flex; align-items: center; justify-content: center; margin: 0 auto 0.5rem; background: #eee; border-radius: 50%; font-size: 1.5rem;">⚽</div>
            <div class="font-bold">{{ match.awayClub }}</div>
            <div v-if="match.awayRedCards > 0" style="color: #dc2626; margin-top: 0.25rem;">
              <span v-for="n in match.awayRedCards" :key="n" style="margin-right: 2px;">🟥</span>
            </div>
          </div>
        </div>

        <!-- Date, Time & Status -->
        <div class="text-center" style="padding: 0.5rem 1rem 1.25rem;">
          <div class="text-muted text-sm" style="margin-bottom: 0.5rem;">
            {{ match.date }}<span v-if="match.time"> &middot; {{ match.time }}</span>
          </div>
          <span
            class="badge"
            :class="{
              'badge-live': match.status === 'live',
              'badge-halftime': match.status === 'halftime',
              'badge-ended': match.status === 'ended',
              'badge-scheduled': match.status === 'scheduled',
            }"
          >{{ statusLabel }}</span>
        </div>
      </div>

      <!-- Goals Timeline -->
      <div v-if="match.goals && match.goals.length > 0" class="card">
        <div style="padding: 1rem 1rem 0.5rem; border-bottom: 1px solid var(--border);">
          <span class="font-bold">Doelpunten</span>
        </div>
        <div style="padding: 0.5rem 0;">
          <div
            v-for="(goal, index) in goalsWithScore"
            :key="index"
            class="goal-row"
          >
            <!-- Home side -->
            <div class="goal-col goal-col-home">
              <template v-if="goal.side === 'home'">
                <span class="goal-player-name">{{ goal.player }}</span>
                <span class="goal-min">{{ goal.minute }}'</span>
                <span class="goal-icon">⚽</span>
              </template>
            </div>

            <!-- Running score center -->
            <div class="goal-col-center">
              {{ goal.runningHome }} – {{ goal.runningAway }}
            </div>

            <!-- Away side -->
            <div class="goal-col goal-col-away">
              <template v-if="goal.side === 'away'">
                <span class="goal-icon">⚽</span>
                <span class="goal-min">{{ goal.minute }}'</span>
                <span class="goal-player-name">{{ goal.player }}</span>
              </template>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="text-center text-muted text-sm" style="margin-top: 1rem;">
        No goals recorded yet.
      </div>
    </div>

    <div v-else class="text-center text-muted">Match not found.</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'

const props = defineProps({
  id: {
    type: String,
    required: true,
  },
})

const router = useRouter()
const api = useApi()

const match = ref(null)
const loading = ref(true)
let refreshInterval = null

async function fetchMatch() {
  try {
    const data = await api.getMatch(props.id)
    match.value = data
  } catch (e) {
    match.value = null
  } finally {
    loading.value = false
  }
}

const statusLabel = computed(() => {
  switch (match.value?.status) {
    case 'live': return 'Live'
    case 'halftime': return 'Half-time'
    case 'ended': return 'Ended'
    case 'scheduled': return 'Scheduled'
    default: return match.value?.status ?? ''
  }
})

const goalsWithScore = computed(() => {
  if (!match.value?.goals) return []
  let homeRunning = 0
  let awayRunning = 0
  return match.value.goals.map((goal) => {
    if (goal.side === 'home') {
      homeRunning++
    } else {
      awayRunning++
    }
    return {
      ...goal,
      runningHome: homeRunning,
      runningAway: awayRunning,
    }
  })
})

onMounted(async () => {
  await fetchMatch()

  if (match.value?.status === 'live' || match.value?.status === 'halftime') {
    refreshInterval = setInterval(fetchMatch, 30000)
  }
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>
