<template>
  <v-app-bar color="grey-lighten-4" elevation="0" density="compact">
    <template v-slot:prepend>
      <v-btn icon size="small" title="快速启动">
        <v-icon>mdi-play-circle-outline</v-icon>
      </v-btn>
    </template>

    <v-btn
      size="small"
      variant="text"
      title="拉取"
      @click="handlePull"
      :loading="loading && loadingAction === 'pull'"
      :disabled="loading"
    >
      <v-icon start>mdi-download</v-icon>
      拉取
    </v-btn>

    <v-btn
      size="small"
      variant="text"
      title="推送"
      @click="handlePush"
      :loading="loading && loadingAction === 'push'"
      :disabled="loading"
    >
      <v-icon start>mdi-upload</v-icon>
      推送
      <v-badge
        v-if="gitStore.unpushedCount > 0"
        :content="gitStore.unpushedCount"
        color="primary"
        inline
        class="ml-1"
      />
    </v-btn>

    <v-btn
      size="small"
      variant="text"
      title="获取（Fetch）"
      @click="handleFetch"
      :loading="loading && loadingAction === 'fetch'"
      :disabled="loading"
    >
      <v-icon start>mdi-sync</v-icon>
      获取
    </v-btn>

    <v-menu>
      <template v-slot:activator="{ props }">
        <v-btn size="small" variant="text" v-bind="props">
          更多
          <v-icon end>mdi-menu-down</v-icon>
        </v-btn>
      </template>
      <v-list density="compact">
        <v-list-item @click="handleRefresh">
          <v-list-item-title>刷新</v-list-item-title>
        </v-list-item>
        <v-list-item>
          <v-list-item-title>历史记录</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>

    <v-spacer />

    <!-- 组件大小设置 -->
    <v-menu>
      <template v-slot:activator="{ props }">
        <v-btn variant="text" v-bind="props" title="组件大小">
          <v-icon start>mdi-resize</v-icon>
          {{ uiStore.componentSize }}
        </v-btn>
      </template>
      <v-list density="compact">
        <v-list-item
          v-for="size in sizeOptions"
          :key="size.value"
          @click="uiStore.setComponentSize(size.value)"
          :active="uiStore.componentSize === size.value"
        >
          <v-list-item-title>{{ size.label }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>

    <!-- 组件密度设置 -->
    <v-menu>
      <template v-slot:activator="{ props }">
        <v-btn variant="text" v-bind="props" title="组件密度">
          <v-icon start>mdi-view-compact</v-icon>
          {{ uiStore.componentDensity }}
        </v-btn>
      </template>
      <v-list density="compact">
        <v-list-item
          v-for="density in densityOptions"
          :key="density.value"
          @click="uiStore.setComponentDensity(density.value)"
          :active="uiStore.componentDensity === density.value"
        >
          <v-list-item-title>{{ density.label }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>

    <v-btn icon @click="uiStore.toggleTheme()" title="切换主题">
      <v-icon>{{ uiStore.theme === 'light' ? 'mdi-weather-night' : 'mdi-weather-sunny' }}</v-icon>
    </v-btn>

    <v-btn icon title="设置">
      <v-icon>mdi-cog</v-icon>
    </v-btn>
  </v-app-bar>
</template>

<script setup>
import { ref } from 'vue'
import { useUiStore } from '../stores/ui'
import { useGitStore } from '../stores/git'
import { useReposStore } from '../stores/repos'

const uiStore = useUiStore()
const gitStore = useGitStore()
const reposStore = useReposStore()

const loading = ref(false)
const loadingAction = ref('')

// 组件大小选项
const sizeOptions = [
  { label: '超小 (x-small)', value: 'x-small' },
  { label: '小 (small)', value: 'small' },
  { label: '默认 (default)', value: 'default' },
  { label: '大 (large)', value: 'large' },
  { label: '超大 (x-large)', value: 'x-large' }
]

// 组件密度选项
const densityOptions = [
  { label: '紧凑 (compact)', value: 'compact' },
  { label: '舒适 (comfortable)', value: 'comfortable' },
  { label: '默认 (default)', value: 'default' }
]

async function handlePull() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    alert('请先打开一个仓库')
    return
  }

  loading.value = true
  loadingAction.value = 'pull'

  try {
    // TODO: 从配置中获取用户信息
    const name = 'User'
    const email = 'user@example.com'
    const result = await gitStore.pull(repoPath, name, email, false)
    alert(`拉取成功: ${result}`)
    // 重新加载仓库信息（包括未推送数量）和文件状态
    await gitStore.loadRepoInfo(repoPath)
  } catch (e) {
    alert('拉取失败: ' + (e.response?.data?.error || e.message))
  } finally {
    loading.value = false
    loadingAction.value = ''
  }
}

async function handlePush() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    alert('请先打开一个仓库')
    return
  }

  loading.value = true
  loadingAction.value = 'push'

  try {
    const result = await gitStore.push(repoPath, 'origin')
    alert(`推送成功: ${result}`)
    // 推送成功后重新加载仓库信息以更新未推送数量
    await gitStore.loadRepoInfo(repoPath)
  } catch (e) {
    alert('推送失败: ' + (e.response?.data?.error || e.message))
  } finally {
    loading.value = false
    loadingAction.value = ''
  }
}

async function handleFetch() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    alert('请先打开一个仓库')
    return
  }

  loading.value = true
  loadingAction.value = 'fetch'

  try {
    await gitStore.fetch(repoPath, 'origin')
    alert('获取成功')
    // 重新加载分支信息
    await gitStore.loadRepoInfo(repoPath)
  } catch (e) {
    alert('获取失败: ' + (e.response?.data?.error || e.message))
  } finally {
    loading.value = false
    loadingAction.value = ''
  }
}

async function handleRefresh() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    alert('请先打开一个仓库')
    return
  }

  loading.value = true
  loadingAction.value = 'refresh'

  try {
    await gitStore.loadRepoInfo(repoPath)
    alert('刷新成功')
  } catch (e) {
    alert('刷新失败: ' + e.message)
  } finally {
    loading.value = false
    loadingAction.value = ''
  }
}
</script>
