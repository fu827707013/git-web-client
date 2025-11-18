# Electron 桌面客户端集成方案（Windows）

## 1. 项目概述

### 1.1 当前架构
- **前端**: Vue 3 + Vite + Vuetify（浏览器运行）
- **后端**: .NET 10 Web API（独立服务）
- **部署**: 需要分别启动前后端

### 1.2 目标架构
- **应用类型**: Windows 桌面客户端
- **前端渲染**: Vue 3 在 Electron 中运行（固定端口：9001）
- **后端服务**: .NET API 随应用自动启动（固定端口：9002）
- **部署**: 一键安装，开箱即用

### 1.3 技术选型
- **Electron**: 桌面应用框架
- **electron-builder**: Windows 安装包打包工具（生成 .exe 安装程序）
- **.NET Self-Contained**: 自包含发布，无需用户安装 .NET 运行时

## 2. 目录结构设计

```
git-cliient/
│
├── git-web/                          # 前端项目根目录
│   │
│   ├── electron/                     # Electron 主进程代码
│   │   ├── main.js                  # 主进程入口（窗口管理、后端启动）
│   │   └── preload.js               # 预加载脚本（安全通信桥）
│   │
│   ├── src/                         # Vue 3 前端源码（保持不变）
│   │   ├── components/
│   │   ├── views/
│   │   └── ...
│   │
│   ├── public/                      # 静态资源
│   │
│   ├── resources/                   # Electron 打包资源
│   │   ├── icon.ico                # Windows 应用图标
│   │   └── installer.nsi           # NSIS 安装脚本（可选）
│   │
│   ├── dist/                        # Vite 构建输出（前端）
│   │
│   ├── dist-electron/               # Electron 主进程构建输出
│   │
│   ├── release/                     # 最终打包输出目录
│   │   └── Git-Web-Client-Setup-1.0.0.exe
│   │
│   ├── vite.config.js               # Vite 配置
│   ├── package.json                 # 项目配置
│   └── electron-builder.json        # Electron 打包配置
│
├── server/                          # .NET 后端项目
│   ├── Program.cs
│   ├── Controllers/
│   └── bin/
│       └── Release/
│           └── net10.0/
│               └── win-x64/         # .NET 自包含发布输出
│                   └── publish/
│                       ├── server.exe
│                       └── ...（所有运行时文件）
│
└── ELECTRON_INTEGRATION.md          # 本文档
```

## 3. 架构设计

