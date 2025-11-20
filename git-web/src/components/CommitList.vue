<template>
  <v-card>
    <!-- <v-card-title>提交历史</v-card-title> -->
    <v-card-text>
      <v-row>
        <v-col cols="12" sm="6">
          <v-text-field v-model="q" label="搜索提交消息" density="compact" hide-details />
        </v-col>
        <v-col cols="12" sm="6">
          <v-text-field v-model="author" label="作者筛选" density="compact" hide-details />
        </v-col>
      </v-row>

      <div class="d-flex flex-wrap gap-2 my-3">
        <v-btn color="primary" @click="refresh">查询</v-btn>
        <v-btn color="secondary" :disabled="!hasSelected" @click="downloadSelected">批量下载所选</v-btn>
        <v-btn color="error" :disabled="!hasSelected" @click="clearSelection">清空选中</v-btn>

        <v-spacer />

        <v-menu>
          <template v-slot:activator="{ props }">
            <v-btn color="info" v-bind="props">
              条件批量下载
              <v-icon end>mdi-menu-down</v-icon>
            </v-btn>
          </template>
          <v-list density="compact">
            <v-list-item @click="showDateRangeDialog">
              <v-list-item-title>按日期范围下载</v-list-item-title>
            </v-list-item>
            <v-list-item @click="showShaRangeDialog">
              <v-list-item-title>按SHA范围下载</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>

      <!-- 提交记录列表 -->
      <v-data-table
        :headers="headers"
        :items="items"
        :items-per-page="-1"
        hide-default-footer
        class="elevation-1"
      >
        <template v-slot:item="{ item }">
          <tr
            @click="(e) => handleRowClick(e, { item })"
            @contextmenu="(e) => handleContextMenu(e, item)"
            class="table-row"
          >
            <td>
              <v-checkbox
                :model-value="selectedSet.has(item.sha)"
                @update:model-value="(val) => toggle(item.sha, val)"
                @click.stop
                hide-details
              />
            </td>
            <td><span class="text-mono">{{ item.sha?.substring(0, 7) }}</span></td>
            <td>{{ item.message }}</td>
            <td>{{ item.author }}</td>
            <td>{{ formatDate(item.date) }}</td>
            <td>
              <div class="d-flex gap-1">
                <v-btn
                  color="primary"
                  @click.stop="downloadCommit(item.sha)"
                >
                  下载
                </v-btn>
                <v-btn
                  variant="outlined"
                  @click.stop="copySha(item.sha)"
                  title="复制SHA"
                >
                  <v-icon>mdi-content-copy</v-icon>
                </v-btn>
              </div>
            </td>
          </tr>
        </template>
      </v-data-table>

      <div class="text-center mt-4" v-if="items.length > 0">
        <v-btn color="primary" @click="loadMore">加载更多</v-btn>
      </div>

      <!-- Context Menu -->
      <div
        v-if="contextMenu.show"
        ref="contextMenuRef"
        class="context-menu-container"
        :style="{
          position: 'fixed',
          left: contextMenu.x + 'px',
          top: contextMenu.y + 'px',
          zIndex: 9999
        }"
        @click.stop
      >
        <v-card elevation="8" class="context-menu-card">
          <v-list density="compact">
            <v-list-item @click="showCommitDetails" class="context-menu-item">
              <template v-slot:prepend>
                <v-icon size="small">mdi-information</v-icon>
              </template>
              <v-list-item-title>查看提交详情</v-list-item-title>
            </v-list-item>
            <v-list-item @click="downloadCommit(contextMenu.item?.sha)" class="context-menu-item">
              <template v-slot:prepend>
                <v-icon size="small">mdi-download</v-icon>
              </template>
              <v-list-item-title>下载此提交</v-list-item-title>
            </v-list-item>
            <v-divider></v-divider>
            <v-list-item @click="copyCommitSha" class="context-menu-item">
              <template v-slot:prepend>
                <v-icon size="small">mdi-content-copy</v-icon>
              </template>
              <v-list-item-title>复制 SHA</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </div>

      <!-- Commit Detail Dialog -->
      <CommitDetailDialog
        v-model="detailDialog.show"
        :path="path"
        :sha="detailDialog.sha"
      />

      <!-- Date Range Download Dialog -->
      <v-dialog v-model="dateRangeDialog.show" max-width="500px">
        <v-card>
          <v-card-title>按日期范围下载</v-card-title>
          <v-card-text>
            <v-row>
              <v-col cols="12">
                <v-text-field
                  v-model="dateRangeDialog.startDate"
                  label="开始日期"
                  type="datetime-local"
                  density="compact"
                  hide-details
                />
              </v-col>
              <v-col cols="12">
                <v-text-field
                  v-model="dateRangeDialog.endDate"
                  label="结束日期"
                  type="datetime-local"
                  density="compact"
                  hide-details
                />
              </v-col>
            </v-row>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn @click="dateRangeDialog.show = false">取消</v-btn>
            <v-btn color="primary" @click="downloadByDateRange" :loading="dateRangeDialog.loading">下载</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- SHA Range Download Dialog -->
      <v-dialog v-model="shaRangeDialog.show" max-width="500px">
        <v-card>
          <v-card-title>按SHA范围下载</v-card-title>
          <v-card-text>
            <v-row>
              <v-col cols="12">
                <v-text-field
                  v-model="shaRangeDialog.startSha"
                  label="开始SHA（包含）"
                  density="compact"
                  hide-details
                  placeholder="输入完整SHA或前7位"
                />
              </v-col>
              <v-col cols="12">
                <v-text-field
                  v-model="shaRangeDialog.endSha"
                  label="结束SHA（包含）"
                  density="compact"
                  hide-details
                  placeholder="输入完整SHA或前7位"
                />
              </v-col>
            </v-row>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn @click="shaRangeDialog.show = false">取消</v-btn>
            <v-btn color="primary" @click="downloadByShaRange" :loading="shaRangeDialog.loading">下载</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </v-card-text>
  </v-card>
