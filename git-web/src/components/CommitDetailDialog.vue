<template>
  <v-dialog v-model="dialog" max-width="900" scrollable>
    <v-card>
      <v-card-title class="d-flex align-center bg-primary">
        <v-icon class="mr-2">mdi-source-commit</v-icon>
        <span>提交详情</span>
        <v-spacer></v-spacer>
        <v-btn icon variant="text" @click="dialog = false">
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </v-card-title>

      <v-card-text v-if="loading" class="text-center pa-8">
        <v-progress-circular indeterminate color="primary"></v-progress-circular>
        <p class="mt-4">加载中...</p>
      </v-card-text>

      <v-card-text v-else-if="error" class="pa-4">
        <v-alert type="error">{{ error }}</v-alert>
      </v-card-text>

      <v-card-text v-else-if="commitData" class="pa-0">
        <!-- Commit Info -->
        <div class="pa-4 bg-grey-lighten-4">
          <div class="mb-3">
            <div class="text-h6 font-weight-bold mb-2">{{ commitData.messageShort }}</div>
            <div class="text-body-2 text-grey-darken-1" v-if="commitData.message !== commitData.messageShort">
              <pre class="commit-message">{{ getFullMessage() }}</pre>
            </div>
          </div>

          <v-divider class="my-3"></v-divider>

          <v-row dense>
            <v-col cols="12" sm="6">
              <div class="d-flex align-center mb-2">
                <v-icon size="small" class="mr-2">mdi-account</v-icon>
                <span class="text-body-2">
                  <strong>作者:</strong> {{ commitData.author }}
                </span>
              </div>
              <div class="d-flex align-center">
                <v-icon size="small" class="mr-2">mdi-email</v-icon>
                <span class="text-body-2">{{ commitData.authorEmail }}</span>
              </div>
            </v-col>
            <v-col cols="12" sm="6">
              <div class="d-flex align-center mb-2">
                <v-icon size="small" class="mr-2">mdi-calendar</v-icon>
                <span class="text-body-2">
                  <strong>日期:</strong> {{ formatDate(commitData.date) }}
                </span>
              </div>
              <div class="d-flex align-center">
                <v-icon size="small" class="mr-2">mdi-git</v-icon>
                <span class="text-body-2 text-mono">{{ commitData.sha.substring(0, 10) }}</span>
              </div>
            </v-col>
          </v-row>

          <v-chip size="small" color="primary" class="mt-3">
            <v-icon start size="small">mdi-file-document-multiple</v-icon>
            {{ commitData.filesChanged }} 个文件变更
          </v-chip>
        </div>

        <v-divider></v-divider>

        <!-- File Tree -->
        <div class="pa-4">
          <div class="d-flex align-center mb-3">
            <h3 class="text-subtitle-1 font-weight-bold">
              <v-icon class="mr-1">mdi-file-tree</v-icon>
              变更文件
            </h3>
            <v-spacer></v-spacer>
            <v-btn
              size="small"
              variant="text"
              @click="expandAll"
              class="mr-2"
            >
              <v-icon size="small" class="mr-1">mdi-arrow-expand-vertical</v-icon>
              全部展开
            </v-btn>
            <v-btn
              size="small"
              variant="text"
              @click="collapseAll"
              class="mr-2"
            >
              <v-icon size="small" class="mr-1">mdi-arrow-collapse-vertical</v-icon>
              全部折叠
            </v-btn>
            <v-text-field
              v-model="searchQuery"
              density="compact"
              placeholder="搜索文件..."
              prepend-inner-icon="mdi-magnify"
              variant="outlined"
              hide-details
              clearable
              style="max-width: 250px;"
            ></v-text-field>
          </div>

          <div class="tree-container">
            <TreeNode
              v-for="node in filteredTreeRoot"
              :key="node.path"
              :node="node"
              :level="0"
              :search-query="searchQuery"
              @toggle="toggleNode"
            />
          </div>

          <v-alert v-if="filteredTreeRoot.length === 0 && searchQuery" type="info" class="mt-3">
            没有找到匹配 "{{ searchQuery }}" 的文件
          </v-alert>
        </div>
      </v-card-text>

      <v-divider></v-divider>

      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn color="primary" variant="text" @click="dialog = false">
          关闭
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '../api'
import TreeNode from './TreeNode.vue'

const props = defineProps({
  modelValue: Boolean,
  path: String,
  sha: String
})

const emit = defineEmits(['update:modelValue'])

