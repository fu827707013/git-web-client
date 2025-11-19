import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../api'

export const useGitStore = defineStore('git', () => {
  // 当前仓库的分支列表
  const branches = ref([])
  const currentBranch = ref(null)

  // 远程仓库列表
  const remotes = ref([])

  // 标签列表
  const tags = ref([])

  // 提交列表
  const commits = ref([])

  // 选中的提交
  const selectedCommit = ref(null)

  // 树形结构（后端返回，直接渲染）
  const unstagedTree = ref([])
  const stagedTree = ref([])

  // 文件数量
  const unstagedCount = ref(0)
  const stagedCount = ref(0)

  // 设置分支列表
  function setBranches(branchList) {
    branches.value = branchList
  }

  // 设置当前分支
  function setCurrentBranch(branch) {
    currentBranch.value = branch
  }

  // 设置远程仓库列表
  function setRemotes(remoteList) {
    remotes.value = remoteList
  }

  // 设置标签列表
  function setTags(tagList) {
    tags.value = tagList
  }

  // 设置提交列表
  function setCommits(commitList) {
    commits.value = commitList
  }

  // 添加更多提交（分页加载）
  function addCommits(commitList) {
    commits.value.push(...commitList)
  }

  // 设置选中的提交
  function selectCommit(commit) {
    selectedCommit.value = commit
  }

  // 递归计算树中的文件数量
  function countFilesInTree(tree) {
    let count = 0
    for (const node of tree) {
      if (node.f) {  // f = isFolder
        count += countFilesInTree(node.c)  // c = children
      } else {
        count++
      }
    }
    return count
  }

  // 递归收集树中所有文件路径
  function collectFilePathsFromTree(tree) {
    const paths = []
    for (const node of tree) {
      if (node.f) {  // f = isFolder
        paths.push(...collectFilePathsFromTree(node.c))  // c = children
      } else {
        paths.push(node.p)  // p = path (扁平化结构，直接在节点上)
      }
    }
    return paths
  }

  // 从树中移除文件节点
  function removeFileFromTree(tree, filePath) {
    for (let i = 0; i < tree.length; i++) {
      const node = tree[i]

      if (node.f) {  // f = isFolder
        // 递归查找子节点
        const removed = removeFileFromTree(node.c, filePath)  // c = children
        if (removed) {
          // 如果文件夹变空了，移除文件夹
          if (node.c.length === 0) {  // c = children
            tree.splice(i, 1)
          }
          return removed
        }
      } else if (node.p === filePath) {  // p = path (扁平化结构)
        // 找到文件，移除并返回
        return tree.splice(i, 1)[0]
      }
    }
    return null
  }

  // 添加文件节点到树（按路径找到正确位置插入）
  function addFileToTree(tree, fileNode) {
    if (!fileNode || !fileNode.p) return

    const pathParts = fileNode.p.split('/')

    // 如果只有一级（根文件），直接插入到根级
    if (pathParts.length === 1) {
      tree.push(fileNode)
      return
    }

    // 多级路径，需要找到或创建父文件夹
    let currentLevel = tree
    for (let i = 0; i < pathParts.length - 1; i++) {
      const folderName = pathParts[i]
      const folderPath = pathParts.slice(0, i + 1).join('/')

      // 查找是否已存在该文件夹
      let folder = currentLevel.find(node => node.p === folderPath && node.f)

      if (!folder) {
        // 创建文件夹节点
        folder = {
          n: folderName,
          p: folderPath,
          f: true,
          c: []
        }
        currentLevel.push(folder)
        // 按文件夹优先、名称排序
        currentLevel.sort((a, b) => {
          if (a.f !== b.f) return a.f ? -1 : 1
          return a.n.localeCompare(b.n)
        })
      }

      currentLevel = folder.c
    }

    // 在正确的位置插入文件
    currentLevel.push(fileNode)
    // 按文件夹优先、名称排序
    currentLevel.sort((a, b) => {
      if (a.f !== b.f) return a.f ? -1 : 1
      return a.n.localeCompare(b.n)
    })
  }

  // 暂存文件（调用后端 API）- 只做 API 调用，返回结果
  async function stageFile(filePath, repoPath) {
    if (!repoPath) {
      console.error('Repository path is required')
      return { success: false, error: 'Repository path is required' }
    }

    try {
      // 调用后端 API
      await api.post('/git/stage', { path: repoPath, filePath })
      return { success: true }
    } catch (e) {
      console.error('Failed to stage file:', e)
      return { success: false, error: e }
    }
  }

  // 取消暂存文件（调用后端 API）- 只做 API 调用，返回结果
  async function unstageFile(filePath, repoPath) {
    if (!repoPath) {
      console.error('Repository path is required')
      return { success: false, error: 'Repository path is required' }
    }

    try {
      // 调用后端 API
      await api.post('/git/unstage', { path: repoPath, filePath })
      return { success: true }
    } catch (e) {
      console.error('Failed to unstage file:', e)
      return { success: false, error: e }
    }
  }

  // 暂存所有文件（调用后端 API）
  async function stageAll(repoPath) {
    if (!repoPath) {
      console.error('Repository path is required')
      return
    }

    try {
      const filePaths = collectFilePathsFromTree(unstagedTree.value)
      await api.post('/git/stage-all', { path: repoPath, filePaths })

      // 移动整棵树
      stagedTree.value.push(...unstagedTree.value)
      stagedCount.value += unstagedCount.value
      unstagedTree.value = []
      unstagedCount.value = 0
    } catch (e) {
      console.error('Failed to stage all files:', e)
      throw e
    }
  }

  // 取消暂存所有文件（调用后端 API）
  async function unstageAll(repoPath) {
    if (!repoPath) {
      console.error('Repository path is required')
      return
    }

    try {
      const filePaths = collectFilePathsFromTree(stagedTree.value)
      await api.post('/git/unstage-all', { path: repoPath, filePaths })

      // 移动整棵树
      unstagedTree.value.push(...stagedTree.value)
      unstagedCount.value += stagedCount.value
      stagedTree.value = []
      stagedCount.value = 0
    } catch (e) {
      console.error('Failed to unstage all files:', e)
      throw e
    }
  }

  // 从后台加载仓库信息（分支、远程、标签 - 快速加载）
  async function loadRepoInfo(repoPath) {
    if (!repoPath) return

    try {
      // 并行加载分支、远程、标签（这些数据量小，快速加载）
      const [branchesRes, remotesRes, tagsRes] = await Promise.all([
        api.get(`/repo/branches?path=${encodeURIComponent(repoPath)}`),
        api.get(`/repo/remotes?path=${encodeURIComponent(repoPath)}`),
        api.get(`/repo/tags?path=${encodeURIComponent(repoPath)}`)
      ])

      branches.value = branchesRes.data || []
      remotes.value = remotesRes.data || []
      tags.value = tagsRes.data || []

      // 异步加载变更文件列表（不阻塞界面）
      loadFileStatus(repoPath)
    } catch (e) {
      console.error('Failed to load repo info:', e)
      // 如果加载失败，清空数据
      branches.value = []
      remotes.value = []
      tags.value = []
    }
  }

  // 异步加载文件状态（独立加载，不阻塞主界面）
  async function loadFileStatus(repoPath) {
    if (!repoPath) return

    try {
      // 先清空旧数据，显示加载状态
      unstagedTree.value = []
      stagedTree.value = []
      unstagedCount.value = 0
      stagedCount.value = 0

      console.log('Loading file status for:', repoPath)
      const startTime = Date.now()

      // 异步加载变更文件列表
      const statusRes = await api.get(`/repo/status?path=${encodeURIComponent(repoPath)}`)

      const loadTime = Date.now() - startTime
      const responseSize = JSON.stringify(statusRes.data).length
      console.log(`File status loaded in ${loadTime}ms, size: ${(responseSize / 1024).toFixed(2)}KB`)

      if (statusRes && statusRes.data) {
        console.log('Response data:', {
          hasUnstagedTree: !!statusRes.data.unstagedTree,
          hasStagedTree: !!statusRes.data.stagedTree,
          unstagedCount: statusRes.data.unstagedCount,
          stagedCount: statusRes.data.stagedCount,
          unstagedTreeLength: statusRes.data.unstagedTree?.length || 0,
          stagedTreeLength: statusRes.data.stagedTree?.length || 0
        })

        // 保存后端返回的树形结构和数量
        unstagedTree.value = statusRes.data.unstagedTree || []
        stagedTree.value = statusRes.data.stagedTree || []
        unstagedCount.value = statusRes.data.unstagedCount || 0
        stagedCount.value = statusRes.data.stagedCount || 0

        console.log('State updated:', {
          unstagedTreeLength: unstagedTree.value.length,
          stagedTreeLength: stagedTree.value.length,
          unstagedCount: unstagedCount.value,
          stagedCount: stagedCount.value
        })
      } else {
        console.warn('No data in response:', statusRes)
      }
    } catch (e) {
      console.error('Failed to load file status:', e)
      console.error('Error details:', {
        message: e.message,
        response: e.response,
        status: e.response?.status,
        statusText: e.response?.statusText
      })

      unstagedTree.value = []
      stagedTree.value = []
      unstagedCount.value = 0
      stagedCount.value = 0
    }
  }

  // Pull（拉取）
  async function pull(repoPath, name, email, rebase = false) {
    if (!repoPath) throw new Error('Repository path is required')

    try {
      const res = await api.post('/git/pull', { path: repoPath, name, email, rebase })
      return res.data.status
    } catch (e) {
      console.error('Failed to pull:', e)
      throw e
    }
  }

  // Push（推送）
  async function push(repoPath, remoteName = 'origin', username = null, password = null) {
    if (!repoPath) throw new Error('Repository path is required')

    try {
      const res = await api.post('/git/push', { path: repoPath, remoteName, username, password })
      return res.data.result
    } catch (e) {
      console.error('Failed to push:', e)
      throw e
    }
  }

  // Fetch（获取）
  async function fetch(repoPath, remoteName = 'origin') {
    if (!repoPath) throw new Error('Repository path is required')

    try {
      await api.post('/git/fetch', { path: repoPath, remoteName })
      return true
    } catch (e) {
      console.error('Failed to fetch:', e)
      throw e
    }
  }

  return {
    branches,
    currentBranch,
    remotes,
    tags,
    commits,
    selectedCommit,
    unstagedTree,
    stagedTree,
    unstagedCount,
    stagedCount,
    setBranches,
    setCurrentBranch,
    setRemotes,
    setTags,
    setCommits,
    addCommits,
    selectCommit,
    stageFile,
    unstageFile,
    stageAll,
    unstageAll,
    loadRepoInfo,
    loadFileStatus,
    pull,
    push,
    fetch
  }
})
