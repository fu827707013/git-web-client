<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon class="mr-2">mdi-git</v-icon>
      Git 仓库管理
    </v-card-title>
    <v-card-text>
      <!-- 添加新仓库表单 -->
      <v-expansion-panels v-model="panel" class="mb-4">
        <v-expansion-panel>
          <v-expansion-panel-title>
            <v-icon class="mr-2">mdi-plus-circle</v-icon>
            添加新仓库
          </v-expansion-panel-title>
          <v-expansion-panel-text>
            <v-text-field
              v-model="repoName"
              label="仓库名称"
              prepend-icon="mdi-tag"
              placeholder="例如：我的项目"
              class="mb-2"
            />
            <v-text-field
              v-model="path"
              label="仓库路径"
              prepend-icon="mdi-folder"
              placeholder="例如：D:\projects\myrepo"
            />
            <div class="d-flex gap-2 mt-3">
              <v-btn color="primary" @click="load" :loading="loading">
                <v-icon class="mr-1">mdi-check</v-icon>
                加载并保存
              </v-btn>
              <v-btn color="secondary" variant="outlined" @click="clearForm">
                <v-icon class="mr-1">mdi-close</v-icon>
                清空
              </v-btn>
            </div>
            <v-alert v-if="error" type="error" class="mt-3" closable @click:close="error = ''">
              {{ error }}
            </v-alert>
            <v-alert v-if="success" type="success" class="mt-3" closable @click:close="success = ''">
              {{ success }}
            </v-alert>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>

      <!-- 已保存的仓库列表 -->
      <div class="mb-2">
        <h3 class="text-subtitle-1 font-weight-bold mb-2">
          <v-icon class="mr-1">mdi-bookmark-multiple</v-icon>
          已保存的仓库
        </h3>
      </div>

      <v-list v-if="savedRepos.length > 0" class="rounded" density="compact">
        <v-list-item
          v-for="repo in savedRepos"
          :key="repo.name"
          @click="loadSavedRepo(repo)"
          class="repo-item"
          :class="{ 'active-repo': currentRepoName === repo.name }"
        >
          <template v-slot:prepend>
            <v-icon color="primary">mdi-source-repository</v-icon>
          </template>

          <v-list-item-title class="font-weight-medium">
            {{ repo.name }}
          </v-list-item-title>
          <v-list-item-subtitle class="text-caption">
            {{ repo.path }}
          </v-list-item-subtitle>
          <v-list-item-subtitle class="text-caption text-grey">
            最后访问: {{ formatDate(repo.lastAccessed) }}
          </v-list-item-subtitle>

          <template v-slot:append>
            <v-btn
              icon
              size="small"
              variant="text"
              @click.stop="deleteRepo(repo.name)"
            >
              <v-icon size="small" color="error">mdi-delete</v-icon>
            </v-btn>
          </template>
        </v-list-item>
      </v-list>

      <v-alert v-else type="info" variant="tonal" class="mt-2">
        暂无保存的仓库，请添加新仓库
      </v-alert>
    </v-card-text>
  </v-card>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const emit = defineEmits(['loaded', 'repoChanged'])

const path = ref('')
const repoName = ref('')
const savedRepos = ref([])
const error = ref('')
const success = ref('')
const loading = ref(false)
const panel = ref(null)
const currentRepoName = ref('')

onMounted(() => {
  fetchSavedRepos()
  const lastRepo = localStorage.getItem('currentRepoName')
  if (lastRepo) {
    currentRepoName.value = lastRepo
  }
})

async function fetchSavedRepos() {
  try {
    const res = await api.get('/repo/saved')
    savedRepos.value = res.data
  } catch (e) {
    console.error('Failed to fetch saved repos:', e)
  }
}

async function load() {
  error.value = ''
  success.value = ''
  loading.value = true

  try {
    const payload = { path: path.value }
    if (repoName.value.trim()) {
      payload.name = repoName.value.trim()
    }

    const res = await api.post('/repo/load', payload)

    emit('loaded', path.value)
    currentRepoName.value = repoName.value.trim() || ''

    if (currentRepoName.value) {
      localStorage.setItem('currentRepoName', currentRepoName.value)
    }

    success.value = repoName.value
      ? `仓库 "${repoName.value}" 加载成功并已保存`
      : '仓库加载成功'

    await fetchSavedRepos()

    // 3秒后关闭面板
    setTimeout(() => {
      panel.value = null
      clearForm()
    }, 1500)
  } catch (e) {
    error.value = '路径无效或非 Git 仓库'
  } finally {
    loading.value = false
  }
}

async function loadSavedRepo(repo) {
  error.value = ''
  success.value = ''
  loading.value = true

  try {
    const res = await api.post('/repo/load', { path: repo.path, name: repo.name })
    emit('loaded', repo.path)
    currentRepoName.value = repo.name
    localStorage.setItem('currentRepoName', repo.name)

    success.value = `仓库 "${repo.name}" 加载成功`

    await fetchSavedRepos()

    setTimeout(() => {
      success.value = ''
    }, 2000)
  } catch (e) {
    error.value = `加载仓库 "${repo.name}" 失败: 路径可能无效`
  } finally {
    loading.value = false
  }
}

async function deleteRepo(name) {
  if (!confirm(`确定要删除仓库 "${name}" 吗？`)) return

  try {
    await api.delete(`/repo/saved/${name}`)
    await fetchSavedRepos()

    if (currentRepoName.value === name) {
      currentRepoName.value = ''
      localStorage.removeItem('currentRepoName')
    }

    success.value = `仓库 "${name}" 已删除`
    setTimeout(() => {
      success.value = ''
    }, 2000)
  } catch (e) {
    error.value = '删除仓库失败'
  }
}

function clearForm() {
  path.value = ''
  repoName.value = ''
  error.value = ''
  success.value = ''
}

function formatDate(dateStr) {
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.repo-item {
  cursor: pointer;
  transition: background-color 0.2s;
  border-left: 3px solid transparent;
}

.repo-item:hover {
  background-color: rgba(0, 0, 0, 0.04);
}

.active-repo {
  background-color: rgba(33, 150, 243, 0.08);
  border-left-color: #2196F3;
}

.gap-2 {
  gap: 8px;
}
</style>