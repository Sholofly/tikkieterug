const BASE = import.meta.env.DEV ? '/api' : ''

async function fetchJson(path) {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(`API ${res.status}: ${path}`)
  return res.json()
}

export function useApi() {
  return {
    searchClubs: (search, active = true) =>
      fetchJson(`/clubs?search=${encodeURIComponent(search)}&active=${active}`),

    getClub: (id) => fetchJson(`/clubs/${id}`),

    getStandings: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/stand`),

    getResults: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/uitslagen`),

    getPeriodestand: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/periodestand`),

    getMatch: (matchId) => fetchJson(`/matches/${matchId}`),

    getCompetitionName: (competitionId) => fetchJson(`/competitions/${competitionId}/naam`),
  }
}