```
┌──────────────────────────────────────────────────────┐
│  Git Web Client.exe (Electron 应用)                   │
│                                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │  主进程 (Main Process)                       │    │
│  │  - 创建应用窗口 (1280x800)                   │    │
│  │  - 启动后端 server.exe (http://localhost:9002)│   │
│  │  - 管理应用生命周期                           │    │
│  │  - 退出时清理子进程                           │    │
│  └─────────────────────────────────────────────┘    │
│                                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │  渲染进程 (Renderer Process)                 │    │
│  │  - Vue 3 应用 (端口: 9001)                   │    │
│  │  - Vuetify UI                                │    │
│  │  - 通过 Axios 调用 http://localhost:9002     │    │
│  └─────────────────────────────────────────────┘    │
│                                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │  后端子进程 (server.exe)                     │    │
│  │  - .NET 10 Web API                          │    │
│  │  - LibGit2Sharp                             │    │
│  │  - 端口: 9002                                │    │
│  └─────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

## 4. 实施步骤

### 步骤 1: 安装 Electron 依赖

在 `git-web` 目录下执行：

```bash
npm install --save-dev electron electron-builder
npm install --save-dev vite-plugin-electron vite-plugin-electron-renderer
npm install --save-dev concurrently cross-env wait-on
```

**依赖说明**:
- `electron`: Electron 核心
- `electron-builder`: 打包工具（生成 Windows 安装程序）
- `vite-plugin-electron`: Vite + Electron 集成
- `concurrently`: 同时运行多个命令
- `wait-on`: 等待服务启动

### 步骤 2: 创建 Electron 主进程文件

创建 `git-web/electron/main.js`

**核心功能**:
1. 创建主窗口
2. 启动后端 server.exe（端口：9002）
3. 加载前端页面（开发环境: http://localhost:9001，生产环境: dist/index.html）
4. 应用退出时终止后端进程

### 步骤 3: 创建预加载脚本

创建 `git-web/electron/preload.js`

**核心功能**:
- 安全地暴露必要的 API 给渲染进程
- 提供获取后端 API 地址的接口（http://localhost:9002）

### 步骤 4: 配置 Vite

修改 `git-web/vite.config.js`

**关键配置**:
- 引入 Electron 插件
- 设置前端开发端口为 9001
- 设置 base 为相对路径 `./`
- 配置 Electron 主进程编译

### 步骤 5: 修改 package.json

**主要修改**:
```json
{
  "name": "git-web-client",
  "version": "1.0.0",
  "main": "dist-electron/main.js",
  "scripts": {
    "dev": "vite --port 9001",
    "build": "vite build",
    "electron:dev": "vite --mode development --port 9001",
    "electron:start": "electron .",
    "electron:build": "npm run build && electron-builder --win"
  }
}
```

### 步骤 6: 创建 electron-builder 配置

创建 `git-web/electron-builder.json`

**关键配置**:
```json
{
  "appId": "com.gitwebclient.app",
  "productName": "Git Web Client",
  "directories": {
    "output": "release",
    "buildResources": "resources"
  },
  "files": [
    "dist/**/*",
    "dist-electron/**/*"
  ],
  "extraResources": [
    {
      "from": "../server/bin/Release/net10.0/win-x64/publish",
      "to": "server",
      "filter": ["**/*"]
    }
  ],
  "win": {
    "target": "nsis",
    "icon": "resources/icon.ico"
  },
  "nsis": {
    "oneClick": false,
    "allowToChangeInstallationDirectory": true,
    "createDesktopShortcut": true,
    "createStartMenuShortcut": true
  }
}
```

### 步骤 7: 修改后端启动配置

修改 `server/Program.cs` 或 `appsettings.json`，将 API 端口改为 9002：

**Program.cs 示例**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel 监听端口 9002
builder.WebHost.UseUrls("http://localhost:9002");

// ... 其他配置
```

或者在启动命令中指定端口：
```bash
dotnet run --urls "http://localhost:9002"
```

### 步骤 8: 发布 .NET 后端（自包含）

在 `server` 目录下执行：

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

**说明**:
- `-r win-x64`: 目标 Windows 64 位
- `--self-contained true`: 包含 .NET 运行时
- 输出目录: `server/bin/Release/net10.0/win-x64/publish`

### 步骤 9: 准备应用图标

将 Windows 图标放置在 `git-web/resources/icon.ico`

**要求**:
- 格式: .ico
- 尺寸: 256x256（推荐包含多尺寸）

## 5. 核心代码实现

### 5.1 主进程 (electron/main.js)

