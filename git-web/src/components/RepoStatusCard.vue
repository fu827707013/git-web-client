<template>
  <v-card>
    <v-card-title>仓库状态</v-card-title>
    <v-card-text>
      <div v-if="!path">未选择仓库</div>
      <div v-else>
        <v-row>
          <v-col cols="12" sm="6"><strong>分支</strong>: {{ status.branch }}</v-col>
          <v-col cols="12" sm="6"><strong>远程</strong>: {{ status.remote }}</v-col>
          <v-col cols="12" sm="6"><strong>未提交文件数</strong>: {{ status.notCommitted }}</v-col>
          <v-col cols="12" sm="6"><strong>未推送提交数</strong>: {{ status.notPushed }}</v-col>
        </v-row>
      </div>
    </v-card-text>
  </v-card>
</template>
<script setup>
import { ref, watch } from 'vue'
import api from '../api'
const props = defineProps({ path: String })
const status = ref({ branch:'', remote:'', notCommitted:0, notPushed:0 })
async function fetch(){
  if(!props.path) return
  const res = await api.get('/repo/status',{ params:{ path: props.path }})
  status.value = res.data
}
watch(()=>props.path, fetch, { immediate:true })
</script>