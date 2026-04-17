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
      <div v-if="themeStore.theme === 'system'" class="theme-hint">
        Volgt apparaatinstelling (nu: {{ resolvedTheme }})
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useThemeStore } from '../stores/theme'

const themeStore = useThemeStore()
const systemIsDark = ref(window.matchMedia('(prefers-color-scheme: dark)').matches)
const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

function onMediaChange(e) { systemIsDark.value = e.matches }
onMounted(() => mediaQuery.addEventListener('change', onMediaChange))
onUnmounted(() => mediaQuery.removeEventListener('change', onMediaChange))

const resolvedTheme = computed(() => systemIsDark.value ? 'donker' : 'licht')
</script>