</template>
<script setup>
import { ref, watch, computed, onMounted, onUnmounted } from 'vue'
import api from '../api'
import CommitDetailDialog from './CommitDetailDialog.vue'

const props = defineProps({ path: String })
const page = ref(1)
const pageSize = ref(20)
const q = ref('')
const author = ref('')
const items = ref([])
const selectedSet = ref(new Set())
const lastClickedIndex = ref(null)
const hasSelected = computed(() => selectedSet.value.size > 0)

const contextMenu = ref({
  show: false,
  x: 0,
  y: 0,
  item: null
})

const contextMenuRef = ref(null)

const detailDialog = ref({
  show: false,
  sha: null
})

const dateRangeDialog = ref({
  show: false,
  startDate: '',
  endDate: '',
  loading: false
})

const shaRangeDialog = ref({
  show: false,
  startSha: '',
  endSha: '',
  loading: false
})

// 点击外部关闭菜单
function handleClickOutside(event) {
  if (contextMenu.value.show && contextMenuRef.value && !contextMenuRef.value.contains(event.target)) {
    contextMenu.value.show = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})

const headers = [
  { title: '选择', key: 'checkbox', sortable: false, width: '80px' },
  { title: 'SHA', key: 'sha', sortable: false, width: '100px' },
  { title: '提交消息', key: 'message', sortable: false },
  { title: '作者', key: 'author', sortable: false, width: '150px' },
  { title: '日期', key: 'date', sortable: false, width: '180px' },
  { title: '操作', key: 'actions', sortable: false, width: '140px' }
]
function formatDate(d) {
  const dt = new Date(d)
  return dt.toLocaleString()
}
async function fetch() {
  if (!props.path) return
  const res = await api.get('/repo/commits', { params: { path: props.path, page: page.value, pageSize: pageSize.value, q: q.value, author: author.value } })
  const arr = res.data.items || []
  if (page.value === 1) items.value = arr
  else items.value = [...items.value, ...arr]
}
function refresh() { page.value = 1; fetch() }
function loadMore() { page.value += 1; fetch() }
async function downloadCommit(sha) {
  const res = await api.post('/git/download-commit', { path: props.path, sha }, { responseType: 'blob' })
  saveBlob(res.data, `commit_${sha.substring(0, 7)}.zip`)
}
async function downloadSelected() {
  const shas = Array.from(selectedSet.value)
  if (shas.length === 0) return
  const ordered = items.value.filter(x => shas.includes(x.sha)).sort((a, b) => new Date(a.date) - new Date(b.date)).map(x => x.sha)
  const res = await api.post('/git/download-commits', { path: props.path, shas: ordered }, { responseType: 'blob' })
  saveBlob(res.data, 'commits.zip')
}
function toggle(sha, val) {
  const set = new Set(selectedSet.value)
  if (val) set.add(sha)
  else set.delete(sha)
  selectedSet.value = set
}

