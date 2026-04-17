import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const STORAGE_KEY = 'tikkieterug_favorites'

function load() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY)) || { clubs: [], competitions: [] }
  } catch {
    return { clubs: [], competitions: [] }
  }
}

function save(data) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
}

export const useFavoritesStore = defineStore('favorites', () => {
  const stored = load()
  // clubs: [{ id, name, competitionId, logo }]
  const clubs = ref(stored.clubs)
  // competitions: [{ id, name, clubId? }]  (clubId if added via a club)
  const competitions = ref(stored.competitions)

  watch([clubs, competitions], () => {
    save({ clubs: clubs.value, competitions: competitions.value })
  }, { deep: true })

  function addClub(club) {
    if (!clubs.value.find(c => c.id === club.id)) {
      clubs.value.push({
        id: club.id,
        name: club.name,
        competitionId: club.competitionId,
        competitionName: club.competitionName,
        logo: club.logo
      })
      // Auto-add competition if club has one
      if (club.competitionId) {
        addCompetition({ id: club.competitionId, name: club.competitionName || `Competitie ${club.competitionId}` })
      }
    }
  }

  function removeClub(id) {
    clubs.value = clubs.value.filter(c => c.id !== id)
  }

  function addCompetition(comp) {
    if (!competitions.value.find(c => String(c.id) === String(comp.id))) {
      competitions.value.push({ id: comp.id, name: comp.name })
    }
  }

  function removeCompetition(id) {
    competitions.value = competitions.value.filter(c => c.id !== id)
  }

  function isClubFavorite(id) {
    return clubs.value.some(c => c.id === id)
  }

  function isCompetitionFavorite(id) {
    return competitions.value.some(c => c.id === id)
  }

  const hasFavorites = () => clubs.value.length > 0 || competitions.value.length > 0

  return {
    clubs, competitions,
    addClub, removeClub,
    addCompetition, removeCompetition,
    isClubFavorite, isCompetitionFavorite,
    hasFavorites
  }
})
