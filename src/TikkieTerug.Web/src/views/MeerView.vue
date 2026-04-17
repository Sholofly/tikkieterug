<template>
  <div class="page-header">
    <h1>Instellingen</h1>
  </div>
  <div class="content">
    <div class="card">
      <div style="margin-bottom: 12px;">
        <span class="font-bold">Thema</span>
      </div>
      <div class="theme-toggle">
        <button class="theme-btn" :class="{ active: themeStore.theme === 'light' }" @click="themeStore.setTheme('light')">
          ☀️ Licht
        </button>
        <button class="theme-btn" :class="{ active: themeStore.theme === 'dark' }" @click="themeStore.setTheme('dark')">
          🌙 Donker
        </button>
        <button class="theme-btn" :class="{ active: themeStore.theme === 'system' }" @click="themeStore.setTheme('system')">
          ⚙️ Systeem
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useThemeStore } from '../stores/theme'
const themeStore = useThemeStore()
</script>
