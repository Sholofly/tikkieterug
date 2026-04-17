<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useThemeStore } from './stores/theme'

const route = useRoute()
const router = useRouter()
const menuOpen = ref(false)

const themeStore = useThemeStore()

const topLevelRoutes = ['home', 'favorites']
const showBack = () => !topLevelRoutes.includes(route.name)

const toggleMenu = () => {
  menuOpen.value = !menuOpen.value
}

const closeMenu = () => {
  menuOpen.value = false
}
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button v-if="showBack()" class="header-back" @click="router.back()" aria-label="Terug">&#8592;</button>
    </div>
    <router-link to="/" class="app-brand" @click="closeMenu">
      TikkieTerug
    </router-link>
    <div class="header-right">
      <button class="menu-toggle" @click="toggleMenu" aria-label="Toggle menu">
        &#9776;
      </button>
    </div>
  </header>

  <div class="drawer-overlay" :class="{ 'is-open': menuOpen }" @click="closeMenu"></div>

  <nav class="side-drawer" :class="{ 'is-open': menuOpen }">
    <div class="drawer-header">
      <span class="drawer-title">Menu</span>
      <button class="close-btn" @click="closeMenu" aria-label="Close menu">&times;</button>
    </div>
    <div class="drawer-links">
      <router-link to="/" class="drawer-item" :class="{ active: route.name === 'home' }" @click="closeMenu">
        <span class="drawer-icon">&#9917;</span>
        <span>Live</span>
      </router-link>
      <router-link to="/favorites" class="drawer-item" :class="{ active: route.name === 'favorites' }" @click="closeMenu">
        <span class="drawer-icon">&#9734;</span>
        <span>Favorieten</span>
      </router-link>
    </div>

    <div class="drawer-divider"></div>

    <div class="theme-selector">
      <span class="theme-label">Thema</span>
      <div class="theme-toggle">
        <button class="theme-btn" :class="{ active: themeStore.theme === 'light' }" @click="themeStore.setTheme('light')" aria-label="Licht thema">
          <span>☀️</span> Licht
        </button>
        <button class="theme-btn" :class="{ active: themeStore.theme === 'dark' }" @click="themeStore.setTheme('dark')" aria-label="Donker thema">
          <span>🌙</span> Donker
        </button>
        <button class="theme-btn" :class="{ active: themeStore.theme === 'system' }" @click="themeStore.setTheme('system')" aria-label="Systeem thema">
          <span>⚙️</span> Systeem
        </button>
      </div>
    </div>
  </nav>

  <main class="main-content">
    <router-view />
  </main>
</template>
