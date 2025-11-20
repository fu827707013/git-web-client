<template>
  <v-container fluid class="pa-0 changes-view-container">
    <v-row no-gutters style="height: 100%">
      <!-- 左侧：文件列表区 (35%) -->
      <v-col cols="4" class="file-list-area">

        <!-- 提交信息区 -->
        <v-card flat>
          <v-card-text class="pa-3">
            <v-textarea v-model="commitMessage" variant="outlined" placeholder="输入提交消息..." rows="3" density="compact"
              hide-details />
            <div class="d-flex gap-2 mt-3">
              <v-btn color="primary" size="small" :disabled="gitStore.stagedCount === 0 || !commitMessage"
                @click="commit">
                提交
              </v-btn>
              <v-btn size="small" variant="outlined" @click="discardAll">
                放弃所有变更
              </v-btn>
            </div>
          </v-card-text>
        </v-card>

        <!-- Unstaged 卡片 -->
        <v-card flat class="mb-2">
          <v-card-title class="d-flex align-center py-2 px-3 bg-grey-lighten-4">
            <span class="text-subtitle-2">Unstaged ({{ gitStore.unstagedCount }})</span>
            <v-spacer />
            <v-btn size="x-small" variant="text" @click="stageAll" :disabled="gitStore.unstagedCount === 0">
              Stage All
            </v-btn>
            <v-btn size="x-small" variant="text" icon @click="toggleExpandUnstaged"
              :disabled="gitStore.unstagedCount === 0" class="mr-1">
              <v-icon size="small">{{ isUnstagedExpanded ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
            </v-btn>
          </v-card-title>
          <v-card-text class="pa-0 unstaged-files-container">
            <!-- 加载状态 -->
            <div v-if="isLoading" class="pa-4 text-center">
              <v-progress-circular indeterminate size="24" width="2" class="mr-2" />
              <span class="text-caption text-grey">加载文件列表中...</span>
            </div>
            <!-- 文件树结构 - 始终渲染以保持 ref -->
            <BaseTree v-else ref="unstagedTreeRef" v-model="localUnstagedTree" childrenKey="c" :defaultOpen="false"
              :virtualization="true" :virtualizationPrerenderCount="10" class="file-tree"
              v-show="localUnstagedTree && localUnstagedTree.length > 0">
              <template v-slot="{ node, stat, tree }">
                <div class="tree-node-item" :style="{ paddingLeft: stat.level * 20 + 'px' }">
                  <!-- 文件夹折叠图标 -->
                  <v-icon v-if="node.f" size="small" class="tree-icon fold-icon" @click.stop="toggleNode(stat)">
                    {{ stat.open ? 'mdi-chevron-down' : 'mdi-chevron-right' }}
                  </v-icon>
                  <span v-else class="tree-icon-placeholder"></span>

                  <!-- 文件夹图标 -->
                  <v-icon v-if="node.f" size="small" class="tree-icon">mdi-folder</v-icon>

                  <!-- 文件暂存图标 -->
                  <v-icon v-if="!node.f" size="small" color="success" class="tree-icon action-icon"
                    @click.stop="stageFile(node, stat)">
                    mdi-plus-circle-outline
                  </v-icon>

                  <!-- 文件/文件夹名称 -->
                  <span class="text-caption node-name">{{ node.n }}</span>

                  <!-- 文件状态标记 -->
                  <v-chip v-if="!node.f && node.s" size="x-small" class="ml-2" :color="getStatusColor(node.s)">
                    {{ node.s }}
                  </v-chip>

                  <!-- 文件夹文件数量 -->
                  <span v-if="node.f" class="text-caption text-grey ml-1">({{ countFiles(node) }})</span>
                </div>
              </template>
            </BaseTree>
            <!-- 空状态提示 -->
            <div v-if="!isLoading && (!localUnstagedTree || localUnstagedTree.length === 0)"
              class="pa-4 text-center text-caption text-grey">
              无未暂存的文件
            </div>
          </v-card-text>
        </v-card>

        <!-- Staged 卡片 -->
        <v-card flat class="mb-2">
          <v-card-title class="d-flex align-center py-2 px-3 bg-grey-lighten-4">
            <span class="text-subtitle-2">Staged ({{ gitStore.stagedCount }})</span>
            <v-spacer />
            <v-btn size="x-small" variant="text" @click="unstageAll" :disabled="gitStore.stagedCount === 0">
              Unstage All
            </v-btn>
            <v-btn size="x-small" variant="text" icon @click="toggleExpandStaged" :disabled="gitStore.stagedCount === 0"
              class="mr-1">
              <v-icon size="small">{{ isStagedExpanded ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
            </v-btn>
          </v-card-title>
          <v-card-text class="pa-0 staged-files-container">
            <!-- Staged 文件树 - 始终渲染以保持 ref -->
            <BaseTree ref="stagedTreeRef" v-model="localStagedTree" childrenKey="c" :defaultOpen="false"
              :virtualization="true" :virtualizationPrerenderCount="10" class="file-tree"
              v-show="localStagedTree && localStagedTree.length > 0">
              <template v-slot="{ node, stat, tree }">
                <div class="tree-node-item" :style="{ paddingLeft: stat.level * 20 + 'px' }">
                  <!-- 文件夹折叠图标 -->
                  <v-icon v-if="node.f" size="small" class="tree-icon fold-icon" @click.stop="toggleNode(stat)">
                    {{ stat.open ? 'mdi-chevron-down' : 'mdi-chevron-right' }}
                  </v-icon>
                  <span v-else class="tree-icon-placeholder"></span>

                  <!-- 文件夹图标 -->
                  <v-icon v-if="node.f" size="small" class="tree-icon">mdi-folder</v-icon>

                  <!-- 文件取消暂存图标 -->
                  <v-icon v-if="!node.f" size="small" color="warning" class="tree-icon action-icon"
                    @click.stop="unstageFile(node, stat)">
                    mdi-minus-circle-outline
                  </v-icon>

                  <!-- 文件/文件夹名称 -->
                  <span class="text-caption node-name">{{ node.n }}</span>

                  <!-- 文件状态标记 -->
                  <v-chip v-if="!node.f && node.s" size="x-small" class="ml-2" :color="getStatusColor(node.s)">
                    {{ node.s }}
                  </v-chip>

                  <!-- 文件夹文件数量 -->
                  <span v-if="node.f" class="text-caption text-grey ml-1">({{ countFiles(node) }})</span>
                </div>
              </template>
            </BaseTree>
            <!-- 空状态提示 -->
            <div v-if="!localStagedTree || localStagedTree.length === 0"
              class="pa-4 text-center text-caption text-grey">
              无已暂存的文件
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- 右侧：差异查看区 (65%，暂不开发) -->
      <v-col cols="8" class="diff-viewer-area">
        <v-card flat height="100%" class="d-flex align-center justify-center">
          <div class="text-center text-grey">
            <v-icon size="64" color="grey-lighten-2">mdi-file-compare</v-icon>
            <p class="text-body-2 mt-2">差异查看器</p>
            <p class="text-caption">暂未开发，后续实现</p>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useGitStore } from '../stores/git'
import { useReposStore } from '../stores/repos'
import { BaseTree } from '@he-tree/vue'
import '@he-tree/vue/style/default.css'

const gitStore = useGitStore()
const reposStore = useReposStore()
const commitMessage = ref('')
const selectedFile = ref(null)
const isLoading = ref(false)

// 树组件引用
const unstagedTreeRef = ref(null)
const stagedTreeRef = ref(null)

// 展开/折叠状态
const isUnstagedExpanded = ref(false)
const isStagedExpanded = ref(false)

// 创建本地树数据（@he-tree/vue 需要本地 ref）
const localUnstagedTree = ref([])
const localStagedTree = ref([])

// 同步 store 数据到本地
watch(() => gitStore.unstagedTree, (newVal) => {
  localUnstagedTree.value = newVal || []
}, { immediate: true })

watch(() => gitStore.stagedTree, (newVal) => {
  localStagedTree.value = newVal || []
}, { immediate: true })

// 监听文件列表变化，判断是否在加载中
watch(() => gitStore.unstagedCount + gitStore.stagedCount, (newVal, oldVal) => {
  // 如果从有数据变成没数据，说明正在加载
  if (oldVal > 0 && newVal === 0) {
    isLoading.value = true
  } else if (newVal > 0) {
    isLoading.value = false
  }
}, { immediate: true })

// 递归计算文件夹中的文件数量
function countFiles(node) {
  if (!node.f) return 0  // 不是文件夹
  let count = 0
  if (node.c && node.c.length > 0) {
    node.c.forEach(child => {
      if (child.f) {
        count += countFiles(child)
      } else {
        count++
      }
    })
  }
  return count
}

// 在 stats 数组中查找指定路径的 stat
function findStatByPath(stats, path) {
  for (const stat of stats) {
    if (stat.data.p === path) {
      return stat
    }
    if (stat.data.f && stat.children) {
      const found = findStatByPath(stat.children, path)
      if (found) return found
    }
  }
  return null
}

// 确保父文件夹存在，返回父 stat（如果是根级文件返回 null）
function ensureParentExists(treeRef, filePath) {
  // 确保 tree ref 已初始化
  if (!treeRef.value) {
    throw new Error('Tree reference is not initialized')
  }

  const pathParts = filePath.split('/')

  // 如果是根级文件，返回 null 和 rootChildren
  if (pathParts.length === 1) {
    return { parentStat: null, children: treeRef.value.rootChildren }
  }

  // 逐级查找或创建父文件夹
  let currentStats = treeRef.value.rootChildren
  let parentStat = null

  for (let i = 0; i < pathParts.length - 1; i++) {
    const folderPath = pathParts.slice(0, i + 1).join('/')
    let folderStat = currentStats.find(s => s.data.p === folderPath && s.data.f)

    if (!folderStat) {
      // 创建文件夹节点
      const folderNode = {
        n: pathParts[i],
        p: folderPath,
        f: true,
        c: []
      }

      // 计算插入位置（文件夹优先，然后按名称排序）
      const insertIndex = currentStats.findIndex(s => {
        if (folderNode.f !== s.data.f) return folderNode.f ? -1 : 1
        return folderNode.n.localeCompare(s.data.n) < 0
      })
      const index = insertIndex === -1 ? currentStats.length : insertIndex

      // 使用 tree.add() 添加文件夹
      treeRef.value.add(folderNode, parentStat, index)

      // 重新查找刚添加的 stat
      folderStat = currentStats.find(s => s.data.p === folderPath && s.data.f)
    }

    parentStat = folderStat
    currentStats = folderStat.children
  }

  return { parentStat, children: currentStats }
}

// 计算节点应插入的索引位置（基于 stats 数组）
function calculateInsertIndex(stats, node) {
  const index = stats.findIndex(s => {
    if (node.f !== s.data.f) return node.f ? -1 : 1
    return node.n.localeCompare(s.data.n) < 0
  })
  return index === -1 ? stats.length : index
}

// 切换节点的折叠/展开状态
function toggleNode(stat) {
  stat.open = !stat.open
}

// 切换 Unstaged 树的展开/折叠状态
function toggleExpandUnstaged() {
  if (!unstagedTreeRef.value) return

  if (isUnstagedExpanded.value) {
    // 折叠所有
    unstagedTreeRef.value.closeAll()
    isUnstagedExpanded.value = false
  } else {
    // 展开所有
    unstagedTreeRef.value.openAll()
    isUnstagedExpanded.value = true
  }
}

// 切换 Staged 树的展开/折叠状态
function toggleExpandStaged() {
  if (!stagedTreeRef.value) return

  if (isStagedExpanded.value) {
    // 折叠所有
    stagedTreeRef.value.closeAll()
    isStagedExpanded.value = false
  } else {
    // 展开所有
    stagedTreeRef.value.openAll()
    isStagedExpanded.value = true
  }
}

// 根据文件状态返回颜色（用于 staged 文件列表）
function getStatusColor(status) {
  switch (status) {
    case 'M': return 'orange' // Modified
    case 'A': return 'success' // Added
    case 'D': return 'error' // Deleted
    case 'R': return 'info' // Renamed
    default: return 'default'
  }
}

async function stageFile(node, stat) {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    console.error('No active repository')
    return
  }

  // 保存节点数据（深拷贝）
  const nodeData = JSON.parse(JSON.stringify(node))

  // 保存原始位置信息以便回滚
  const originalParentStat = stat.parent
  const originalIndex = stat.parent ? stat.parent.children.indexOf(stat) : unstagedTreeRef.value.rootChildren.indexOf(stat)

  // 从 unstaged 树中移除（使用 ref 方法）
  unstagedTreeRef.value.remove(stat)

  try {
    // 调用后端 API
    const result = await gitStore.stageFile(node.p, repoPath)

    if (result.success) {
      // 在 staged 树中确保父文件夹存在
      const { parentStat, children } = ensureParentExists(stagedTreeRef, node.p)

      // 计算插入位置
      const index = calculateInsertIndex(children, nodeData)

      // 添加到 staged 树（使用 ref 方法）
      stagedTreeRef.value.add(nodeData, parentStat, index)

      // 更新计数
      gitStore.unstagedCount--
      gitStore.stagedCount++
    } else {
      throw new Error(result.error || 'Unknown error')
    }
  } catch (e) {
    console.error('Failed to stage file:', e)

    // 回滚：添加回 unstaged 树的原始位置
    unstagedTreeRef.value.add(nodeData, originalParentStat, originalIndex)

    alert('暂存文件失败: ' + (e.message || e))
  }
}

async function unstageFile(node, stat) {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    console.error('No active repository')
    return
  }

  // 保存节点数据（深拷贝）
  const nodeData = JSON.parse(JSON.stringify(node))

  // 保存原始位置信息以便回滚
  const originalParentStat = stat.parent
  const originalIndex = stat.parent ? stat.parent.children.indexOf(stat) : stagedTreeRef.value.rootChildren.indexOf(stat)

  // 从 staged 树中移除（使用 ref 方法）
  stagedTreeRef.value.remove(stat)

  try {
    // 调用后端 API
    const result = await gitStore.unstageFile(node.p, repoPath)

    if (result.success) {
      // 在 unstaged 树中确保父文件夹存在
      const { parentStat, children } = ensureParentExists(unstagedTreeRef, node.p)

      // 计算插入位置
      const index = calculateInsertIndex(children, nodeData)

      // 添加到 unstaged 树（使用 ref 方法）
      unstagedTreeRef.value.add(nodeData, parentStat, index)

      // 更新计数
      gitStore.stagedCount--
      gitStore.unstagedCount++
    } else {
      throw new Error(result.error || 'Unknown error')
    }
  } catch (e) {
    console.error('Failed to unstage file:', e)

    // 回滚：添加回 staged 树的原始位置
    stagedTreeRef.value.add(nodeData, originalParentStat, originalIndex)

    alert('取消暂存失败: ' + (e.message || e))
  }
}

async function stageAll() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    console.error('No active repository')
    return
  }

  try {
    await gitStore.stageAll(repoPath)
  } catch (e) {
    alert('暂存所有文件失败: ' + e.message)
  }
}

