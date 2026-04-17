<template>
  <div class="content">
    <div class="page-header">
      <h1>{{ competitionName }}</h1>
    </div>

    <div class="tabs">
      <button
        class="tab"
        :class="{ active: activeTab === 'stand' }"
        @click="switchTab('stand')"
      >Stand</button>
      <button
        class="tab"
        :class="{ active: activeTab === 'uitslagen' }"
        @click="switchTab('uitslagen')"
      >Uitslagen</button>
      <button
        class="tab"
        :class="{ active: activeTab === 'programma' }"
        @click="switchTab('programma')"
      >Programma</button>
      <button
        class="tab"
        :class="{ active: activeTab === 'periodes' }"
        @click="switchTab('periodes')"
      >Periodes</button>
    </div>

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
        <div v-for="group in results" :key="group.date" class="date-group">
          <div class="text-sm text-muted">{{ new Date(group.date + 'T00:00:00').toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' }) }}</div>
           <div class="card">
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row"
              @click="router.push(`/match/${match.matchId}`)"
            >
               <div class="fixture-home">
                 <span>{{ match.homeClub }}</span>
                 <img v-if="match.homeLogo" :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
               </div>
               <div class="fixture-center">
                 <span v-if="match.status !== 'scheduled'" class="fixture-score">{{ match.homeScore }} – {{ match.awayScore }}</span>
                 <span v-else class="fixture-score text-muted">{{ match.time }}</span>
                 <span
                   class="badge"
                   :class="{
                     'badge-live': match.status === 'live',
                     'badge-halftime': match.status === 'halftime',
                     'badge-ended': match.status === 'ended',
                     'badge-scheduled': match.status === 'scheduled',
                   }"
                 >{{ { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[match.status] || match.status }}</span>
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

    <!-- Programma Tab -->
    <div v-if="activeTab === 'programma'">
      <div v-if="loadingFixtures" class="loading">Laden...</div>
      <div v-else-if="fixtures.length === 0" class="text-muted">Geen aankomende wedstrijden.</div>
      <div v-else>
        <div v-for="group in fixtures" :key="group.date" class="date-group">
          <div class="text-sm text-muted">{{ new Date(group.date + 'T00:00:00').toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' }) }}</div>
           <div class="card">
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="fixture-row"
              @click="router.push(`/match/${match.matchId}`)"
            >
               <div class="fixture-home">
                 <span>{{ match.homeClub }}</span>
                 <img v-if="match.homeLogo" :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
               </div>
               <div class="fixture-time">{{ match.time }}</div>
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
        <div v-for="periode in periodes" :key="periode.period" class="card">
          <h3>Periode {{ periode.period }}</h3>
          <div class="standings-row header">
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
            class="standings-row"
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
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'

const props = defineProps({
  id: {
    type: String,
    required: true
  }
})

const router = useRouter()
const api = useApi()

const competitionId = parseInt(props.id, 10)

const competitionName = ref('Competitie')

const activeTab = ref('stand')
const detailedStand = ref(false)

const standings = ref([])
const results = ref([])
const fixtures = ref([])
const periodes = ref([])

const loadingStandings = ref(false)
const loadingResults = ref(false)
const loadingFixtures = ref(false)
const loadingPeriodes = ref(false)

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

function loadActiveTab(tab) {
  if (tab === 'stand') loadStandings()
  else if (tab === 'uitslagen') loadResults()
  else if (tab === 'programma') loadFixtures()
  else if (tab === 'periodes') loadPeriodes()
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
