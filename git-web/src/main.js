import { createApp, watch } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// 从 localStorage 读取 UI 设置
const savedSize = localStorage.getItem('componentSize') || 'small'
const savedDensity = localStorage.getItem('componentDensity') || 'compact'

const pinia = createPinia()
const vuetify = createVuetify({
  components,
  directives,
  defaults: {
    VBtn: {
      size: savedSize,
      density: savedDensity
    },
    VTextField: {
      density: savedDensity
    },
    VSelect: {
      density: savedDensity
    },
    VTextarea: {
      density: savedDensity
    },
    VCheckbox: {
      density: savedDensity
    },
    VIcon: {
      size: savedSize
    },
    VChip: {
      size: savedSize
    },
    VListItem: {
      density: savedDensity
    }
  }
})

const app = createApp(App)
  .use(pinia)
  .use(vuetify)

// 挂载后初始化 UI store
app.mount('#app')

// 提示用户修改设置后需要刷新页面才能完全生效
console.log('UI Settings loaded:', { size: savedSize, density: savedDensity })