async function unstageAll() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    console.error('No active repository')
    return
  }

  try {
    await gitStore.unstageAll(repoPath)
  } catch (e) {
    alert('取消暂存所有文件失败: ' + e.message)
  }
}

function selectFile(file) {
  selectedFile.value = file
  // TODO: 在右侧显示文件差异
}

async function commit() {
  const repoPath = reposStore.activeRepo?.path
  if (!repoPath) {
    alert('请先选择仓库')
    return
  }

  if (!commitMessage.value) {
    alert('请输入提交消息')
    return
  }

  if (gitStore.stagedCount === 0) {
    alert('没有已暂存的文件')
    return
  }

  try {
    const sha = await gitStore.commit(repoPath, commitMessage.value)

    // 清空本地 staged 树
    localStagedTree.value = []

    alert(`提交成功！\nSHA: ${sha}`)
    commitMessage.value = ''
  } catch (e) {
    console.error('Failed to commit:', e)
    alert('提交失败: ' + (e.message || e))
  }
}

function discardAll() {
  if (confirm('确定要放弃所有变更吗？')) {
    gitStore.unstagedTree = []
    gitStore.stagedTree = []
    gitStore.unstagedCount = 0
    gitStore.stagedCount = 0
  }
}
</script>

<style scoped>
.changes-view-container {
  height: 100%;
}

