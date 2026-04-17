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
      <svg viewBox="0 0 512 512" class="brand-logo" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="512" height="512" rx="112" fill="#1e3a5f" />
        <rect x="24" y="24" width="464" height="464" rx="88" fill="none" stroke="#f97316" stroke-width="24" />
        <path d="M 230 140 H 130 L 60 210 L 130 280 H 170 L 120 230 H 230 V 420 H 290 V 230 H 310 V 140 H 230 Z" fill="#f97316" />
        <path d="M 330 140 H 450 V 230 H 410 V 420 H 350 V 230 H 330 V 140 Z" fill="#f97316" />
        <g transform="translate(380, 360)">
          <circle cx="0" cy="0" r="42" fill="#ffffff" />
          <path d="M 0 -18 L -17 -6 L -10 14 L 10 14 L 17 -6 Z" fill="#111827" />
          <path d="M -17 -6 L -33 -18 M -10 14 L -20 32 M 10 14 L 20 32 M 17 -6 L 33 -18 M 0 -18 L 0 -36" stroke="#111827" stroke-width="5" stroke-linecap="round" />
          <path d="M -33 -18 L -42 -10 M -20 32 L -10 40 M 20 32 L 10 40 M 33 -18 L 42 -10 M 0 -36 L -12 -40 M 0 -36 L 12 -40" stroke="#111827" stroke-width="5" stroke-linecap="round" />
        </g>
      </svg>
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
