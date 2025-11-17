<template>
  <v-card>
    <v-card-title>提交历史</v-card-title>
    <v-card-text>
      <v-row class="mb-2">
        <v-col cols="12" sm="6"><v-text-field v-model="q" label="搜索提交消息" /></v-col>
        <v-col cols="12" sm="6"><v-text-field v-model="author" label="作者筛选" /></v-col>
      </v-row>
      <v-btn color="primary" class="mb-2" @click="refresh">查询</v-btn>
      <v-btn color="secondary" class="mb-2 ml-2" :disabled="!hasSelected" @click="downloadSelected">批量下载所选</v-btn>
      <v-btn color="error" class="mb-2 ml-2" :disabled="!hasSelected" @click="clearSelection">清空选中</v-btn>

      <!-- 提交记录列表 -->
      <v-data-table
        :headers="headers"
        :items="items"
        :items-per-page="pageSize"
        hide-default-footer
        class="elevation-1"
        @click:row="handleRowClick"
      >
        <template v-slot:item.checkbox="{ item }">
          <v-checkbox
            :model-value="selectedSet.has(item.sha)"
            @update:model-value="(val) => toggle(item.sha, val)"
            @click.stop
            hide-details
          />
        </template>
        <template v-slot:item.sha="{ item }">
          <span class="text-mono">{{ item.sha?.substring(0, 7) }}</span>
        </template>
        <template v-slot:item.date="{ item }">
          {{ formatDate(item.date) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-btn
            size="small"
            color="primary"
            @click.stop="downloadCommit(item.sha)"
          >
            下载
          </v-btn>
        </template>
      </v-data-table>

      <div class="text-center mt-4" v-if="items.length > 0">
        <v-btn color="primary" @click="loadMore">加载更多</v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>
<script setup>
import { ref, watch, computed } from 'vue'
import api from '../api'
const props = defineProps({ path: String })
const page = ref(1)
const pageSize = ref(20)
const q = ref('')
const author = ref('')
const items = ref([])
const selectedSet = ref(new Set())
const lastClickedIndex = ref(null)
const hasSelected = computed(() => selectedSet.value.size > 0)

const headers = [
  { title: '选择', key: 'checkbox', sortable: false, width: '80px' },
  { title: 'SHA', key: 'sha', sortable: false, width: '100px' },
  { title: '提交消息', key: 'message', sortable: false },
  { title: '作者', key: 'author', sortable: false, width: '150px' },
  { title: '日期', key: 'date', sortable: false, width: '180px' },
  { title: '操作', key: 'actions', sortable: false, width: '100px' }
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
watch(() => props.path, () => { page.value = 1; fetch() }, { immediate: true })
</script>

<style scoped>
.text-mono {
  font-family: monospace;
  font-size: 0.9em;
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
</style>