.file-list-area {
  border-right: 1px solid rgba(0, 0, 0, 0.12);
  overflow-y: auto;
  height: 100%;
}

.diff-viewer-area {
  background-color: #fafafa;
}

.unstaged-files-container,
.staged-files-container {
  max-height: 600px;
  overflow-y: auto;
}

/* @he-tree/vue 文件树样式 */
.file-tree {
  font-size: 0.75rem;
}

.tree-node-item {
  display: flex;
  align-items: center;
  padding: 4px 8px;
  transition: background-color 0.2s;
  user-select: none;
}

.tree-node-item:hover {
  background-color: rgba(0, 0, 0, 0.04);
}

/* 统一所有树图标的样式 */
.tree-icon {
  margin-right: 4px;
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

/* 文件夹/文件占位符，保持布局一致 */
.tree-icon-placeholder {
  display: inline-block;
  width: 20px;
  height: 20px;
  margin-right: 4px;
  flex-shrink: 0;
}

/* 折叠图标样式 */
.fold-icon {
  cursor: pointer;
  transition: opacity 0.2s;
}

.fold-icon:hover {
  opacity: 0.7;
}

/* 操作图标（暂存/取消暂存）样式 */
.action-icon {
  cursor: pointer;
  transition: opacity 0.2s;
}

.action-icon:hover {
  opacity: 0.7;
}

/* 节点名称 */
.node-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
