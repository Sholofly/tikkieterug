<script setup>
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const topLevelRoutes = ['home', 'nieuws', 'competities', 'favorites', 'meer']
const showBack = () => !topLevelRoutes.includes(route.name)
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button v-if="showBack()" class="header-back" @click="router.back()" aria-label="Terug">&#8592;</button>
    </div>
    <router-link to="/" class="app-brand">
      TikkieTerug
    </router-link>
    <div class="header-right"></div>
  </header>

  <main class="main-content">
    <router-view />
  </main>

  <nav class="bottom-nav">
    <router-link to="/" class="bottom-nav-item" :class="{ active: route.name === 'home' }">
      <span class="bottom-nav-icon">⚽</span>
      <span class="bottom-nav-label">Wedstrijden</span>
    </router-link>
    <router-link to="/nieuws" class="bottom-nav-item" :class="{ active: route.name === 'nieuws' }">
      <span class="bottom-nav-icon">📰</span>
      <span class="bottom-nav-label">Nieuws</span>
    </router-link>
    <router-link to="/competities" class="bottom-nav-item" :class="{ active: route.name === 'competities' }">
      <span class="bottom-nav-icon">🏆</span>
      <span class="bottom-nav-label">Competities</span>
    </router-link>
    <router-link to="/volgt" class="bottom-nav-item" :class="{ active: route.name === 'favorites' }">
      <span class="bottom-nav-icon">❤️</span>
      <span class="bottom-nav-label">Volgt</span>
    </router-link>
    <router-link to="/meer" class="bottom-nav-item" :class="{ active: route.name === 'meer' }">
      <span class="bottom-nav-icon">☰</span>
      <span class="bottom-nav-label">Meer</span>
    </router-link>
  </nav>
</template>
