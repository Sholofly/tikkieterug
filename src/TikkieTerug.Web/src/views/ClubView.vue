<template>
  <div class="content">
    <div v-if="loading" class="loading text-center">Laden...</div>

    <div v-else-if="data">
      <!-- Team photo -->
      <div v-if="data.club.teamPhoto" class="card" style="padding: 0; overflow: hidden; margin-bottom: 10px;">
        <img :src="data.club.teamPhoto" :alt="data.club.name" style="width: 100%; display: block;" />
      </div>

      <!-- Club header -->
      <div class="card" style="margin-bottom: 10px;">
        <div class="flex items-center gap-2">
          <img :src="data.club.logo" :alt="data.club.name" class="club-logo-lg" />
          <div>
            <h1 class="font-bold" style="font-size: 1.2rem;">{{ data.club.name }}</h1>
            <router-link
              v-if="data.club.competitionId"
              :to="`/competition/${data.club.competitionId}`"
              class="text-sm"
            >{{ data.club.competitionName || 'Competitie' }}</router-link>
          </div>
          <button
            class="btn-favorite"
            :class="isFavorite ? 'active' : ''"
            style="margin-left: auto;"
            @click="toggleFavorite"
          >{{ isFavorite ? '★ Favoriet' : '☆ Favoriet' }}</button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button class="tab" :class="{ active: activeTab === 'programma' }" @click="activeTab = 'programma'">Programma</button>
        <button class="tab" :class="{ active: activeTab === 'uitslagen' }" @click="activeTab = 'uitslagen'">Uitslagen</button>
        <button class="tab" :class="{ active: activeTab === 'stand' }" @click="activeTab = 'stand'">Stand</button>
        <button class="tab" :class="{ active: activeTab === 'topscorers' }" @click="activeTab = 'topscorers'">Topscorers</button>
      </div>

      <!-- Programma -->
      <div v-if="activeTab === 'programma'">
        <div v-if="data.programma.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen aankomende wedstrijden.</div>
        <div v-else class="card">
          <div
            v-for="match in allProgramma"
            :key="match.matchId"
            class="fixture-row"
            @click="router.push(`/match/${match.matchId}`)"
          >
            <div class="fixture-home">
              <span>{{ match.homeClub }}</span>
              <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
            </div>
            <div class="fixture-center" style="min-width: 70px;">
              <span class="text-xs text-muted">{{ formatDateShort(match.date) }}</span>
              <span class="fixture-time" style="font-size: 0.8rem;">{{ match.time }}</span>
            </div>
            <div class="fixture-away">
              <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
              <span>{{ match.awayClub }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Uitslagen -->
      <div v-if="activeTab === 'uitslagen'">
        <div v-if="data.uitslagen.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen uitslagen.</div>
        <div v-else class="card">
          <div
            v-for="match in allUitslagen"
            :key="match.matchId"
            class="fixture-row"
            @click="router.push(`/match/${match.matchId}`)"
          >
            <div class="fixture-home">
              <span>{{ match.homeClub }}</span>
              <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
            </div>
            <div class="fixture-center" style="min-width: 70px;">
              <span class="text-xs text-muted">{{ formatDateShort(match.date) }}</span>
              <span v-if="match.status !== 'scheduled'" class="fixture-score">{{ match.homeScore }} – {{ match.awayScore }}</span>
              <span v-else class="fixture-score text-muted">{{ match.time }}</span>
            </div>
            <div class="fixture-away">
              <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
              <span>{{ match.awayClub }}</span>
            </div>
            <!-- Report indicators -->
            <div v-if="match.homeReport || match.awayReport" class="report-indicators">
              <span v-if="match.homeReport" class="report-icon" title="Thuisverslag">📝</span>
              <span v-if="match.awayReport" class="report-icon" title="Uitverslag">📝</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Stand -->
      <div v-if="activeTab === 'stand'">
        <div v-if="data.stand.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen stand beschikbaar.</div>
        <div v-else class="card">
          <div class="standings-row header compact-team">
            <span class="standings-pos">#</span>
            <span></span>
            <span class="standings-name">Club</span>
            <span class="standings-num">W</span>
            <span class="standings-pts">Pts</span>
          </div>
          <div
            v-for="row in data.stand"
            :key="row.clubId"
            class="standings-row compact-team"
            :class="{ 'highlight-row': row.clubId === (data.club.id ?? parseInt(id)) }"
          >
            <span class="standings-pos">{{ row.position }}</span>
            <span><img :src="row.logo" class="club-logo-sm" :alt="row.club" /></span>
            <span class="standings-name">{{ row.club }}</span>
            <span class="standings-num">{{ row.played }}</span>
            <span class="standings-pts">{{ row.points }}</span>
          </div>
        </div>
      </div>

      <!-- Topscorers -->
      <div v-if="activeTab === 'topscorers'">
        <div v-if="data.topscorers.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen topscorers.</div>
        <div v-else class="card">
          <div class="topscorer-row three-col header">
            <span class="topscorer-name">Speler</span>
            <span class="topscorer-num">Seizoen</span>
            <span class="topscorer-num">Totaal</span>
          </div>
          <div
            v-for="player in data.topscorers"
            :key="player.playerId"
            class="topscorer-row three-col"
          >
            <span class="topscorer-name">{{ player.name }}</span>
            <span class="topscorer-num font-bold">{{ player.goalsThisSeason }}</span>
            <span class="topscorer-num text-muted">{{ player.totalGoals }}</span>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="text-center text-muted">Club niet gevonden.</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const props = defineProps({
  id: {
    type: String,
    required: true,
  },
})

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const data = ref(null)
const loading = ref(true)
const activeTab = ref('programma')

const clubId = computed(() => parseInt(props.id))

const isFavorite = computed(() => favoritesStore.isClubFavorite(clubId.value))

function toggleFavorite() {
  if (!data.value) return
  if (isFavorite.value) {
    favoritesStore.removeClub(clubId.value)
  } else {
    favoritesStore.addClub({
      id: data.value.club.id,
      name: data.value.club.name,
      logo: data.value.club.logo,
      competitionId: data.value.club.competitionId,
      competitionName: data.value.club.competitionName,
    })
  }
}

function formatDate(dateStr) {
  const today = new Date().toISOString().slice(0, 10)
  if (dateStr === today) return 'Vandaag'
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' })
}

function formatDateShort(dateStr) {
  const today = new Date().toISOString().slice(0, 10)
  if (dateStr === today) return 'Vandaag'
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

// Flatten programma groups into a single sorted list
const allProgramma = computed(() => {
  if (!data.value?.programma) return []
  return data.value.programma.flatMap(g => g.matches.map(m => ({ ...m, date: g.date })))
})

const allUitslagen = computed(() => {
  if (!data.value?.uitslagen) return []
  return data.value.uitslagen.flatMap(g => g.matches.map(m => ({ ...m, date: g.date })))
})

function statusLabel(status) {
  return { live: 'Live', halftime: 'Rust', ended: 'Afgelopen', scheduled: 'Gepland' }[status] || status
}

// Compact stand grid: only 5 columns (no detailed toggle needed - use competition page for that)
// Highlight current club's row

onMounted(async () => {
  try {
    data.value = await api.getClubTeam(props.id)
  } catch (e) {
    data.value = null
  } finally {
    loading.value = false
  }
})
</script>
