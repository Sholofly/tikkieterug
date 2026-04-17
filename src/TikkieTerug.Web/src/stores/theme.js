import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const STORAGE_KEY = 'tikkieterug_theme'

export const useThemeStore = defineStore('theme', () => {
  const theme = ref(localStorage.getItem(STORAGE_KEY) || 'system')
  
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  
  function applyTheme(currentTheme) {
    if (currentTheme === 'system') {
      const isDark = mediaQuery.matches
      document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
    } else {
      document.documentElement.setAttribute('data-theme', currentTheme)
    }
  }

  // Listen to system theme changes if in 'system' mode
  mediaQuery.addEventListener('change', () => {
    if (theme.value === 'system') {
      applyTheme('system')
    }
  })

  watch(theme, (newTheme) => {
    localStorage.setItem(STORAGE_KEY, newTheme)
    applyTheme(newTheme)
  }, { immediate: true })

  function setTheme(newTheme) {
    if (['dark', 'light', 'system'].includes(newTheme)) {
      theme.value = newTheme
    }
  }

  return {
    theme,
    setTheme
  }
})
