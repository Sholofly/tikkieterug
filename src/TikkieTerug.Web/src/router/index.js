import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'home',
    component: () => import('../views/HomeView.vue')
  },
  {
    path: '/nieuws',
    name: 'nieuws',
    component: () => import('../views/NieuwsView.vue')
  },
  {
    path: '/competities',
    name: 'competities',
    component: () => import('../views/CompetitiesView.vue')
  },
  {
    path: '/volgt',
    name: 'favorites',
    component: () => import('../views/FavoritesView.vue')
  },
  {
    path: '/favorites',
    redirect: '/volgt'
  },
  {
    path: '/meer',
    name: 'meer',
    component: () => import('../views/MeerView.vue')
  },
  {
    path: '/competition/:id',
    name: 'competition',
    component: () => import('../views/CompetitionView.vue'),
    props: true
  },
  {
    path: '/match/:id',
    name: 'match',
    component: () => import('../views/MatchView.vue'),
    props: true
  },
  {
    path: '/club/:id',
    name: 'club',
    component: () => import('../views/ClubView.vue'),
    props: true
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
