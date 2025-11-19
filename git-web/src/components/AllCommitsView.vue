<template>
  <div class="all-commits-view">
    <!-- 直接使用现有的 CommitList 组件 -->
    <CommitList :path="path" @commit-selected="handleCommitSelected" />
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from 'vue'
import CommitList from './CommitList.vue'
import { useUiStore } from '../stores/ui'

const props = defineProps({
  path: {
    type: String,
    required: true
  }
})

const emit = defineEmits(['commit-selected'])
const uiStore = useUiStore()

function handleCommitSelected(commit) {
  // 显示底部详情面板
  uiStore.showBottomPanel('commit')
  emit('commit-selected', commit)
}
</script>

<style scoped>
.all-commits-view {
  height: 100%;
  overflow: auto;
}
</style>
