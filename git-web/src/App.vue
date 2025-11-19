<template>
  <v-app :theme="uiStore.theme">
    <!-- 顶部工具栏 -->
    <Toolbar />

    <!-- 仓库 Tab 标签栏 -->
    <RepoTabs />

    <!-- 左侧导航 -->
    <LeftNavigation />

    <!-- 主内容区 -->
    <v-main>
      <v-container fluid class="pa-0 main-content-container">
        <!-- 欢迎屏幕（无仓库时显示） -->
        <div v-if="reposStore.openRepos.length === 0" class="welcome-screen">
          <v-card class="text-center pa-8" elevation="0">
            <v-icon size="100" color="grey-lighten-1">mdi-source-repository</v-icon>
            <h2 class="text-h5 mt-4 mb-2">欢迎使用 Git Web Client</h2>
            <p class="text-body-1 text-grey">
              请点击仓库标签栏的 + 按钮添加一个 Git 仓库开始使用
            </p>
          </v-card>
        </div>

        <!-- 内容区（有仓库时显示） -->
        <div v-else class="content-area">
          <!-- All Commits 视图 -->
          <AllCommitsView
            v-if="uiStore.currentView === 'all-commits' && currentRepoPath"
            :path="currentRepoPath"
            @commit-selected="handleCommitSelected"
          />

          <!-- Changes 视图 -->
          <ChangesView
            v-else-if="uiStore.currentView === 'changes'"
          />
        </div>
      </v-container>
    </v-main>

    <!-- 底部详情面板 -->
    <BottomDetailPanel />
  </v-app>
</template>

<script setup>
import { computed, onMounted, watch } from 'vue'
import { useTheme } from 'vuetify'
import { useUiStore } from './stores/ui'
import { useReposStore } from './stores/repos'
import { useGitStore } from './stores/git'

import Toolbar from './components/Toolbar.vue'
import RepoTabs from './components/RepoTabs.vue'
import LeftNavigation from './components/LeftNavigation.vue'
import AllCommitsView from './components/AllCommitsView.vue'
import ChangesView from './components/ChangesView.vue'
import BottomDetailPanel from './components/BottomDetailPanel.vue'

const uiStore = useUiStore()
const reposStore = useReposStore()
const gitStore = useGitStore()
const vuetifyTheme = useTheme()

// 计算当前仓库路径
const currentRepoPath = computed(() => {
  return reposStore.activeRepo?.path || ''
})

// 计算底部内边距（底部面板显示时需要）
const bottomPadding = computed(() => {
  return uiStore.bottomPanelVisible ? `${uiStore.bottomPanelHeight + 48}px` : '0'
})

// 初始化主题和加载已保存的仓库
onMounted(async () => {
  uiStore.initTheme()
  vuetifyTheme.global.name.value = uiStore.theme

  // 从后台加载已保存的仓库
  await reposStore.loadSavedRepos()
})

// 监听主题变化
watch(() => uiStore.theme, (newTheme) => {
  vuetifyTheme.global.name.value = newTheme
})

// 监听当前仓库变化
watch(() => reposStore.activeRepo, (repo) => {
  // 当前激活仓库变化时的逻辑（如果需要的话）
})

// 处理提交选中事件
function handleCommitSelected(commit) {
  gitStore.selectCommit(commit)
}
</script>

<style>
/* 全局样式优化 */
html, body {
  margin: 0;
  padding: 0;
  height: 100%;
  overflow: hidden;
}

#app {
  height: 100%;
}

.v-main {
  /* Vuetify会自动计算高度，减去app-bar的高度 */
}

.main-content-container {
  height: 100%;
  padding-bottom: v-bind(bottomPadding);
}

.welcome-screen {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  background-color: #f5f5f5;
}

.content-area {
  height: 100%;
  overflow: hidden;
}

/* 深色主题背景 */
.v-theme--dark .welcome-screen {
  background-color: #121212;
}
</style>