function handleRowClick(event, { item }) {
  const clickedIndex = items.value.findIndex(x => x.sha === item.sha)

  // 如果按住 Shift 键且之前有点击过行,进行范围选择
  if (event.shiftKey && lastClickedIndex.value !== null) {
    const start = Math.min(lastClickedIndex.value, clickedIndex)
    const end = Math.max(lastClickedIndex.value, clickedIndex)
    const set = new Set(selectedSet.value)

    for (let i = start; i <= end; i++) {
      set.add(items.value[i].sha)
    }

    selectedSet.value = set
  } else {
    // 普通点击,切换当前行的选中状态
    const set = new Set(selectedSet.value)
    if (set.has(item.sha)) {
      set.delete(item.sha)
    } else {
      set.add(item.sha)
    }
    selectedSet.value = set
  }

  lastClickedIndex.value = clickedIndex
}
function clearSelection() {
  selectedSet.value = new Set()
  lastClickedIndex.value = null
}

function handleContextMenu(event, item) {
  event.preventDefault()
  event.stopPropagation()

  contextMenu.value.show = false
  contextMenu.value.item = item

  // 使用 clientX/clientY 配合 position: fixed，这样可以精确定位
  contextMenu.value.x = event.clientX
  contextMenu.value.y = event.clientY

  // 使用 nextTick 确保位置更新后再显示
  setTimeout(() => {
    contextMenu.value.show = true
  }, 10)
}

function showCommitDetails() {
  if (contextMenu.value.item) {
    detailDialog.value.sha = contextMenu.value.item.sha
    detailDialog.value.show = true
  }
  contextMenu.value.show = false
}

function copyCommitSha() {
  if (contextMenu.value.item) {
    const sha = contextMenu.value.item.sha
    copySha(sha)
  }
  contextMenu.value.show = false
}

function copySha(sha) {
  navigator.clipboard.writeText(sha).then(() => {
    console.log('SHA copied to clipboard:', sha)
    // 可以添加一个提示
    alert('SHA已复制到剪贴板: ' + sha.substring(0, 7))
  }).catch(err => {
    console.error('Failed to copy SHA:', err)
    alert('复制失败，请重试')
  })
}

function saveBlob(blob, name) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

function showDateRangeDialog() {
  // 设置默认日期为最近7天
  const now = new Date()
  const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)

  dateRangeDialog.value.endDate = formatDateTimeLocal(now)
  dateRangeDialog.value.startDate = formatDateTimeLocal(weekAgo)
  dateRangeDialog.value.show = true
}

function showShaRangeDialog() {
  shaRangeDialog.value.startSha = ''
  shaRangeDialog.value.endSha = ''
  shaRangeDialog.value.show = true
}

