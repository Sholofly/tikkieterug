<template>
  <div class="content">
    <div v-if="loading" class="loading text-center mt-4">Laden...</div>

    <div v-else-if="club" class="card">
      <div class="text-center mb-4">
        <img
          v-if="club.logo"
          :src="club.logo"
          :alt="club.name"
          class="club-logo-lg"
        />
      </div>

      <div class="flex items-center justify-between mb-4">
        <h1 class="font-bold">{{ club.name }}</h1>
        <button
          class="btn"
          :class="isFavorite ? 'btn-danger' : 'btn-primary'"
          @click="toggleFavorite"
        >
          {{ isFavorite ? '★ Verwijder favoriet' : '☆ Voeg toe aan favorieten' }}
        </button>
      </div>

      <div class="flex gap-3 mt-2">
        <div>
          <span class="text-muted text-sm">Speeldag</span>
          <p class="font-bold">{{ club.speeldag ?? '—' }}</p>
        </div>
        <div>
          <span class="text-muted text-sm">Status</span>
          <p class="font-bold">{{ club.isActive ? 'Actief' : 'Inactief' }}</p>
        </div>
        <div>
          <span class="text-muted text-sm">Afdeling</span>
          <p class="font-bold">{{ afdelingName }}</p>
        </div>
      </div>

      <div v-if="club.competitionId" class="mt-4">
        <router-link
          :to="`/competition/${club.competitionId}`"
          class="btn btn-outline"
        >
          Bekijk competitie
        </router-link>
      </div>
    </div>

    <div v-else class="text-center mt-4 text-muted">Club niet gevonden.</div>
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

const club = ref(null)
const loading = ref(true)

const AFDELING_MAP = {
  0: 'BVO',
  1: 'Landelijk',
  2: 'Oost',
  3: 'Noord',
  4: 'West',
  5: 'Dames',
  6: 'Zuid 1',
  7: 'Zuid 2',
}

const afdelingName = computed(() => {
  if (club.value == null) return '—'
  return AFDELING_MAP[club.value.afdelingId] ?? '—'
})

const isFavorite = computed(() => favoritesStore.isClubFavorite(props.id))

function toggleFavorite() {
  if (isFavorite.value) {
    favoritesStore.removeClub(props.id)
  } else {
    favoritesStore.addClub(club.value)
  }
}

onMounted(async () => {
  try {
    club.value = await api.getClub(props.id)
  } finally {
    loading.value = false
  }
})
</script>
