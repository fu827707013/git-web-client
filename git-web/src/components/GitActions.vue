<template>
  <v-card>
    <v-card-title>Git 操作</v-card-title>
    <v-card-text>
      <div class="mb-2">
        <v-text-field v-model="authorName" label="作者名称" />
        <v-text-field v-model="authorEmail" label="作者邮箱" />
      </div>
      <v-row>
        <v-col cols="12" md="4">
          <v-btn color="primary" @click="pull">Pull</v-btn>
        </v-col>
        <v-col cols="12" md="4">
          <v-btn color="secondary" @click="commit">Commit</v-btn>
        </v-col>
      </v-row>
      <div class="mt-2" v-if="msg">{{ msg }}</div>
    </v-card-text>
  </v-card>
</template>
<script setup>
import { ref } from 'vue'
import api from '../api'
const props = defineProps({ path: String })
const authorName = ref('')
const authorEmail = ref('')
const msg = ref('')
async function pull(){
  if(!props.path) return
  const res = await api.post('/git/pull',{ path: props.path, name: authorName.value||'User', email: authorEmail.value||'user@example.com', rebase: false })
  msg.value = `Pull: ${res.data.status}`
}
async function commit(){
  if(!props.path) return
  const files = []
  const message = 'web commit'
  try{
    const res = await api.post('/git/commit',{ path: props.path, files, message, authorName: authorName.value||'User', authorEmail: authorEmail.value||'user@example.com' })
    msg.value = `Commit: ${res.data.sha.substring(0,7)}`
  }catch(e){
    msg.value = 'Commit 失败'
  }
}
</script>