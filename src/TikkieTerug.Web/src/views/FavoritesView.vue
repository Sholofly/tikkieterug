<template>
  <div>
    <div class="page-header">
      <h1>Favorieten</h1>
    </div>

    <div class="search-box">
      <div class="search-container">
        <input
          v-model="searchTerm"
          class="search-input"
          type="text"
          placeholder="Zoek een club..."
          autocomplete="off"
          @focus="isDropdownOpen = true"
          @blur="handleBlur"
        />
        
        <!-- Search dropdown -->
        <div v-if="searchTerm.length >= 2 && isDropdownOpen" class="search-dropdown">
          <div v-if="isSearching" class="dropdown-item text-center text-muted py-3">Zoeken...</div>
          <div v-else-if="searchResults.length === 0 && !isSearching" class="dropdown-item text-center text-muted py-3">
            Geen clubs gevonden voor "{{ searchTerm }}".
          </div>
          <div v-else class="dropdown-list">
            <div
              v-for="club in searchResults"
              :key="club.id"
              class="dropdown-item"
              style="cursor: pointer;"
              @click="router.push(`/club/${club.id}`)"
            >
              <div class="flex items-center justify-between gap-3">
                <div class="flex items-center gap-2 overflow-hidden">
                  <img
                    v-if="club.logo"
                    :src="club.logo"
                    :alt="club.name"
                    class="club-logo-sm shrink-0"
                  />
                  <div class="flex flex-col min-w-0">
                    <span class="font-bold truncate">{{ club.name }}</span>
                    <span v-if="club.competitionName" class="text-xs text-muted truncate">{{ club.competitionName }}</span>
                  </div>
                </div>
                <button
                  v-if="!favoritesStore.isClubFavorite(club.id)"
                  class="btn btn-sm btn-primary shrink-0"
                  @mousedown.prevent
                  @click.stop="addClub(club)"
                >
                  ☆
                </button>
                <span v-else class="btn btn-sm btn-outline shrink-0" style="pointer-events: none;">★</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="content">
      <!-- Favorite clubs -->
      <section class="mt-4">
        <h2 class="font-bold mb-2">Mijn clubs</h2>
        <div v-if="favoritesStore.clubs.length === 0" class="empty text-muted text-sm">
          Nog geen favoriete clubs toegevoegd.
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
        <h2 class="font-bold mb-2">Mijn competities</h2>
        <div v-if="favoritesStore.competitions.length === 0" class="empty text-muted text-sm">
          Nog geen favoriete competities toegevoegd.
        </div>
        <div
          v-for="competition in favoritesStore.competitions"
          :key="competition.id"
          class="card mb-2"
        >
          <div class="flex items-center justify-between gap-3">
            <span
              class="font-bold truncate"
              style="cursor: pointer;"
              @click="router.push(`/competition/${competition.id}`)"
            >
              {{ competition.name }}
            </span>
            <button
              class="btn btn-sm btn-danger"
              @click="favoritesStore.removeCompetition(competition.id)"
            >
              Verwijderen
            </button>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../composables/useApi.js'
import { useFavoritesStore } from '../stores/favorites.js'

const router = useRouter()
const api = useApi()
const favoritesStore = useFavoritesStore()

const searchTerm = ref('')
const searchResults = ref([])
const isSearching = ref(false)
const isDropdownOpen = ref(false)

function handleBlur() {
  setTimeout(() => {
    isDropdownOpen.value = false
  }, 200)
}

let debounceTimer = null
let searchCounter = 0

watch(searchTerm, (value) => {
  clearTimeout(debounceTimer)
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
      if (thisSearch !== searchCounter) return  // stale response, ignore
      searchResults.value = results
    } catch (e) {
      if (thisSearch !== searchCounter) return
      searchResults.value = []
    } finally {
      if (thisSearch === searchCounter) {
        isSearching.value = false
      }
    }
  }, 300)
})

function addClub(club) {
  favoritesStore.addClub({
    id: club.id,
    name: club.name,
    competitionId: club.competitionId,
    competitionName: club.competitionName,
    logo: club.logo,
  })
}
</script>
