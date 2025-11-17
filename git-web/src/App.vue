<template>
  <v-app>
    <!-- App Bar -->
    <v-app-bar color="primary" prominent>
      <v-app-bar-title class="d-flex align-center">
        <v-icon size="large" class="mr-3">mdi-git</v-icon>
        <span class="text-h5 font-weight-bold">Git Web Client</span>
      </v-app-bar-title>

      <template v-slot:append>
        <v-chip v-if="path" color="white" variant="outlined" class="mr-2">
          <v-icon start>mdi-folder-open</v-icon>
          {{ getCurrentRepoDisplay() }}
        </v-chip>
        <v-btn icon @click="toggleTheme">
          <v-icon>{{ theme === 'light' ? 'mdi-weather-night' : 'mdi-weather-sunny' }}</v-icon>
        </v-btn>
      </template>
    </v-app-bar>

    <!-- Navigation Drawer -->
    <v-navigation-drawer v-model="drawer" app width="320">
      <RepoLoader @loaded="onLoaded" />
    </v-navigation-drawer>

    <!-- Main Content -->
    <v-main>
      <v-container fluid class="pa-4">
        <v-row v-if="!path">
          <v-col cols="12">
            <v-card class="text-center pa-8" elevation="0">
              <v-icon size="100" color="grey-lighten-1">mdi-source-repository</v-icon>
              <h2 class="text-h5 mt-4 mb-2">欢迎使用 Git Web Client</h2>
              <p class="text-body-1 text-grey">
                请从左侧面板选择或添加一个 Git 仓库开始使用
              </p>
              <v-btn
                color="primary"
                size="large"
                class="mt-4"
                @click="drawer = true"
              >
                <v-icon class="mr-2">mdi-plus</v-icon>
                选择仓库
              </v-btn>
            </v-card>
          </v-col>
        </v-row>

        <template v-else>
          <!-- Repository Status Section -->
          <v-row>
            <v-col cols="12">
              <RepoStatusCard :path="path" />
            </v-col>
          </v-row>

          <!-- Git Actions Section (commented out for now) -->
          <!-- <v-row>
            <v-col cols="12">
              <GitActions :path="path" />
            </v-col>
          </v-row> -->

          <!-- Commit List Section -->
          <v-row>
            <v-col cols="12">
              <CommitList :path="path" />
            </v-col>
          </v-row>
        </template>
      </v-container>
    </v-main>

    <!-- Footer -->
    <v-footer app class="text-center">
      <v-col cols="12">
        <span class="text-caption">
          Git Web Client &copy; {{ new Date().getFullYear() }} -
          Powered by Vue 3 + Vuetify 3
        </span>
      </v-col>
    </v-footer>
  </v-app>
</template>

<script setup>
import RepoLoader from './components/RepoLoader.vue'
import RepoStatusCard from './components/RepoStatusCard.vue'
import CommitList from './components/CommitList.vue'
import GitActions from './components/GitActions.vue'
import { ref, onMounted } from 'vue'
import { useTheme } from 'vuetify'

const path = ref(localStorage.getItem('repoPath') || '')
const drawer = ref(true)
const theme = ref('light')
const vuetifyTheme = useTheme()

onMounted(() => {
  const savedTheme = localStorage.getItem('theme') || 'light'
  theme.value = savedTheme
  vuetifyTheme.global.name.value = savedTheme
})

function onLoaded(p) {
  path.value = p
  localStorage.setItem('repoPath', p)
}

function toggleTheme() {
  theme.value = theme.value === 'light' ? 'dark' : 'light'
  vuetifyTheme.global.name.value = theme.value
  localStorage.setItem('theme', theme.value)
}

function getCurrentRepoDisplay() {
  const repoName = localStorage.getItem('currentRepoName')
  if (repoName) {
    return repoName
  }
  // 如果没有仓库名称，显示路径的最后一部分
  const parts = path.value.split(/[/\\]/)
  return parts[parts.length - 1] || path.value
}
</script>

<style>
/* 全局样式优化 */
.v-app-bar-title {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.v-main {
  background-color: #f5f5f5;
}

.v-theme--dark .v-main {
  background-color: #121212;
}
</style>