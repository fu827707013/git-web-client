<template>
  <div>
    <!-- 添加新仓库表单 -->
    <v-text-field
      v-model="repoName"
      label="仓库名称"
      prepend-icon="mdi-tag"
      placeholder="例如：我的项目"
      class="mb-3"
      required
    />
    <v-text-field
      v-model="path"
      label="仓库路径"
      prepend-icon="mdi-folder"
      placeholder="例如：D:\projects\myrepo"
      class="mb-3"
      required
    />
    <div class="d-flex gap-2">
      <v-btn color="primary" @click="load" :loading="loading" block>
        <v-icon class="mr-1">mdi-check</v-icon>
        添加仓库
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
  </div>
</template>
<script setup>
import { ref } from 'vue'

const emit = defineEmits(['loaded'])

const path = ref('')
const repoName = ref('')
const error = ref('')
const success = ref('')
const loading = ref(false)

async function load() {
  // 验证输入
  if (!repoName.value.trim()) {
    error.value = '请输入仓库名称'
    return
  }
  if (!path.value.trim()) {
    error.value = '请输入仓库路径'
    return
  }

  error.value = ''
  success.value = ''
  loading.value = true

  try {
    // Emit 包含 name 和 path 的对象，由父组件调用 API
    emit('loaded', {
      name: repoName.value.trim(),
      path: path.value.trim()
    })

    success.value = `仓库 "${repoName.value}" 添加成功`

    // 1秒后清空表单
    setTimeout(() => {
      clearForm()
    }, 1000)
  } catch (e) {
    error.value = '添加仓库失败'
  } finally {
    loading.value = false
  }
}

function clearForm() {
  path.value = ''
  repoName.value = ''
  error.value = ''
  success.value = ''
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>