```javascript
const { app, BrowserWindow } = require('electron');
const { spawn } = require('child_process');
const path = require('path');

let mainWindow;
let apiProcess;

// 启动后端 API（端口 9002）
function startBackendAPI() {
  const isDev = process.env.NODE_ENV === 'development';

  if (isDev) {
    // 开发环境：假设后端已手动启动在 9002 端口
    console.log('开发模式：请手动启动后端 (dotnet run --urls "http://localhost:9002")');
    return;
  }

  // 生产环境：启动打包的 server.exe
  const serverPath = path.join(process.resourcesPath, 'server', 'server.exe');
  apiProcess = spawn(serverPath, ['--urls', 'http://localhost:9002']);

  apiProcess.stdout.on('data', (data) => {
    console.log(`[API] ${data}`);
  });

  apiProcess.stderr.on('data', (data) => {
    console.error(`[API Error] ${data}`);
  });

  apiProcess.on('error', (err) => {
    console.error('后端启动失败:', err);
  });

  apiProcess.on('exit', (code) => {
    console.log(`后端进程退出，代码: ${code}`);
  });
}

// 创建主窗口
function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    },
    icon: path.join(__dirname, '../resources/icon.ico')
  });

  const isDev = process.env.NODE_ENV === 'development';

  if (isDev) {
    // 开发环境：加载 Vite 开发服务器（端口 9001）
    mainWindow.loadURL('http://localhost:9001');
    mainWindow.webContents.openDevTools();
  } else {
    // 生产环境：加载打包后的 index.html
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'));
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

// 应用就绪后启动
app.whenReady().then(() => {
  startBackendAPI();

  // 等待后端启动后再创建窗口（给后端 2 秒启动时间）
  setTimeout(createWindow, 2000);

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

// 所有窗口关闭时退出（Windows 和 Linux）
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// 应用退出时清理后端进程
app.on('quit', () => {
  if (apiProcess) {
    console.log('正在关闭后端进程...');
    apiProcess.kill();
  }
});

// 处理未捕获的异常
process.on('uncaughtException', (error) => {
  console.error('未捕获的异常:', error);
});
```

### 5.2 预加载脚本 (electron/preload.js)

```javascript
const { contextBridge } = require('electron');

// 安全地暴露 API 给渲染进程
contextBridge.exposeInMainWorld('electronAPI', {
  // 平台信息
  platform: 'win32',

  // 后端 API 地址（端口 9002）
  apiUrl: 'http://localhost:9002',

  // 是否为 Electron 环境
  isElectron: true
});
```

### 5.3 Vite 配置 (vite.config.js)

```javascript
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import electron from 'vite-plugin-electron';
import renderer from 'vite-plugin-electron-renderer';

export default defineConfig({
  plugins: [
    vue(),
    electron([
      {
        // 主进程入口
        entry: 'electron/main.js',
        onstart(options) {
          options.startup();
        },
        vite: {
          build: {
            outDir: 'dist-electron'
          }
        }
      },
      {
        // 预加载脚本入口
        entry: 'electron/preload.js',
        onstart(options) {
          options.reload();
        },
        vite: {
          build: {
            outDir: 'dist-electron'
          }
        }
      }
    ]),
    renderer()
  ],
  // 设置为相对路径，适配 Electron 文件协议
  base: './',
  // 开发服务器配置（端口 9001）
  server: {
    port: 9001,
    strictPort: true
  },
  // 构建配置
  build: {
    outDir: 'dist',
    emptyOutDir: true
  }
});
```

### 5.4 前端 API 配置调整

修改前端的 API 基础地址，在 Vue 应用中配置：

**方式 1: 在 main.js 或 axios 配置文件中**
```javascript
import axios from 'axios';

// 检测是否在 Electron 环境中
if (window.electronAPI && window.electronAPI.isElectron) {
  axios.defaults.baseURL = window.electronAPI.apiUrl; // http://localhost:9002
} else {
  // 普通浏览器环境（开发时）
  axios.defaults.baseURL = 'http://localhost:9002'; // 或其他端口
}
```

**方式 2: 使用环境变量**
```javascript
// 创建 .env.development
VITE_API_URL=http://localhost:9002

// 创建 .env.production
VITE_API_URL=http://localhost:9002

// 在代码中使用
axios.defaults.baseURL = import.meta.env.VITE_API_URL;
```

## 6. 开发和打包流程

### 6.1 开发环境运行

**终端 1 - 启动后端（端口 9002）**:
```bash
cd server
dotnet run --urls "http://localhost:9002"
```

**终端 2 - 启动前端开发服务器（端口 9001）**:
```bash
cd git-web
npm run electron:dev
```

**终端 3 - 启动 Electron**:
```bash
cd git-web
set NODE_ENV=development
npm run electron:start
```

### 6.2 生产环境打包

**步骤 1: 发布后端**
```bash
cd server
dotnet publish -c Release -r win-x64 --self-contained true
```

