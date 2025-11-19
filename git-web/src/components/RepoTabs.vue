<template>
  <v-app-bar class="repo-tabs-container" color="grey-lighten-5" elevation="0" density="compact">
    <v-tabs
      v-model="reposStore.activeRepoId"
      density="compact"
      bg-color="transparent"
      slider-color="primary"
      show-arrows
      class="flex-grow-1"
    >
      <v-tab
        v-for="repo in reposStore.openRepos"
        :key="repo.id"
        :value="repo.id"
        @click="reposStore.setActiveRepo(repo.id)"
      >
        {{ repo.name }}{{ repo.hasChanges ? '*' : '' }}
        <v-btn
          icon="mdi-close"
          size="x-small"
          variant="text"
          class="ml-2"
          @click.stop="closeRepo(repo.id)"
        />
      </v-tab>

      <!-- 添加新仓库按钮 -->
      <v-btn
        icon
        size="small"
        variant="text"
        class="ml-2"
        @click="openRepoDialog"
        title="添加新仓库"
      >
        <v-icon>mdi-plus</v-icon>
      </v-btn>
    </v-tabs>
  </v-app-bar>

    <!-- 添加仓库对话框 -->
    <v-dialog v-model="showDialog" max-width="600">
      <v-card>
        <v-card-title>添加仓库</v-card-title>
        <v-card-text>
          <!-- 集成 RepoLoader 组件 -->
          <RepoLoader @loaded="handleRepoLoaded" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="showDialog = false">关闭</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
</template>

<script setup>
import { ref } from 'vue'
import { useReposStore } from '../stores/repos'
import RepoLoader from './RepoLoader.vue'

const reposStore = useReposStore()
const showDialog = ref(false)

async function closeRepo(id) {
  if (confirm('确定要关闭此仓库吗？')) {
    try {
      await reposStore.closeRepo(id)
    } catch (e) {
      console.error('Failed to close repo:', e)
      alert('关闭仓库失败')
    }
  }
}

function openRepoDialog() {
  showDialog.value = true
}

async function handleRepoLoaded(repoData) {
  // repoData 包含 { name, path }
  try {
    await reposStore.addRepo({
      name: repoData.name,
      path: repoData.path
    })
    showDialog.value = false
  } catch (e) {
    console.error('Failed to add repo:', e)
    alert('添加仓库失败：路径无效或非 Git 仓库')
  }
}
</script>

<style scoped>
.repo-tabs-container {
  border-bottom: 1px solid rgba(0, 0, 0, 0.12);
}

.repo-tabs-container :deep(.v-toolbar__content) {
  padding: 0 !important;
}
</style>
