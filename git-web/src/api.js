import axios from 'axios'

// 检测是否在 Electron 环境中，并配置正确的 API 地址
let baseURL = '/api'

if (window.electronAPI && window.electronAPI.isElectron) {
  // Electron 环境：直接使用后端 API 地址
  baseURL = window.electronAPI.apiUrl + '/api'
}

const api = axios.create({ baseURL })

export default api