**步骤 2: 构建前端并打包 Electron 应用**
```bash
cd git-web
npm run electron:build
```

**输出**: `git-web/release/Git-Web-Client-Setup-1.0.0.exe`

### 6.3 端口配置总结

| 组件 | 开发环境端口 | 生产环境端口 | 说明 |
|------|-------------|-------------|------|
| 前端 Vue | 9001 | - | 开发时 Vite Dev Server |
| 后端 API | 9002 | 9002 | .NET Web API |
| Electron | - | - | 加载前端页面 |

## 7. 测试清单

- [ ] 开发环境：前端在 9001 端口正常运行
- [ ] 开发环境：后端在 9002 端口正常运行
- [ ] 开发环境：Electron 窗口正常加载前端页面
- [ ] 开发环境：前端能正常调用 9002 端口的 API
- [ ] 生产环境：应用正常启动（双击 .exe）
- [ ] 生产环境：后端 API 自动启动在 9002 端口
- [ ] 生产环境：前端页面正常加载
- [ ] 仓库添加功能正常
- [ ] 提交历史查看正常
- [ ] 文件下载功能正常
- [ ] 关闭应用后 server.exe 进程自动终止
- [ ] 在无 .NET 环境的 Windows 电脑上能正常运行
- [ ] 端口 9001 和 9002 没有被其他程序占用

## 8. 注意事项

### 8.1 端口占用问题
- **前端端口**: 9001（仅开发环境）
- **后端端口**: 9002（开发和生产环境）
- 如果端口被占用，需要关闭占用端口的程序
- 检查端口占用：`netstat -ano | findstr "9001"` 或 `netstat -ano | findstr "9002"`
- 结束进程：`taskkill /PID <进程ID> /F`

### 8.2 防火墙设置
- Windows 防火墙可能拦截 9002 端口
- 首次运行时如果提示防火墙警告，选择"允许访问"

### 8.3 CORS 配置
- 由于前端和后端都在 localhost，需要确保后端配置了 CORS
- 在 `Program.cs` 中添加：
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:9001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors("AllowLocalhost");
```

### 8.4 安全性
- 本地应用，API 仅监听 localhost
- 不暴露到外网，无需额外安全措施
- 端口固定为 9002，便于管理

### 8.5 打包体积
- .NET 自包含发布约 70-100MB
- Electron 基础约 150MB
- 总安装包约 250-300MB

## 9. 常见问题

**Q: 开发时如何调试主进程？**
A: 在 VSCode 中配置 launch.json，或使用 `console.log` 输出到终端

**Q: 端口 9001 或 9002 被占用怎么办？**
A:
1. 查找占用端口的进程：`netstat -ano | findstr "9002"`
2. 结束进程：`taskkill /PID <进程ID> /F`
3. 或修改配置文件中的端口号

**Q: 打包后应用启动慢怎么办？**
A:
1. 添加启动画面（Splash Screen）
2. 优化后端启动逻辑
3. 使用 `PublishSingleFile=true` 减少文件数量

**Q: 如何验证后端是否启动成功？**
A:
1. 打开浏览器访问 `http://localhost:9002`
2. 或在任务管理器中查看是否有 server.exe 进程

**Q: 前端无法调用 API 怎么办？**
A:
1. 检查后端是否在 9002 端口运行
2. 检查前端 axios baseURL 是否配置正确
3. 检查浏览器控制台的网络请求错误信息

## 10. 后续优化方向

- [ ] 添加应用启动画面（显示"正在启动后端服务..."）
- [ ] 实现托盘图标和最小化到托盘
- [ ] 自动更新功能（electron-updater）
- [ ] 端口占用检测和自动切换
- [ ] 错误日志记录到文件
- [ ] 安装包体积优化
- [ ] 添加应用签名（避免 Windows SmartScreen 警告）
- [ ] 后端健康检查（启动失败时显示友好提示）
- [ ] 支持用户自定义端口号
