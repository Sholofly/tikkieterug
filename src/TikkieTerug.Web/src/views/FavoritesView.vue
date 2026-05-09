<template>
  <div>
    <div class="page-header">
      <h1>Volgt</h1>
    </div>

    <div class="content">
      <!-- Favorite clubs -->
      <section class="mt-4">
        <div class="flex items-center justify-between mb-2">
          <h2 class="font-bold">Mijn clubs</h2>
          <button class="add-btn" @click="openSheet('clubs')">+</button>
        </div>
        <div v-if="favoritesStore.clubs.length === 0" class="card text-muted text-sm" style="text-align: center;">
          Nog geen clubs die je volgt.
        </div>
        <div
          v-for="club in favoritesStore.clubs"
          :key="club.id"
          class="card mb-2"
          style="cursor: pointer;"
          @click="router.push(`/club/${club.id}`)"
        >
          <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-2">
              <img
                v-if="club.logo"
                :src="club.logo"
                :alt="club.name"
                class="club-logo-sm"
              />
              <div class="flex flex-col">
                <span class="font-bold truncate">{{ club.name }}</span>
                <span v-if="club.competitionName" class="text-xs text-muted">{{ club.competitionName }}</span>
              </div>
            </div>
            <button
              class="btn btn-sm btn-danger"
              @click.stop="favoritesStore.removeClub(club.id)"
            >
              Verwijderen
            </button>
          </div>
        </div>
      </section>

      <!-- Favorite competitions -->
      <section class="mt-4">
        <div class="flex items-center justify-between mb-2">
          <h2 class="font-bold">Mijn competities</h2>
          <button class="add-btn" @click="openSheet('competitions')">+</button>
        </div>
        <div v-if="favoritesStore.competitions.length === 0" class="card text-muted text-sm" style="text-align: center;">
          Nog geen competities die je volgt.
        </div>
        <div
          v-for="competition in favoritesStore.competitions"
          :key="competition.id"
          class="card mb-2"
          style="cursor: pointer;"
          @click="router.push(`/competition/${competition.id}`)"
        >
          <div class="flex items-center justify-between gap-3">
            <div class="flex flex-col min-w-0">
              <span class="font-bold truncate">{{ competition.name }}</span>
            </div>
            <button
              class="btn btn-sm btn-danger"
              @click.stop="favoritesStore.removeCompetition(competition.id)"
            >
              Verwijderen
            </button>
          </div>
        </div>
      </section>
    </div>

    <!-- Popup overlay -->
    <Teleport to="body">
      <Transition name="popup">
        <div v-if="sheetOpen" class="popup-backdrop" @click.self="closeSheet">
          <div class="popup-panel">
            <div class="popup-header">
              <h2 class="font-bold">{{ sheetMode === 'clubs' ? 'Club toevoegen' : 'Competitie toevoegen' }}</h2>
              <button class="popup-close" @click="closeSheet">&times;</button>
            </div>

            <div class="popup-search">
              <input
                ref="sheetInput"
                :value="searchTerm"
                class="search-input"
                type="search"
                :placeholder="sheetMode === 'clubs' ? 'Zoek een club...' : 'Zoek een competitie...'"
                autocomplete="off"
                @input="onSearchInput"
              />
            </div>

            <div class="popup-results">
              <!-- Club results -->
              <template v-if="sheetMode === 'clubs'">
                <div v-if="searchTerm.length < 2" class="text-muted text-sm" style="text-align: center; padding: 24px 0;">
                  Typ minimaal 2 letters om te zoeken.
                </div>
                <div v-else-if="isSearching" class="text-muted text-sm" style="text-align: center; padding: 24px 0;">
                  Zoeken...
                </div>
                <div v-else-if="searchResults.length === 0" class="text-muted text-sm" style="text-align: center; padding: 24px 0;">
                  Geen clubs gevonden voor "{{ searchTerm }}".
                </div>
                <div v-else>
                  <div
                    v-for="club in searchResults"
                    :key="club.id"
                    class="result-row"
                    @click="router.push(`/club/${club.id}`); closeSheet()"
                  >
                    <div class="flex items-center gap-2 min-w-0" style="flex: 1;">
                      <img v-if="club.logo" :src="club.logo" :alt="club.name" class="club-logo-sm shrink-0" />
                      <div class="flex flex-col min-w-0">
                        <span class="font-bold truncate">{{ club.name }}</span>
                        <span v-if="club.competitionName" class="text-xs text-muted truncate">{{ club.competitionName }}</span>
                      </div>
                    </div>
                    <button
                      v-if="!favoritesStore.isClubFavorite(club.id)"
                      class="btn btn-sm btn-primary shrink-0"
                      @click.stop="addClub(club)"
                    >+</button>
                    <span v-else class="text-xs text-muted shrink-0" style="padding: 6px;">Volgt</span>
                  </div>
                </div>
              </template>

              <!-- Competition results -->
              <template v-if="sheetMode === 'competitions'">
                <div v-if="searchTerm.length < 2" class="text-muted text-sm" style="text-align: center; padding: 24px 0;">
                  Typ minimaal 2 letters om te zoeken.
                </div>
                <div v-else-if="competitionResults.length === 0" class="text-muted text-sm" style="text-align: center; padding: 24px 0;">
                  Geen competities gevonden voor "{{ searchTerm }}".
                </div>
                <div v-else>
                  <div
                    v-for="comp in competitionResults"
                    :key="comp.id"
                    class="result-row"
                    @click="router.push(`/competition/${comp.id}`); closeSheet()"
                  >
                    <div class="flex flex-col min-w-0" style="flex: 1;">
                      <span class="font-bold truncate">{{ comp.name }}</span>
                      <span class="text-xs text-muted truncate">{{ comp.afdelingName }}<template v-if="comp.speeldag"> · {{ comp.speeldag }}</template></span>
                    </div>
                    <button
                      v-if="!favoritesStore.isCompetitionFavorite(comp.id)"
                      class="btn btn-sm btn-primary shrink-0"
                      @click.stop="addCompetition(comp)"
                    >+</button>
                    <span v-else class="text-xs text-muted shrink-0" style="padding: 6px;">Volgt</span>
                  </div>
                </div>
              </template>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const sheetOpen = ref(false)
