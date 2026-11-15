<template>
  <div
    class="fixture-row-wrap"
    @click="router.push(`/match/${match.matchId}?source=${match.source || 1}`)"
  >
    <div class="fixture-row" style="cursor: pointer;">
      <div class="fixture-home">
        <span :class="{ 'font-bold': match.status === 'ended' && match.homeScore > match.awayScore }">{{ match.homeClub }}</span>
        <img :src="match.homeLogo" class="club-logo-sm" :alt="match.homeClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.homeClubId}`)" />
      </div>
      <div class="fixture-center">
        <span v-if="showTime" class="fixture-time" style="font-size: 0.85rem;">{{ match.time }}</span>
        <span v-else class="fixture-score" :class="{ 'text-live': match.status === 'live' }">{{ match.homeScore }} – {{ match.awayScore }}</span>
        <span
          v-if="match.status !== 'scheduled'"
          class="badge"
          :class="{
            'badge-live': match.status === 'live',
            'badge-halftime': match.status === 'halftime',
            'badge-ended': match.status === 'ended' || match.status === 'cancelled' || match.status === 'suspended',
          }"
        >{{ statusLabel }}</span>
      </div>
      <div class="fixture-away">
        <img :src="match.awayLogo" class="club-logo-sm" :alt="match.awayClub" style="cursor: pointer;" @click.stop="router.push(`/club/${match.awayClubId}`)" />
        <span :class="{ 'font-bold': match.status === 'ended' && match.awayScore > match.homeScore }">{{ match.awayClub }}</span>
      </div>
    </div>
    <div v-if="match.homeRedCards || match.awayRedCards" class="fixture-red-row">
      <div style="display: flex; justify-content: flex-end; gap: 2px; padding-right: 26px;">
        <span v-for="n in (match.homeRedCards || 0)" :key="'hr'+n" class="red-stripe"></span>
      </div>
      <div></div>
      <div style="display: flex; gap: 2px; padding-left: 26px;">
        <span v-for="n in (match.awayRedCards || 0)" :key="'ar'+n" class="red-stripe"></span>
      </div>
    </div>
    <div v-if="subtitle" class="fixture-match-type">{{ subtitle }}</div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'

const props = defineProps({
  match: { type: Object, required: true },
  // Also show the competition name next to the match type
  showCompetition: { type: Boolean, default: false },
})

const router = useRouter()

const LABELS = {
  live: 'Live',
  halftime: 'Rust',
  ended: 'Afgelopen',
  scheduled: 'Gepland',
  cancelled: 'Afgelast',
  suspended: 'Gestaakt',
}

const statusLabel = computed(() => LABELS[props.match.status] || props.match.status)
const showTime = computed(() => props.match.status === 'scheduled' || props.match.status === 'cancelled')
const subtitle = computed(() =>
  [props.match.matchType, props.showCompetition ? props.match._compName : null].filter(Boolean).join(' · ')
)
</script>
