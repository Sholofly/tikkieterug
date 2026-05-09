<template>
  <div class="page-header">
    <h1>Competities</h1>
  </div>

  <!-- Afdeling pills -->
  <div class="afd-scroll">
    <button
      v-for="afd in afdelingen"
      :key="afd.afdelingId"
      class="afd-pill"
      :class="{ active: selectedAfdelingId === afd.afdelingId }"
      @click="selectAfdeling(afd)"
    >
      {{ afd.afdelingName }}
    </button>
  </div>

  <div class="content">
    <div v-if="loading" class="loading">Laden...</div>

    <template v-else-if="selectedAfdeling">
      <div v-for="group in groupedCompetitions" :key="group.label || 'all'" style="margin-top: 10px;">
        <div
          v-if="group.label"
          class="text-xs text-muted font-bold"
          style="margin-bottom: 4px; text-transform: uppercase; letter-spacing: 0.5px; padding: 0 4px;"
        >
          {{ group.label }}
        </div>
        <div class="comp-grid">
          <div
            v-for="comp in group.competitions"
            :key="comp.id"
            class="comp-cell"
            @click="router.push(`/competition/${comp.id}`)"
          >
            <span class="comp-name">{{ comp.name }}</span>
            <button
              class="fav-btn"
              :class="{ 'fav-active': favoritesStore.isCompetitionFavorite(comp.id) }"
              @click.stop="toggleFavorite(comp)"
            >
              {{ favoritesStore.isCompetitionFavorite(comp.id) ? '★' : '☆' }}
            </button>
          </div>
        </div>
      </div>

      <div v-if="selectedAfdeling.competitions.length === 0" style="margin-top: 16px;">
        <div class="card text-muted text-sm" style="text-align: center;">
          Geen competities beschikbaar voor deze afdeling.
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const loading = ref(false)
const afdelingen = ref([])
const storedAfdeling = localStorage.getItem('competities-afdeling')
const selectedAfdelingId = ref(storedAfdeling ? parseInt(storedAfdeling, 10) : null)

const selectedAfdeling = computed(() =>
  afdelingen.value.find(a => a.afdelingId === selectedAfdelingId.value) ?? null
)

const groupedCompetitions = computed(() => {
  if (!selectedAfdeling.value) return []
  const comps = selectedAfdeling.value.competitions
  const speeldagen = new Set(comps.map(c => c.speeldag).filter(Boolean))
  const hasMultiple = speeldagen.size > 1

  if (!hasMultiple) {
    return [{ label: null, competitions: comps }]
  }

  const order = ['Zaterdag', 'Zondag']
  const groups = []
  for (const dag of order) {
    const filtered = comps.filter(c => c.speeldag === dag)
    if (filtered.length > 0) {
      groups.push({ label: dag, competitions: filtered })
    }
  }
  // Any remaining speeldagen not in order
  const remaining = comps.filter(c => !order.includes(c.speeldag))
  if (remaining.length > 0) {
    groups.push({ label: null, competitions: remaining })
  }
  return groups
})

function selectAfdeling(afd) {
  selectedAfdelingId.value = afd.afdelingId
  localStorage.setItem('competities-afdeling', afd.afdelingId)
}

function toggleFavorite(comp) {
  if (favoritesStore.isCompetitionFavorite(comp.id)) {
    favoritesStore.removeCompetition(comp.id)
  } else {
    favoritesStore.addCompetition({ id: comp.id, name: comp.name })
  }
}

onMounted(async () => {
  loading.value = true
  try {
    afdelingen.value = await api.getCompetitions()
    if (afdelingen.value.length > 0 && !afdelingen.value.find(a => a.afdelingId === selectedAfdelingId.value)) {
      selectedAfdelingId.value = afdelingen.value[0].afdelingId
    }
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.afd-scroll {
  display: flex;
  gap: 6px;
  overflow-x: auto;
  padding: 12px 16px 12px;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
  max-width: 100%;
}

.afd-scroll::-webkit-scrollbar {
  display: none;
}

.afd-pill {
  padding: 6px 14px;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
  cursor: pointer;
  border: 1px solid var(--border);
  background: var(--bg-card);
  color: var(--text-secondary);
  transition: background 0.15s, color 0.15s, border-color 0.15s;
  flex-shrink: 0;
}

.afd-pill.active {
  background: var(--accent);
  color: var(--bg-primary);
  border-color: var(--accent);
}

.comp-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
}

.comp-cell {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 4px;
  background: var(--bg-card);
  border-radius: var(--radius);
  padding: 8px 10px;
  cursor: pointer;
  min-width: 0;
}

.comp-name {
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}

.fav-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 0.95rem;
  color: var(--text-secondary);
  padding: 0 2px;
  transition: color 0.15s;
  flex-shrink: 0;
  line-height: 1;
}

.fav-btn.fav-active {
  color: var(--accent);
}

.fav-btn:hover {
  color: var(--accent);
}
</style>
