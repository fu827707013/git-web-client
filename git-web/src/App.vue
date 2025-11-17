<template>
  <v-app>
    <v-container>
      <v-row>
        <v-col cols="12" md="6">
          <RepoLoader @loaded="onLoaded" />
        </v-col>
        <v-col cols="12" md="6">
          <RepoStatusCard :path="path" />
        </v-col>
      </v-row>
      <!-- <v-row>
        <v-col cols="12" md="12">
          <GitActions :path="path" />
        </v-col>
      </v-row> -->
      <v-row>
        <v-col cols="12">
          <CommitList :path="path" />
        </v-col>
      </v-row>
    </v-container>
  </v-app>
</template>

<script setup>
import RepoLoader from './components/RepoLoader.vue'
import RepoStatusCard from './components/RepoStatusCard.vue'
import CommitList from './components/CommitList.vue'
import GitActions from './components/GitActions.vue'
import { ref } from 'vue'
const path = ref(localStorage.getItem('repoPath') || '')
function onLoaded(p) {
  path.value = p
  localStorage.setItem('repoPath', p)
}
</script>