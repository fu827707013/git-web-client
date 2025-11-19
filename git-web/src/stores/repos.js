import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../api'

export const useReposStore = defineStore('repos', () => {
  // 已打开的仓库列表
  const openRepos = ref([])

  // 当前激活的仓库 ID
  const activeRepoId = ref(null)

  // 获取当前激活的仓库
  const activeRepo = computed(() => {
    return openRepos.value.find(repo => repo.id === activeRepoId.value)
  })

  // 从后台API加载已保存的仓库
  async function loadSavedRepos() {
    try {
      const res = await api.get('/repo/saved')
      const savedRepos = res.data || []

      // 将后台保存的仓库转换为 openRepos 格式
      openRepos.value = savedRepos.map(repo => ({
        id: repo.name, // 使用 name 作为 id
        name: repo.name,
        path: repo.path,
        hasChanges: false
      }))

      // 如果有仓库，激活第一个
      if (openRepos.value.length > 0 && !activeRepoId.value) {
        activeRepoId.value = openRepos.value[0].id
      }
    } catch (e) {
      console.error('Failed to load saved repos from backend:', e)
    }
  }

  // 添加仓库（会调用后台API保存）
  async function addRepo(repo) {
    try {
      // 检查是否已存在相同路径的仓库
      const existing = openRepos.value.find(r => r.path === repo.path)
      if (existing) {
        // 如果已存在，只需激活它
        activeRepoId.value = existing.id
        return existing
      }

      // 调用后台API加载并保存仓库
      await api.post('/repo/load', {
        path: repo.path,
        name: repo.name
      })

      const newRepo = {
        id: repo.name, // 使用 name 作为 id
        name: repo.name,
        path: repo.path,
        hasChanges: false
      }
      openRepos.value.push(newRepo)
      activeRepoId.value = newRepo.id

      return newRepo
    } catch (e) {
      console.error('Failed to add repo:', e)
      throw e
    }
  }

  // 关闭仓库（会调用后台API删除）
  async function closeRepo(id) {
    try {
      const repo = openRepos.value.find(r => r.id === id)
      if (!repo) return

      // 调用后台API删除
      await api.delete(`/repo/saved/${repo.name}`)

      // 从本地列表移除
      const index = openRepos.value.findIndex(r => r.id === id)
      if (index !== -1) {
        openRepos.value.splice(index, 1)
        // 如果关闭的是当前激活的仓库，切换到第一个仓库
        if (activeRepoId.value === id) {
          activeRepoId.value = openRepos.value.length > 0 ? openRepos.value[0].id : null
        }
      }
    } catch (e) {
      console.error('Failed to close repo:', e)
      throw e
    }
  }

  // 设置激活的仓库
  function setActiveRepo(id) {
    activeRepoId.value = id
  }

  // 更新仓库变更状态
  function setRepoHasChanges(id, hasChanges) {
    const repo = openRepos.value.find(r => r.id === id)
    if (repo) {
      repo.hasChanges = hasChanges
    }
  }

  return {
    openRepos,
    activeRepoId,
    activeRepo,
    addRepo,
    closeRepo,
    setActiveRepo,
    setRepoHasChanges,
    loadSavedRepos
  }
})
