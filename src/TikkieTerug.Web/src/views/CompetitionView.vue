<template>
  <div class="content">
    <div class="page-header">
      <h1 style="flex: 1;">{{ competitionName }}</h1>
      <button
        class="btn-favorite"
        :class="isFavorite ? 'active' : ''"
        @click="toggleFavorite"
      >{{ isFavorite ? '❤️ Volgt' : '🤍 Volgen' }}</button>
    </div>

    <select class="tab-select" v-model="activeTab">
      <option value="stand">📊 Stand</option>
      <option value="uitslagen">⚽ Uitslagen</option>
      <option value="programma">📅 Programma</option>
      <option value="periodes">📋 Periodes</option>
      <option value="topscorers">🥇 Topscorers</option>
    </select>

    <!-- Stand Tab -->
    <div v-if="activeTab === 'stand'">
      <div v-if="loadingStandings" class="loading">Laden...</div>
      <div v-else-if="standings.length === 0" class="text-muted">Geen stand beschikbaar.</div>
      <div v-else>
        <div class="flex items-center" style="justify-content: flex-end; padding: 0 0 6px;">
          <button class="toggle-detail" @click="detailedStand = !detailedStand">
            {{ detailedStand ? 'Compact' : 'Details' }}
          </button>
        </div>
        <div class="card" :class="{ 'standings-scroll': detailedStand }">
          <div class="standings-row" :class="detailedStand ? 'header detailed' : 'header compact'">
            <span class="standings-pos">#</span>
            <span></span>
            <span class="standings-name">Club</span>
            <span class="standings-num">W</span>
            <template v-if="detailedStand">
              <span class="standings-num">W</span>
              <span class="standings-num">G</span>
              <span class="standings-num">V</span>
              <span class="standings-num">+</span>
              <span class="standings-num">−</span>
            </template>
            <span class="standings-num">+/−</span>
            <span class="standings-pts">Pts</span>
          </div>
          <div
            v-for="row in standings"
            :key="row.clubId"
            class="standings-row"
            :class="detailedStand ? 'detailed' : 'compact'"
            style="cursor: pointer;"
            @click="router.push(`/club/${row.clubId}`)"
          >
            <span class="standings-pos">{{ row.position }}</span>
            <span>
              <img v-if="row.logo" :src="row.logo" class="club-logo-sm" :alt="row.club" />
              <span v-else class="club-logo-sm"></span>
            </span>
            <span class="standings-name flex items-center gap-2">
              {{ row.club }}
              <span v-if="row.periodWon > 0" class="period-badge">P{{ row.periodWon }}</span>
              <span v-if="row.penaltyPoints > 0" class="penalty-marker">*</span>
            </span>
            <span class="standings-num">{{ row.played }}</span>
            <template v-if="detailedStand">
              <span class="standings-num">{{ row.won }}</span>
              <span class="standings-num">{{ row.drawn }}</span>
              <span class="standings-num">{{ row.lost }}</span>
              <span class="standings-num">{{ row.goalsFor }}</span>
              <span class="standings-num">{{ row.goalsAgainst }}</span>
            </template>
            <span class="standings-num">{{ row.goalDifference > 0 ? '+' : '' }}{{ row.goalDifference }}</span>
            <span class="standings-pts">{{ row.points }}</span>
          </div>
        </div>
        <div v-if="standings.some(r => r.penaltyPoints > 0)" class="penalty-note">
          * Punten in mindering:
          <span v-for="row in standings.filter(r => r.penaltyPoints > 0)" :key="row.clubId" class="penalty-item">
            {{ row.club }} (−{{ row.penaltyPoints }})
          </span>
        </div>
      </div>
    </div>

    <!-- Uitslagen Tab -->
    <div v-if="activeTab === 'uitslagen'">
      <div v-if="loadingResults" class="loading">Laden...</div>
      <div v-else-if="results.length === 0" class="text-muted">Geen uitslagen beschikbaar.</div>
      <div v-else>
        <div v-for="group in results" :key="group.date" class="card" style="margin-bottom: 8px;">
          <div class="text-xs text-muted font-bold" style="margin-bottom: 6px;">{{ formatDateShort(group.date) }}</div>
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row-wrap"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="fixture-row" style="cursor: pointer;">
               <div class="fixture-home">
                 <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
                 <img v-if="match.homeLogo" :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
               </div>
               <div class="fixture-center">
                 <span v-if="match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                 <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                 <span
                   v-if="match.status !== 'scheduled' && group.date === today"
                   class="badge"
                   :class="{
                     'badge-live': match.status === 'live',
                     'badge-halftime': match.status === 'halftime',
                     'badge-ended': match.status === 'ended',
                   }"
                 >{{ statusLabel(match.status) }}</span>
               </div>
               <div class="fixture-away">
                 <img v-if="match.awayLogo" :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                 <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
               </div>
              </div>
            </div>
        </div>
      </div>
    </div>

    <!-- Programma Tab -->
    <div v-if="activeTab === 'programma'">
      <div v-if="loadingFixtures" class="loading">Laden...</div>
      <div v-else-if="fixtures.length === 0" class="text-muted">Geen aankomende wedstrijden.</div>
      <div v-else>
        <div v-for="group in fixtures" :key="group.date" class="card" style="margin-bottom: 8px;">
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
                 <img v-if="match.homeLogo" :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
               </div>
               <div class="fixture-center">
                 <span v-if="!match.status || match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                 <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                 <span v-if="match.status && group.date === today" class="badge" :class="{ 'badge-live': match.status === 'live', 'badge-halftime': match.status === 'halftime', 'badge-ended': match.status === 'ended', 'badge-scheduled': match.status === 'scheduled' }">{{ statusLabel(match.status) }}</span>
               </div>
               <div class="fixture-away">
                 <img v-if="match.awayLogo" :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                 <span>{{ match.awayClub }}</span>
               </div>
              </div>
            </div>
        </div>
      </div>
    </div>

    <!-- Periodes Tab -->
    <div v-if="activeTab === 'periodes'">
      <div v-if="loadingPeriodes" class="loading">Laden...</div>
      <div v-else-if="periodes.length === 0" class="text-muted">Geen periodes beschikbaar.</div>
      <div v-else>
        <div v-for="periode in periodesReversed" :key="periode.period" class="card">
          <h3>Periode {{ periode.period }}
            <span v-if="periode.period === maxPeriod" class="badge badge-live" style="margin-left: 6px; animation: none;">Lopend</span>
          </h3>
          <div class="standings-row periodes header">
            <span class="standings-pos">#</span>
            <span></span>
            <span class="standings-name">Club</span>
            <span class="standings-num">W</span>
            <span class="standings-num">W</span>
            <span class="standings-num">G</span>
            <span class="standings-num">V</span>
            <span class="standings-pts">Pts</span>
          </div>
          <div
            v-for="row in periode.standings"
            :key="row.clubId"
            class="standings-row periodes"
            style="cursor: pointer;"
            @click="router.push(`/club/${row.clubId}`)"
          >
            <span class="standings-pos">{{ row.position }}</span>
            <span>
              <img v-if="row.logo" :src="row.logo" class="club-logo-sm" :alt="row.club" />
              <span v-else class="club-logo-sm"></span>
            </span>
            <span class="standings-name">{{ row.club }}</span>
            <span class="standings-num">{{ row.played }}</span>
            <span class="standings-num">{{ row.won }}</span>
            <span class="standings-num">{{ row.drawn }}</span>
            <span class="standings-num">{{ row.lost }}</span>
            <span class="standings-pts">{{ row.points }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Topscorers Tab -->
    <div v-if="activeTab === 'topscorers'">
      <div v-if="loadingTopscorers" class="loading">Laden...</div>
      <div v-else-if="topscorers.length === 0" class="text-muted">Geen topscorers beschikbaar.</div>
      <div v-else class="card">
        <div class="topscorer-row header">
          <span></span>
          <span class="topscorer-name">Speler</span>
          <span class="topscorer-num">Goals</span>
        </div>
        <div
          v-for="player in topscorers"
          :key="player.playerId"
          class="topscorer-row"
          style="cursor: pointer;"
          @click="router.push(`/club/${player.clubId}`)"
        >
          <span class="topscorer-name">
            <span>{{ player.name }}</span>
            <span class="text-xs text-muted flex items-center gap-2">
              <img :src="player.logo" class="club-logo-sm" :alt="player.club" />
              {{ player.club }}
            </span>
          </span>
          <span class="topscorer-num font-bold">{{ player.goals }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const props = defineProps({
  id: {
    type: String,
    required: true
  }
})

const router = useRouter()
const route = useRoute()
const api = useApi()
const favoritesStore = useFavoritesStore()

const competitionId = parseInt(props.id, 10)

const competitionName = ref('Competitie')

const isFavorite = computed(() => favoritesStore.isCompetitionFavorite(competitionId))

function toggleFavorite() {
  if (isFavorite.value) {
    favoritesStore.removeCompetition(competitionId)
  } else {
    favoritesStore.addCompetition({ id: competitionId, name: competitionName.value })
  }
}

const validTabs = ['stand', 'uitslagen', 'programma', 'periodes', 'topscorers']
const activeTab = ref(validTabs.includes(route.query.tab) ? route.query.tab : 'stand')

// Sync tab to URL query
watch(activeTab, (tab) => {
  router.replace({ query: { ...route.query, tab } })
})
const detailedStand = ref(false)

const standings = ref([])
const results = ref([])
const fixtures = ref([])
const periodes = ref([])
const topscorers = ref([])

const loadingStandings = ref(false)
const loadingResults = ref(false)
const loadingFixtures = ref(false)
const loadingPeriodes = ref(false)
const loadingTopscorers = ref(false)

const today = new Date().toISOString().slice(0, 10)

const periodesReversed = computed(() => [...periodes.value].reverse())
const maxPeriod = computed(() => periodes.value.length > 0 ? Math.max(...periodes.value.map(p => p.period)) : 0)

function statusLabel(status) {
  return { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[status] || status
}

function formatDateShort(dateStr) {
  const today = new Date().toISOString().slice(0, 10)
  if (dateStr === today) return 'Vandaag'
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

async function loadStandings() {
  if (standings.value.length > 0) return
  loadingStandings.value = true
  try {
    standings.value = await api.getStandings(competitionId)
  } finally {
    loadingStandings.value = false
  }
}

async function loadResults() {
  if (results.value.length > 0) return
  loadingResults.value = true
  try {
    results.value = await api.getResults(competitionId)
  } finally {
    loadingResults.value = false
  }
}

async function loadFixtures() {
  if (fixtures.value.length > 0) return
  loadingFixtures.value = true
  try {
    fixtures.value = await api.getFixtures(competitionId)
  } finally {
    loadingFixtures.value = false
  }
}

async function loadPeriodes() {
  if (periodes.value.length > 0) return
  loadingPeriodes.value = true
  try {
    periodes.value = await api.getPeriodestand(competitionId)
  } finally {
    loadingPeriodes.value = false
  }
}

async function loadTopscorers() {
  if (topscorers.value.length > 0) return
  loadingTopscorers.value = true
  try {
    topscorers.value = await api.getCompetitionTopscorers(competitionId)
  } finally {
    loadingTopscorers.value = false
  }
}

function loadActiveTab(tab) {
  if (tab === 'stand') loadStandings()
  else if (tab === 'uitslagen') loadResults()
  else if (tab === 'programma') loadFixtures()
  else if (tab === 'periodes') loadPeriodes()
  else if (tab === 'topscorers') loadTopscorers()
}

function switchTab(tab) {
  activeTab.value = tab
}

watch(activeTab, (tab) => {
  loadActiveTab(tab)
})

onMounted(() => {
  loadActiveTab(activeTab.value)
  api.getCompetitionName(competitionId).then(data => {
    competitionName.value = data.name
  }).catch(() => {})
})
</script>
