<template>
  <v-card>
    <v-card-title>提交历史</v-card-title>
    <v-card-text>
      <v-row class="mb-2">
        <v-col cols="12" sm="6"><v-text-field v-model="q" label="搜索提交消息"/></v-col>
        <v-col cols="12" sm="6"><v-text-field v-model="author" label="作者筛选"/></v-col>
      </v-row>
      <v-btn color="primary" class="mb-2" @click="refresh">查询</v-btn>
      <v-btn color="secondary" class="mb-2 ml-2" :disabled="!hasSelected" @click="downloadSelected">批量下载所选</v-btn>
      <v-list>
        <v-list-item v-for="c in items" :key="c.sha">
          <v-list-item-title>{{ c.shaShort }} {{ c.message }}</v-list-item-title>
          <v-list-item-subtitle>{{ c.author }} · {{ formatDate(c.date) }} · 变更 {{ c.changedFiles }} 个文件</v-list-item-subtitle>
          <template #append>
            <v-checkbox :model-value="selectedSet.has(c.sha)" @update:modelValue="val => toggle(c.sha, val)"  />
            <v-btn size="small" @click="downloadCommit(c.sha)">下载文件</v-btn>
          </template>
        </v-list-item>
      </v-list>
      <v-btn class="mt-2" @click="loadMore">加载更多</v-btn>
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
const hasSelected = computed(()=> selectedSet.value.size > 0)
function formatDate(d){
  const dt = new Date(d)
  return dt.toLocaleString()
}
async function fetch(){
  if(!props.path) return
  const res = await api.get('/repo/commits',{ params:{ path: props.path, page: page.value, pageSize: pageSize.value, q: q.value, author: author.value }})
  const arr = res.data.items || []
  if(page.value===1) items.value = arr
  else items.value = [...items.value, ...arr]
}
function refresh(){ page.value = 1; fetch() }
function loadMore(){ page.value += 1; fetch() }
async function downloadCommit(sha){
  const res = await api.post('/git/download-commit',{ path: props.path, sha },{ responseType:'blob' })
  saveBlob(res.data, `commit_${sha.substring(0,7)}.zip`)
}
async function downloadSelected(){
  const shas = Array.from(selectedSet.value)
  if(shas.length===0) return
  const ordered = items.value.filter(x=>shas.includes(x.sha)).sort((a,b)=> new Date(a.date)-new Date(b.date)).map(x=>x.sha)
  const res = await api.post('/git/download-commits',{ path: props.path, shas: ordered },{ responseType:'blob' })
  saveBlob(res.data, 'commits.zip')
}
function toggle(sha, val){
  const set = new Set(selectedSet.value)
  if(val) set.add(sha)
  else set.delete(sha)
  selectedSet.value = set
}
function saveBlob(blob, name){
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}
watch(()=>props.path, ()=>{ page.value=1; fetch() }, { immediate:true })
</script>