import { app, BrowserWindow } from 'electron';
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

// 获取当前文件的目录路径（ESM 中没有 __dirname）
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

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
  console.log('启动后端服务:', serverPath);

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
  console.log('Electron 应用启动中...');
  startBackendAPI();

  // 等待后端启动后再创建窗口（给后端 2 秒启动时间）
  setTimeout(() => {
    console.log('创建主窗口...');
    createWindow();
  }, 2000);

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
