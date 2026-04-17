let _base = null

async function getBase() {
  if (_base !== null) return _base
  try {
    const res = await fetch('/config.json')
    const config = await res.json()
    _base = config.apiUrl || (import.meta.env.DEV ? '/api' : '')
  } catch {
    _base = import.meta.env.DEV ? '/api' : ''
  }
  return _base
}

async function fetchJson(path) {
  const base = await getBase()
  const res = await fetch(`${base}${path}`)
  if (!res.ok) throw new Error(`API ${res.status}: ${path}`)
  return res.json()
}

export function useApi() {
  return {
    searchClubs: (search, active = true) =>
      fetchJson(`/clubs?search=${encodeURIComponent(search)}&active=${active}`),

    getClub: (id) => fetchJson(`/clubs/${id}`),

    getClubTeam: (id) => fetchJson(`/clubs/${id}/team`),

    getStandings: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/stand`),

    getResults: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/uitslagen`),

    getPeriodestand: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/periodestand`),

    getMatch: (matchId) => fetchJson(`/matches/${matchId}`),

    getCompetitionName: (competitionId) => fetchJson(`/competitions/${competitionId}/naam`),

    getFixtures: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/programma`),

    getCompetitionTopscorers: (competitionId) =>
      fetchJson(`/competitions/${competitionId}/topscorers`),

    getClubInfo: (clubId) => fetchJson(`/clubs/${clubId}/info`),

    getClubProgramma: (clubId) => fetchJson(`/clubs/${clubId}/programma`),

    getMatchHistory: (homeId, awayId, homeCat, awayCat) =>
      fetchJson(`/matches/history?home=${homeId}&away=${awayId}&homeCat=${encodeURIComponent(homeCat)}&awayCat=${encodeURIComponent(awayCat)}`),
  }
}
