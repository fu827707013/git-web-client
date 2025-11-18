import { contextBridge } from 'electron';

// 安全地暴露 API 给渲染进程
contextBridge.exposeInMainWorld('electronAPI', {
  // 平台信息
  platform: 'win32',

  // 后端 API 地址（端口 9002）
  apiUrl: 'http://localhost:9002',

  // 是否为 Electron 环境
  isElectron: true
});
