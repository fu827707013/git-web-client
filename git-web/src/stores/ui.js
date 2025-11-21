import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useUiStore = defineStore('ui', () => {
  // 当前视图：'all-commits' | 'changes'
  const currentView = ref('all-commits')

  // 左侧面板宽度
  const leftPanelWidth = ref(200)

  // 底部面板状态
  const bottomPanelVisible = ref(false)
  const bottomPanelHeight = ref(300)
  const bottomPanelActiveTab = ref('commit') // 'commit' | 'changes' | 'file-tree'

  // 主题
  const theme = ref('light')

  // 组件大小: 'x-small' | 'small' | 'default' | 'large' | 'x-large'
  const componentSize = ref('small')

  // 组件密度: 'compact' | 'comfortable' | 'default'
  const componentDensity = ref('compact')

  // 切换视图
  function switchView(viewName) {
    currentView.value = viewName
  }

  // 显示底部面板
  function showBottomPanel(tab = 'commit') {
    bottomPanelVisible.value = true
    bottomPanelActiveTab.value = tab
  }

  // 隐藏底部面板
  function hideBottomPanel() {
    bottomPanelVisible.value = false
  }

  // 切换底部面板标签页
  function switchBottomPanelTab(tab) {
    bottomPanelActiveTab.value = tab
  }

  // 切换主题
  function toggleTheme() {
    theme.value = theme.value === 'light' ? 'dark' : 'light'
    localStorage.setItem('theme', theme.value)
  }

  // 初始化主题
  function initTheme() {
    const savedTheme = localStorage.getItem('theme') || 'light'
    theme.value = savedTheme
  }

  // 设置组件大小
  function setComponentSize(size) {
    componentSize.value = size
    localStorage.setItem('componentSize', size)
  }

  // 设置组件密度
  function setComponentDensity(density) {
    componentDensity.value = density
    localStorage.setItem('componentDensity', density)
  }

  // 初始化 UI 设置
  function initUiSettings() {
    const savedTheme = localStorage.getItem('theme') || 'light'
    const savedSize = localStorage.getItem('componentSize') || 'small'
    const savedDensity = localStorage.getItem('componentDensity') || 'compact'

    theme.value = savedTheme
    componentSize.value = savedSize
    componentDensity.value = savedDensity
  }

  return {
    currentView,
    leftPanelWidth,
    bottomPanelVisible,
    bottomPanelHeight,
    bottomPanelActiveTab,
    theme,
    componentSize,
    componentDensity,
    switchView,
    showBottomPanel,
    hideBottomPanel,
    switchBottomPanelTab,
    toggleTheme,
    initTheme,
    setComponentSize,
    setComponentDensity,
    initUiSettings
  }
})