function formatDateTimeLocal(date) {
  const pad = (num) => String(num).padStart(2, '0')
  const year = date.getFullYear()
  const month = pad(date.getMonth() + 1)
  const day = pad(date.getDate())
  const hours = pad(date.getHours())
  const minutes = pad(date.getMinutes())
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

async function downloadByDateRange() {
  if (!dateRangeDialog.value.startDate || !dateRangeDialog.value.endDate) {
    alert('请选择开始日期和结束日期')
    return
  }

  const startDate = new Date(dateRangeDialog.value.startDate)
  const endDate = new Date(dateRangeDialog.value.endDate)

  if (startDate > endDate) {
    alert('开始日期不能晚于结束日期')
    return
  }

  // 筛选符合日期范围的提交
  const filtered = items.value.filter(item => {
    const commitDate = new Date(item.date)
    return commitDate >= startDate && commitDate <= endDate
  })

  if (filtered.length === 0) {
    alert('在此日期范围内没有找到提交记录')
    return
  }

  // 按日期排序
  const sorted = filtered.sort((a, b) => new Date(a.date) - new Date(b.date))
  const shas = sorted.map(x => x.sha)

  dateRangeDialog.value.loading = true
  try {
    const res = await api.post('/git/download-commits', { path: props.path, shas }, { responseType: 'blob' })
    const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-')
    saveBlob(res.data, `commits_${timestamp}.zip`)
    dateRangeDialog.value.show = false
  } catch (e) {
    alert('下载失败: ' + (e.response?.data?.error || e.message))
  } finally {
    dateRangeDialog.value.loading = false
  }
}

async function downloadByShaRange() {
  if (!shaRangeDialog.value.startSha || !shaRangeDialog.value.endSha) {
    alert('请输入开始SHA和结束SHA')
    return
  }

  const startSha = shaRangeDialog.value.startSha.trim()
  const endSha = shaRangeDialog.value.endSha.trim()

  // 在当前列表中查找匹配的SHA
  const startIndex = items.value.findIndex(item =>
    item.sha.startsWith(startSha) || item.sha === startSha
  )
  const endIndex = items.value.findIndex(item =>
    item.sha.startsWith(endSha) || item.sha === endSha
  )

  if (startIndex === -1) {
    alert('未找到开始SHA: ' + startSha)
    return
  }
  if (endIndex === -1) {
    alert('未找到结束SHA: ' + endSha)
    return
  }

  // 获取范围内的所有提交（包含开始和结束）
  const start = Math.min(startIndex, endIndex)
  const end = Math.max(startIndex, endIndex)
  const rangeItems = items.value.slice(start, end + 1)

  // 按日期排序
  const sorted = rangeItems.sort((a, b) => new Date(a.date) - new Date(b.date))
  const shas = sorted.map(x => x.sha)

  shaRangeDialog.value.loading = true
  try {
    const res = await api.post('/git/download-commits', { path: props.path, shas }, { responseType: 'blob' })
    const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-')
    saveBlob(res.data, `commits_${timestamp}.zip`)
    shaRangeDialog.value.show = false
  } catch (e) {
    alert('下载失败: ' + (e.response?.data?.error || e.message))
  } finally {
    shaRangeDialog.value.loading = false
  }
}

watch(() => props.path, () => { page.value = 1; fetch() }, { immediate: true })
</script>

<style scoped>
.text-mono {
  font-family: monospace;
  font-size: 0.9em;
}

.table-row {
  cursor: pointer;
  transition: background-color 0.2s;
}

.table-row:hover {
  background-color: rgba(0, 0, 0, 0.04);
}

.v-theme--dark .table-row:hover {
  background-color: rgba(255, 255, 255, 0.08);
}

:deep(.v-data-table tbody tr) {
  cursor: pointer;
}

:deep(.v-data-table tbody tr:hover) {
  background-color: rgba(0, 0, 0, 0.04);
}

:deep(.v-data-table tbody tr.v-data-table__selected) {
  background-color: rgba(33, 150, 243, 0.1);
}

/* 右键菜单样式 */
.context-menu-container {
  user-select: none;
}

.context-menu-card {
  min-width: 200px;
  border-radius: 4px;
}

.context-menu-item {
  cursor: pointer;
  transition: background-color 0.2s;
}

.context-menu-item:hover {
  background-color: rgba(0, 0, 0, 0.08);
}

.v-theme--dark .context-menu-item:hover {
  background-color: rgba(255, 255, 255, 0.12);
}
</style>