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
        :class="{ active: activeTab === 'periodes' }"
        @click="switchTab('periodes')"
      >Periodes</button>
    </div>

    <!-- Stand Tab -->
    <div v-if="activeTab === 'stand'">
      <div v-if="loadingStandings" class="loading">Laden...</div>
      <div v-else-if="standings.length === 0" class="text-muted">Geen stand beschikbaar.</div>
      <div v-else class="card">
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
          v-for="row in standings"
          :key="row.clubId"
          class="standings-row"
        >
          <span class="standings-pos">{{ row.position }}</span>
          <span>
            <img v-if="row.logo" :src="row.logo" class="club-logo-sm" :alt="row.club" />
            <span v-else class="club-logo-sm"></span>
          </span>
          <span class="standings-name flex items-center gap-2">
            {{ row.club }}
            <span v-if="row.periodWon > 0" class="period-badge">P{{ row.periodWon }}</span>
          </span>
          <span class="standings-num">{{ row.played }}</span>
          <span class="standings-num">{{ row.won }}</span>
          <span class="standings-num">{{ row.drawn }}</span>
          <span class="standings-num">{{ row.lost }}</span>
          <span class="standings-pts">{{ row.points }}</span>
        </div>
      </div>
    </div>

    <!-- Uitslagen Tab -->
    <div v-if="activeTab === 'uitslagen'">
      <div v-if="loadingResults" class="loading">Laden...</div>
      <div v-else-if="results.length === 0" class="text-muted">Geen uitslagen beschikbaar.</div>
      <div v-else>
        <div v-for="group in results" :key="group.date" class="date-group">
          <div class="text-sm text-muted">{{ group.date }}</div>
          <div class="card">
            <div
              v-for="match in group.matches"
              :key="match.matchId"
              class="match-row"
              @click="router.push(`/match/${match.matchId}`)"
            >
              <div class="match-teams">
                <div class="match-team">
                  <img v-if="match.homeLogo" :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" />
                  <span>{{ match.homeClub }}</span>
                </div>
                <div class="match-score">
                  <span v-if="match.status === 'live'" class="score-line">
                    {{ match.homeScore }} - {{ match.awayScore }}
                    <span class="badge badge-live">LIVE</span>
                  </span>
                  <span v-else-if="match.status === 'halftime'" class="score-line">
                    {{ match.homeScore }} - {{ match.awayScore }}
                    <span class="badge badge-halftime">RUST</span>
                  </span>
                  <span v-else-if="match.status === 'ended'" class="score-line">
                    {{ match.homeScore }} - {{ match.awayScore }}
                    <span class="badge badge-ended">Eind</span>
                  </span>
                  <span v-else class="score-line">
                    <span class="badge badge-scheduled">{{ match.time }}</span>
                  </span>
                </div>
                <div class="match-team">
                  <img v-if="match.awayLogo" :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" />
                  <span>{{ match.awayClub }}</span>
                </div>
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

const standings = ref([])
const results = ref([])
const periodes = ref([])

const loadingStandings = ref(false)
const loadingResults = ref(false)
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
