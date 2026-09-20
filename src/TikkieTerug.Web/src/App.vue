<script setup>
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const topLevelRoutes = ['home', 'competities', 'favorites', 'meer']
const showBack = () => !topLevelRoutes.includes(route.name)
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button v-if="showBack()" class="header-back" @click="router.back()" aria-label="Terug">&#8592;</button>
    </div>
    <router-link to="/" class="app-brand">
      <svg viewBox="0 0 512 512" class="brand-logo" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <rect width="512" height="512" rx="112" fill="#1e3a5f" />
        <text x="256" y="322" text-anchor="middle" fill="#f97316" font-family="Arial, sans-serif" font-size="230" font-weight="900" letter-spacing="-28">TT</text>
        <g transform="translate(391 364)">
          <circle r="48" fill="#fff" stroke="#1e3a5f" stroke-width="4" />
          <path d="M0-19 18-6 11 15H-11L-18-6Z" fill="#1e3a5f" />
          <path d="M0-19V-43M18-6l21-13M11 15l13 22M-11 15l-13 22M-18-6l-21-13" fill="none" stroke="#1e3a5f" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M0-43 20-35 39-19M39-19 42 4 24 24M24 24 0 37-24 24M-24 24-42 4-39-19M-39-19-20-35 0-43" fill="none" stroke="#1e3a5f" stroke-width="5" stroke-linejoin="round" />
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
