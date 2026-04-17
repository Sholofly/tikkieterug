import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'home',
    component: () => import('../views/HomeView.vue')
  },
  {
    path: '/favorites',
    name: 'favorites',
    component: () => import('../views/FavoritesView.vue')
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