const dialog = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const commitData = ref(null)
const loading = ref(false)
const error = ref('')
const searchQuery = ref('')
const expandedNodes = ref(new Set())

watch(() => props.sha, async (newSha) => {
  if (newSha && props.path && dialog.value) {
    await fetchCommitDetails()
  }
})

watch(dialog, async (newVal) => {
  if (newVal && props.sha && props.path) {
    await fetchCommitDetails()
  }
})

async function fetchCommitDetails() {
  loading.value = true
  error.value = ''
  commitData.value = null
  expandedNodes.value.clear()

  try {
    const res = await api.get('/repo/commit-details', {
      params: {
        path: props.path,
        sha: props.sha
      }
    })
    commitData.value = res.data

    // 默认展开所有目录
    setTimeout(() => {
      expandAll()
    }, 0)
  } catch (e) {
    error.value = '加载提交详情失败'
    console.error(e)
  } finally {
    loading.value = false
  }
}

// 构建树形结构
function buildTreeStructure() {
  if (!commitData.value) return []

  const files = commitData.value.files || []
  const root = { children: {}, files: [] }

  files.forEach(file => {
    const parts = file.path.split('/')
    let current = root

    // 构建目录结构
    for (let i = 0; i < parts.length - 1; i++) {
      const part = parts[i]
      if (!current.children[part]) {
        current.children[part] = {
          name: part,
          path: parts.slice(0, i + 1).join('/'),
          children: {},
          files: [],
          isFolder: true
        }
      }
      current = current.children[part]
    }

    // 添加文件
    current.files.push({
      ...file,
      name: parts[parts.length - 1],
      isFolder: false
    })
  })

  return root
}

// 将树结构转换为数组格式
const treeRoot = computed(() => {
  const root = buildTreeStructure()

  function convertToArray(node, path = '') {
    const result = []

    // 添加子文件夹
    Object.values(node.children || {}).forEach(child => {
      result.push({
        ...child,
        children: convertToArray(child, child.path),
        expanded: expandedNodes.value.has(child.path)
      })
    })

    // 添加文件
    if (node.files) {
      result.push(...node.files)
    }

    return result.sort((a, b) => {
      // 文件夹优先
      if (a.isFolder && !b.isFolder) return -1
      if (!a.isFolder && b.isFolder) return 1
      // 同类型按名称排序
      return (a.name || '').localeCompare(b.name || '')
    })
  }

  return convertToArray(root)
})

// 过滤树（搜索功能）
const filteredTreeRoot = computed(() => {
  if (!searchQuery.value) return treeRoot.value

  const query = searchQuery.value.toLowerCase()

  function filterTree(nodes) {
    return nodes.filter(node => {
      if (!node.isFolder) {
        // 文件：检查路径是否匹配
        return node.path.toLowerCase().includes(query)
      } else {
        // 文件夹：递归过滤子节点
        const filteredChildren = filterTree(node.children || [])
        if (filteredChildren.length > 0) {
          node.children = filteredChildren
          return true
        }
        return false
      }
    })
  }

  return filterTree(JSON.parse(JSON.stringify(treeRoot.value)))
})

function toggleNode(nodePath) {
  if (expandedNodes.value.has(nodePath)) {
    expandedNodes.value.delete(nodePath)
  } else {
    expandedNodes.value.add(nodePath)
  }
}

function expandAll() {
  function addAllPaths(nodes) {
    nodes.forEach(node => {
      if (node.isFolder) {
        expandedNodes.value.add(node.path)
        if (node.children) {
          addAllPaths(node.children)
        }
      }
    })
  }
  addAllPaths(treeRoot.value)
}

function collapseAll() {
  expandedNodes.value.clear()
}

function formatDate(dateStr) {
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

function getFullMessage() {
  if (!commitData.value) return ''
  const lines = commitData.value.message.split('\n')
  return lines.slice(1).join('\n').trim()
}
</script>

<style scoped>
.text-mono {
  font-family: monospace;
}

.commit-message {
  white-space: pre-wrap;
  font-family: inherit;
  margin: 0;
  font-size: 0.875rem;
}

.tree-container {
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 8px;
  background-color: #fafafa;
  max-height: 500px;
  overflow-y: auto;
}

.v-theme--dark .tree-container {
  border-color: #424242;
  background-color: #1e1e1e;
}

.bg-grey-lighten-4 {
  background-color: #f5f5f5;
}

.v-theme--dark .bg-grey-lighten-4 {
  background-color: #1e1e1e;
}
</style>
