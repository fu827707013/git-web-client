import { app, BrowserWindow, dialog, Menu, shell } from 'electron';
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

// 获取当前文件的目录路径（ESM 中没有 __dirname）
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

let mainWindow;
let apiProcess;

// 日志文件路径
const logFilePath = path.join(app.getPath('userData'), 'app.log');

// 写入日志到文件和控制台
function log(message) {
  const timestamp = new Date().toISOString();
  const logMessage = `[${timestamp}] ${message}\n`;
  console.log(message);
  try {
    fs.appendFileSync(logFilePath, logMessage);
  } catch (err) {
    console.error('写入日志失败:', err);
  }
}

// 启动后端 API（端口 9002）
function startBackendAPI() {
  const isDev = process.env.NODE_ENV === 'development';

  if (isDev) {
    // 开发环境：假设后端已手动启动在 9002 端口
    return;
  }

  // 生产环境：启动打包的 GitWeb.Api.exe
  const serverPath = path.join(process.resourcesPath, 'server', 'GitWeb.Api.exe');

  // 检查后端文件是否存在
  if (!fs.existsSync(serverPath)) {
    log(`[错误] 后端文件不存在: ${serverPath}`);
    dialog.showErrorBox('启动错误', `后端服务器文件不存在:\n${serverPath}\n\n日志文件位置:\n${logFilePath}`);
    return;
  }

  try {
    apiProcess = spawn(serverPath, ['--urls', 'http://localhost:9002'], {
      cwd: path.dirname(serverPath),
      env: { ...process.env }
    });

    apiProcess.on('error', (err) => {
      log(`[错误] 后端启动失败: ${err.message}`);
      dialog.showErrorBox('后端启动失败', `${err.message}\n\n日志文件位置:\n${logFilePath}`);
    });

    apiProcess.on('exit', (code) => {
      if (code !== 0 && code !== null) {
        log(`[错误] 后端进程异常退出，代码: ${code}`);
      }
    });

    log(`后端服务已启动`);
  } catch (err) {
    log(`[错误] 启动后端时发生异常: ${err.message}`);
    dialog.showErrorBox('启动错误', `${err.message}\n\n日志文件位置:\n${logFilePath}`);
  }
}

// 创建主窗口
function createWindow() {
  const isDev = process.env.NODE_ENV === 'development';

  // 在开发环境使用 __dirname，在生产环境使用 app.getAppPath()
  const preloadPath = isDev
    ? path.join(__dirname, 'preload.js')
    : path.join(app.getAppPath(), 'dist-electron', 'preload.js');

  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: preloadPath,
      contextIsolation: true,
      nodeIntegration: false
    },
    icon: path.join(__dirname, '../resources/icon.ico')
  });

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

  // 创建菜单
  const template = [
    {
      label: '帮助',
      submenu: [
        {
          label: '打开日志文件',
          click: () => {
            shell.openPath(logFilePath);
          }
        },
        {
          label: '打开日志文件夹',
          click: () => {
            shell.showItemInFolder(logFilePath);
          }
        },
        {
          type: 'separator'
        },
        {
          label: '关于',
          click: () => {
            dialog.showMessageBox(mainWindow, {
              type: 'info',
              title: '关于',
              message: 'Git Web Client',
              detail: `版本: ${app.getVersion()}\n日志: ${logFilePath}`
            });
          }
        }
      ]
    }
  ];

  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);
}

// 应用就绪后启动
app.whenReady().then(() => {
  log(`应用启动 - 版本: ${app.getVersion()}`);

  startBackendAPI();

  // 等待后端启动后再创建窗口（给后端 2 秒启动时间）
  setTimeout(() => {
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
