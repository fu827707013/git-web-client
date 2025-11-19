<template>
  <v-sheet v-if="uiStore.bottomPanelVisible" class="bottom-panel" elevation="4">
    <v-toolbar density="compact" color="grey-lighten-4">
      <v-tabs
        v-model="uiStore.bottomPanelActiveTab"
        density="compact"
        bg-color="transparent"
      >
        <v-tab value="commit">提交详情</v-tab>
        <v-tab value="changes">变更文件</v-tab>
        <v-tab value="file-tree">文件树</v-tab>
      </v-tabs>

      <v-spacer />

      <v-btn
        icon="mdi-close"
        size="small"
        variant="text"
        @click="uiStore.hideBottomPanel()"
      />
    </v-toolbar>

    <v-divider />

    <div class="panel-content" :style="{ height: uiStore.bottomPanelHeight + 'px' }">
      <v-window v-model="uiStore.bottomPanelActiveTab">
        <!-- Commit 标签页 -->
        <v-window-item value="commit">
          <div class="pa-4">
            <template v-if="selectedCommit">
              <div class="commit-info">
                <h3 class="text-h6 mb-2">{{ selectedCommit.message }}</h3>
                <div class="text-caption text-grey mb-4">
                  <div>作者: {{ selectedCommit.author }}</div>
                  <div>日期: {{ selectedCommit.date }}</div>
                  <div>SHA: {{ selectedCommit.sha }}</div>
                </div>
              </div>

              <!-- 显示文件变更列表 -->
              <div v-if="commitDetails">
                <v-divider class="my-2" />
                <div class="text-subtitle-2 mb-2">变更文件 ({{ commitDetails.filesChanged }})</div>
                <v-list density="compact">
                  <v-list-item
                    v-for="file in commitDetails.files"
                    :key="file.path"
                    class="text-caption"
                  >
                    <template v-slot:prepend>
                      <v-chip size="x-small" :color="getStatusColor(file.status)">
                        {{ file.status }}
                      </v-chip>
                    </template>
                    <v-list-item-title class="text-caption">{{ file.path }}</v-list-item-title>
                  </v-list-item>
                </v-list>
              </div>
            </template>
            <div v-else class="text-center text-grey py-8">
              <v-icon size="48">mdi-information-outline</v-icon>
              <p class="mt-2">选择一个提交以查看详情</p>
            </div>
          </div>
        </v-window-item>

        <!-- Changes 标签页 -->
        <v-window-item value="changes">
          <div class="pa-4">
            <div v-if="commitDetails" class="changes-list">
              <div class="text-subtitle-2 mb-2">
                变更统计: {{ commitDetails.filesChanged }} 个文件
              </div>
              <v-list density="compact">
                <v-list-item
                  v-for="file in commitDetails.files"
                  :key="file.path"
                >
                  <template v-slot:prepend>
                    <v-chip size="x-small" :color="getStatusColor(file.status)">
                      {{ file.status }}
                    </v-chip>
                  </template>
                  <v-list-item-title class="text-caption">{{ file.path }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </div>
            <div v-else class="text-center text-grey py-8">
              未选择提交
            </div>
          </div>
        </v-window-item>

        <!-- File Tree 标签页 -->
        <v-window-item value="file-tree">
          <div class="pa-4">
            <div v-if="commitDetails" class="file-tree">
              <div class="text-subtitle-2 mb-2">文件树视图</div>
              <!-- TODO: 使用 TreeNode 组件构建文件树 -->
              <v-list density="compact">
                <v-list-item
                  v-for="file in commitDetails.files"
                  :key="file.path"
                >
                  <template v-slot:prepend>
                    <v-icon size="small">mdi-file</v-icon>
                  </template>
                  <v-list-item-title class="text-caption">{{ file.path }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </div>
            <div v-else class="text-center text-grey py-8">
              未选择提交
            </div>
          </div>
        </v-window-item>
      </v-window>
    </div>
  </v-sheet>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useUiStore } from '../stores/ui'
import { useGitStore } from '../stores/git'
import api from '../api'

const uiStore = useUiStore()
const gitStore = useGitStore()

const selectedCommit = ref(null)
const commitDetails = ref(null)

// 监听选中的提交
watch(() => gitStore.selectedCommit, async (commit) => {
  if (commit) {
    selectedCommit.value = commit
    // TODO: 调用 API 获取提交详情
    // commitDetails.value = await fetchCommitDetails(commit.sha)

    // 模拟数据
    commitDetails.value = {
      filesChanged: 3,
      files: [
        { path: 'src/App.vue', status: 'M' },
        { path: 'src/components/HelloWorld.vue', status: 'M' },
        { path: 'package.json', status: 'M' }
      ]
    }
  }
})

function getStatusColor(status) {
  const colors = {
    'M': 'warning',
    'A': 'success',
    'D': 'error',
    'R': 'info'
  }
  return colors[status] || 'default'
}

async function fetchCommitDetails(sha) {
  try {
    const response = await api.get(`/repo/commit-details`, {
      params: { path: gitStore.activeRepo?.path, sha }
    })
    return response.data
  } catch (error) {
    console.error('Failed to fetch commit details:', error)
    return null
  }
}
</script>

<style scoped>
.bottom-panel {
  position: fixed;
  bottom: 0;
  left: 200px; /* 左侧导航宽度 */
  right: 0;
  z-index: 100;
  border-top: 1px solid rgba(0, 0, 0, 0.12);
}

.panel-content {
  overflow-y: auto;
  background-color: white;
}

.commit-info {
  border-left: 4px solid #1976d2;
  padding-left: 12px;
}
</style>