const sheetMode = ref('clubs')
const sheetInput = ref(null)

const searchTerm = ref('')
const searchResults = ref([])
const isSearching = ref(false)
const allCompetitions = ref([])

onMounted(async () => {
  try {
    const groups = await api.getCompetitions()
    const flat = []
    for (const group of groups) {
      for (const comp of group.competitions) {
        flat.push({ id: comp.id, name: comp.name, speeldag: comp.speeldag, afdelingName: group.afdelingName })
      }
    }
    allCompetitions.value = flat
  } catch {
    allCompetitions.value = []
  }
})

const competitionResults = computed(() => {
  if (searchTerm.value.length < 2) return []
  const q = searchTerm.value.toLowerCase()
  return allCompetitions.value.filter(c =>
    c.name.toLowerCase().includes(q) || c.afdelingName.toLowerCase().includes(q)
  )
})

function openSheet(mode) {
  sheetMode.value = mode
  searchTerm.value = ''
  searchResults.value = []
  isSearching.value = false
  sheetOpen.value = true
  nextTick(() => {
    sheetInput.value?.focus()
  })
}

function closeSheet() {
  sheetOpen.value = false
  searchTerm.value = ''
  searchResults.value = []
}

let debounceTimer = null
let searchCounter = 0

function onSearchInput(e) {
  const value = e.target.value
  searchTerm.value = value
  clearTimeout(debounceTimer)

  if (sheetMode.value === 'competitions') return

  searchResults.value = []
  if (value.length < 2) {
    isSearching.value = false
    return
  }

  isSearching.value = true
  const thisSearch = ++searchCounter
  debounceTimer = setTimeout(async () => {
    try {
      const results = await api.searchClubs(value)
      if (thisSearch !== searchCounter) return
      searchResults.value = results
    } catch {
      if (thisSearch !== searchCounter) return
      searchResults.value = []
    } finally {
      if (thisSearch === searchCounter) {
        isSearching.value = false
      }
    }
  }, 300)
}

function addClub(club) {
  favoritesStore.addClub({
    id: club.id,
    name: club.name,
    competitionId: club.competitionId,
    competitionName: club.competitionName,
    logo: club.logo,
  })
}

function addCompetition(comp) {
  favoritesStore.addCompetition({ id: comp.id, name: comp.name })
}
</script>

<style scoped>
.add-btn {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: none;
  background: var(--accent);
  color: var(--bg-primary);
  font-size: 1.3rem;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: transform 0.15s;
}
.add-btn:active {
  transform: scale(0.9);
}

/* Popup */
.popup-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 2000;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 80px 16px 24px;
}
.popup-panel {
  background: var(--bg-primary);
  width: 100%;
  max-width: 420px;
  height: min(500px, calc(100dvh - 120px));
  border-radius: var(--radius);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.3);
}
.popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px 8px;
}
.popup-close {
  background: none;
  border: none;
  color: var(--text-secondary);
  font-size: 1.6rem;
  cursor: pointer;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  line-height: 1;
}
.popup-close:hover {
  background: var(--bg-card);
}
.popup-search {
  padding: 8px 20px 12px;
}
.popup-results {
  flex: 1;
  overflow-y: auto;
  padding: 0 20px 20px;
  -webkit-overflow-scrolling: touch;
}

/* Transition */
.popup-enter-active,
.popup-leave-active {
  transition: opacity 0.15s ease;
}
.popup-enter-from,
.popup-leave-to {
  opacity: 0;
}
.sheet-panel {
  background: var(--bg-primary);
  width: 100%;
  max-width: 480px;
  max-height: 85dvh;
  border-radius: 16px 16px 0 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sheet-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px 8px;
}
.sheet-close {
  background: none;
  border: none;
  color: var(--text-secondary);
  font-size: 1.6rem;
  cursor: pointer;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  line-height: 1;
}
.sheet-close:hover {
  background: var(--bg-card);
}
.sheet-search {
  padding: 8px 20px 12px;
}
.sheet-results {
  flex: 1;
  overflow-y: auto;
  padding: 0 20px 20px;
  -webkit-overflow-scrolling: touch;
}
.result-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
}
.result-row:last-child {
  border-bottom: none;
}

/* Transitions */
.sheet-enter-active,
.sheet-leave-active {
  transition: opacity 0.2s ease;
}
.sheet-enter-active .sheet-panel,
.sheet-leave-active .sheet-panel {
  transition: transform 0.2s ease, opacity 0.2s ease;
}
.sheet-enter-from,
.sheet-leave-to {
  opacity: 0;
}
.sheet-enter-from .sheet-panel {
  transform: scale(0.97);
  opacity: 0;
}
.sheet-leave-to .sheet-panel {
  transform: scale(0.97);
  opacity: 0;
}
</style>
