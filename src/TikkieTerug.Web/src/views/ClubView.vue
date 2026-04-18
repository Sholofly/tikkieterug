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
          >{{ isFavorite ? '❤️ Volgt' : '🤍 Volgen' }}</button>
        </div>
      </div>

      <!-- Tab selector -->
      <select class="tab-select" v-model="activeTab">
        <option value="programma">📅 Programma</option>
        <option value="uitslagen">⚽ Uitslagen</option>
        <option value="stand">📊 Stand</option>
        <option value="topscorers">🥇 Topscorers</option>
        <option value="info">ℹ️ Info</option>
      </select>

      <!-- Programma -->
      <div v-if="activeTab === 'programma'">
        <div v-if="data.programma.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen aankomende wedstrijden.</div>
        <div v-else class="card">
          <div
            v-for="match in allProgramma"
            :key="match.matchId"
            class="fixture-row-wrap"
            @click="router.push(`/match/${match.matchId}`)"
          >
            <div class="text-xs text-muted" style="text-align: center; padding-top: 4px;">{{ formatDateShort(match.date) }}</div>
            <div class="fixture-row" style="cursor: pointer;">
              <div class="fixture-home">
                <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
                <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
              </div>
              <div class="fixture-center">
                <span v-if="!match.status || match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                <span v-if="match.status && match.date === today" class="badge" :class="{ 'badge-live': match.status === 'live', 'badge-halftime': match.status === 'halftime', 'badge-ended': match.status === 'ended', 'badge-scheduled': match.status === 'scheduled' }">{{ statusLabel(match.status) }}</span>
              </div>
              <div class="fixture-away">
                <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
              </div>
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
            class="fixture-row-wrap"
            @click="router.push(`/match/${match.matchId}`)"
          >
            <div class="text-xs text-muted" style="text-align: center; padding-top: 4px;">{{ formatDateShort(match.date) }}</div>
            <div class="fixture-row" style="cursor: pointer;">
              <div class="fixture-home">
                <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
                <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
              </div>
              <div class="fixture-center">
                <span v-if="match.status === 'scheduled'" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
                <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
                <span v-if="match.status !== 'scheduled' && match.date === today" class="badge" :class="{ 'badge-live': match.status === 'live', 'badge-halftime': match.status === 'halftime', 'badge-ended': match.status === 'ended' }">{{ statusLabel(match.status) }}</span>
              </div>
              <div class="fixture-away">
                <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
                <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Stand -->
      <div v-if="activeTab === 'stand'">
        <div v-if="data.stand.length === 0" class="text-muted text-sm" style="padding: 12px 0;">Geen stand beschikbaar.</div>
        <div v-else>
          <div class="card">
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
              style="cursor: pointer;"
              @click="router.push(`/club/${row.clubId}`)"
            >
              <span class="standings-pos">{{ row.position }}</span>
              <span><img :src="row.logo" class="club-logo-sm" :alt="row.club" /></span>
              <span class="standings-name">{{ row.club }}<span v-if="row.penaltyPoints > 0" class="penalty-marker">*</span></span>
              <span class="standings-num">{{ row.played }}</span>
              <span class="standings-pts">{{ row.points }}</span>
            </div>
          </div>
          <div v-if="data.stand.some(r => r.penaltyPoints > 0)" class="penalty-note">
            * Punten in mindering:
            <span v-for="row in data.stand.filter(r => r.penaltyPoints > 0)" :key="row.clubId" class="penalty-item">
              {{ row.club }} (−{{ row.penaltyPoints }})
            </span>
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

      <!-- Info -->
      <div v-if="activeTab === 'info'">
        <div v-if="infoLoading" class="loading text-center">Laden...</div>
        <div v-else-if="!clubInfo" class="text-muted text-sm" style="padding: 12px 0;">Geen clubinformatie beschikbaar.</div>
        <div v-else class="card">
          <div v-if="clubInfo.fullName" class="info-row">
            <span class="info-label">Naam</span>
            <span>{{ clubInfo.fullName }}</span>
          </div>
          <div v-if="clubInfo.founded" class="info-row">
            <span class="info-label">Opgericht</span>
            <span>{{ clubInfo.founded }}</span>
          </div>
          <div v-if="clubInfo.fieldName" class="info-row">
            <span class="info-label">Sportpark</span>
            <span>{{ clubInfo.fieldName }}</span>
          </div>
          <div v-if="clubInfo.address || clubInfo.postalCode || clubInfo.city" class="info-row">
            <span class="info-label">Adres</span>
            <span>
              <span v-if="clubInfo.address">{{ clubInfo.address }}</span>
              <br v-if="clubInfo.address && (clubInfo.postalCode || clubInfo.city)" />
              <span v-if="clubInfo.postalCode || clubInfo.city">{{ [clubInfo.postalCode, clubInfo.city].filter(Boolean).join(' ') }}</span>
              <a
                v-if="clubInfo.latitude && clubInfo.longitude"
                :href="`https://www.google.com/maps/search/?api=1&query=${clubInfo.latitude},${clubInfo.longitude}`"
                target="_blank"
                rel="noopener"
                class="info-map-link"
                @click.stop
              >📍 Kaart</a>
            </span>
          </div>
          <div v-if="clubInfo.phone" class="info-row">
            <span class="info-label">Telefoon</span>
            <a :href="`tel:${clubInfo.phone}`">{{ clubInfo.phone }}</a>
          </div>
          <div v-if="clubInfo.website" class="info-row">
            <span class="info-label">Website</span>
            <a :href="clubInfo.website.startsWith('http') ? clubInfo.website : `https://${clubInfo.website}`" target="_blank" rel="noopener">{{ clubInfo.website }}</a>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="text-center text-muted">Club niet gevonden.</div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const props = defineProps({
  id: {
    type: String,
    required: true,
  },
})

const router = useRouter()
const route = useRoute()
const api = useApi()
const favoritesStore = useFavoritesStore()

const validTabs = ['programma', 'uitslagen', 'stand', 'topscorers', 'info']
const data = ref(null)
const loading = ref(true)
const activeTab = ref(validTabs.includes(route.query.tab) ? route.query.tab : 'programma')
const clubInfo = ref(null)
const infoLoading = ref(false)
const infoLoaded = ref(false)
const today = new Date().toISOString().slice(0, 10)

// Sync tab to URL and lazy-load info
watch(activeTab, async (tab) => {
  router.replace({ query: { ...route.query, tab } })
  if (tab === 'info' && !infoLoaded.value) {
    infoLoading.value = true
    try {
      clubInfo.value = await api.getClubInfo(props.id)
    } catch {
      clubInfo.value = null
    } finally {
      infoLoading.value = false
      infoLoaded.value = true
    }
  }
})

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

async function fetchClub(resetTab = true) {
  loading.value = true
  data.value = null
  clubInfo.value = null
  infoLoaded.value = false
  if (resetTab) activeTab.value = 'programma'
  try {
    data.value = await api.getClubTeam(props.id)
  } catch (e) {
    data.value = null
  } finally {
    loading.value = false
  }
}

watch(() => props.id, () => fetchClub(true))

onMounted(() => fetchClub(false))
</script>
