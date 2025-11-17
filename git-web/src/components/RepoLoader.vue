<template>
  <v-card>
    <v-card-title>加载本地仓库</v-card-title>
    <v-card-text>
      <v-text-field v-model="path" label="仓库路径" />
      <v-btn color="primary" @click="load">加载</v-btn>
      <v-chip-group v-model="recentIndex" class="mt-3">
        <v-chip v-for="(p,i) in recent" :key="i" @click="useRecent(p)">{{ p }}</v-chip>
      </v-chip-group>
      <div class="mt-2" v-if="error" style="color:red">{{ error }}</div>
    </v-card-text>
  </v-card>
  </template>
<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'
const emit = defineEmits(['loaded'])
const path = ref('')
const recent = ref([])
const recentIndex = ref(null)
const error = ref('')
onMounted(() => {
  const r = JSON.parse(localStorage.getItem('recentRepos')||'[]')
  recent.value = r
})
function saveRecent(p){
  const r = JSON.parse(localStorage.getItem('recentRepos')||'[]')
  const arr = [p, ...r.filter(x=>x!==p)].slice(0,5)
  localStorage.setItem('recentRepos', JSON.stringify(arr))
  recent.value = arr
}
async function load(){
  error.value = ''
  try{
    const res = await api.post('/repo/load',{ path: path.value })
    emit('loaded', path.value)
    saveRecent(path.value)
  }catch(e){
    error.value = '路径无效或非 Git 仓库'
  }
}
function useRecent(p){
  path.value = p
}
</script>