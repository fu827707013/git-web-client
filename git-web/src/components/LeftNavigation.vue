<template>
  <v-navigation-drawer permanent width="200" class="left-nav">
    <!-- 仓库名称 -->
    <div class="pa-3 d-flex align-center">
      <span class="text-subtitle-2 font-weight-bold">{{ repoName }}</span>
      <v-spacer />
      <v-btn icon size="x-small" variant="text">
        <v-icon size="small">mdi-cog</v-icon>
      </v-btn>
    </div>

    <v-divider />

    <!-- 视图切换按钮 -->
    <v-list density="compact" nav class="py-1">
      <v-list-item
        :active="uiStore.currentView === 'changes'"
        @click="uiStore.switchView('changes')"
      >
        <template v-slot:prepend>
          <v-icon>mdi-file-document-edit</v-icon>
        </template>
        <v-list-item-title>变更 ({{ changesCount }})</v-list-item-title>
      </v-list-item>

      <v-list-item
        :active="uiStore.currentView === 'all-commits'"
        @click="uiStore.switchView('all-commits')"
      >
        <template v-slot:prepend>
          <v-icon>mdi-source-commit</v-icon>
        </template>
        <v-list-item-title>所有提交</v-list-item-title>
      </v-list-item>
    </v-list>

    <v-divider />

    <!-- 工具按钮（暂不开发） -->
    <div class="pa-2 d-flex justify-center">
      <v-btn icon size="x-small" variant="text" title="历史">
        <v-icon size="small">mdi-history</v-icon>
      </v-btn>
      <v-btn icon size="x-small" variant="text" title="搜索">
        <v-icon size="small">mdi-magnify</v-icon>
      </v-btn>
      <v-btn icon size="x-small" variant="text" title="刷新">
        <v-icon size="small">mdi-refresh</v-icon>
      </v-btn>
    </div>

    <v-divider />

    <!-- 筛选输入框 -->
    <div class="pa-2">
      <v-text-field
        v-model="filterText"
        density="compact"
        variant="outlined"
        placeholder="筛选"
        prepend-inner-icon="mdi-magnify"
        hide-details
      />
    </div>

    <v-divider />

    <!-- 分支列表 -->
    <v-expansion-panels variant="accordion" class="branch-panels">
      <!-- 星标分支 -->
      <v-expansion-panel>
        <v-expansion-panel-title class="py-2">
          <span class="text-caption">星标</span>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <v-list density="compact" class="pa-0">
            <v-list-item
              v-for="branch in starredBranches"
              :key="branch.name"
              :value="branch.name"
              class="pl-4"
            >
              <v-list-item-title class="text-caption">
                <v-icon size="x-small" v-if="branch.current">mdi-check</v-icon>
                {{ branch.name }}
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <!-- 本地分支 -->
      <v-expansion-panel>
        <v-expansion-panel-title class="py-2">
          <span class="text-caption">分支</span>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <v-list density="compact" class="pa-0">
            <v-list-item
              v-for="branch in gitStore.branches"
              :key="branch.name"
              :value="branch.name"
              class="pl-4"
            >
              <v-list-item-title class="text-caption">
                <v-icon size="x-small" v-if="branch.current">mdi-check</v-icon>
                {{ branch.name }}
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <!-- 远程仓库 -->
      <v-expansion-panel>
        <v-expansion-panel-title class="py-2">
          <span class="text-caption">远程</span>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <v-list density="compact" class="pa-0">
            <v-list-item
              v-for="remote in gitStore.remotes"
              :key="remote.name"
              :value="remote.name"
              class="pl-4"
            >
              <v-list-item-title class="text-caption">{{ remote.name }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <!-- 标签 -->
      <v-expansion-panel>
        <v-expansion-panel-title class="py-2">
          <span class="text-caption">标签</span>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <v-list density="compact" class="pa-0">
            <v-list-item
              v-for="tag in gitStore.tags"
              :key="tag.name"
              :value="tag.name"
              class="pl-4"
            >
              <v-list-item-title class="text-caption">{{ tag.name }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <!-- 暂存（暂不开发） -->
      <v-list-item density="compact" class="text-caption">
        暂存
      </v-list-item>

      <!-- 子模块（暂不开发） -->
      <v-list-item density="compact" class="text-caption">
        子模块
      </v-list-item>
    </v-expansion-panels>
  </v-navigation-drawer>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useUiStore } from '../stores/ui'
import { useGitStore } from '../stores/git'
import { useReposStore } from '../stores/repos'

const uiStore = useUiStore()
const gitStore = useGitStore()
const reposStore = useReposStore()

const filterText = ref('')

const repoName = computed(() => {
  return reposStore.activeRepo?.name || '未选择仓库'
})

const changesCount = computed(() => {
  return gitStore.unstagedCount + gitStore.stagedCount
})

// 星标分支数据（暂时为空，后续可以实现星标功能）
const starredBranches = ref([])

// 监听当前激活的仓库变化
watch(() => reposStore.activeRepo, async (newRepo) => {
  if (newRepo && newRepo.path) {
    console.log('Loading repo info for:', newRepo.name, newRepo.path)
    await gitStore.loadRepoInfo(newRepo.path)
  } else {
    // 如果没有仓库，清空数据
    gitStore.setBranches([])
    gitStore.setRemotes([])
    gitStore.setTags([])
  }
}, { immediate: true })

// 初始化时加载当前仓库信息
onMounted(async () => {
  if (reposStore.activeRepo && reposStore.activeRepo.path) {
    await gitStore.loadRepoInfo(reposStore.activeRepo.path)
  }
})
</script>

<style scoped>
.left-nav {
  border-right: 1px solid rgba(0, 0, 0, 0.12);
}

.branch-panels :deep(.v-expansion-panel-title) {
  min-height: 32px !important;
  padding: 0 8px !important;
  font-size: 0.75rem;
}

.branch-panels :deep(.v-expansion-panel-text__wrapper) {
  padding: 0 !important;
}
</style>
