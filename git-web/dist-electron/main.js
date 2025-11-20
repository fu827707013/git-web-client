import { app, BrowserWindow, dialog, Menu, shell } from "electron";
import { spawn } from "child_process";
import path from "path";
import { fileURLToPath } from "url";
import fs from "fs";
const __filename$1 = fileURLToPath(import.meta.url);
const __dirname$1 = path.dirname(__filename$1);
let mainWindow;
let apiProcess;
const logFilePath = path.join(app.getPath("userData"), "app.log");
function log(message) {
  const timestamp = (/* @__PURE__ */ new Date()).toISOString();
  const logMessage = `[${timestamp}] ${message}
`;
  console.log(message);
  try {
    fs.appendFileSync(logFilePath, logMessage);
  } catch (err) {
    console.error("写入日志失败:", err);
  }
}
function startBackendAPI() {
  const isDev = process.env.NODE_ENV === "development";
  if (isDev) {
    return;
  }
  const serverPath = path.join(process.resourcesPath, "server", "GitWeb.Api.exe");
  if (!fs.existsSync(serverPath)) {
    log(`[错误] 后端文件不存在: ${serverPath}`);
    dialog.showErrorBox("启动错误", `后端服务器文件不存在:
${serverPath}

日志文件位置:
${logFilePath}`);
    return;
  }
  try {
    apiProcess = spawn(serverPath, ["--urls", "http://localhost:9002"], {
      cwd: path.dirname(serverPath),
      env: { ...process.env }
    });
    apiProcess.on("error", (err) => {
      log(`[错误] 后端启动失败: ${err.message}`);
      dialog.showErrorBox("后端启动失败", `${err.message}

日志文件位置:
${logFilePath}`);
    });
    apiProcess.on("exit", (code) => {
      if (code !== 0 && code !== null) {
        log(`[错误] 后端进程异常退出，代码: ${code}`);
      }
    });
    log(`后端服务已启动`);
  } catch (err) {
    log(`[错误] 启动后端时发生异常: ${err.message}`);
    dialog.showErrorBox("启动错误", `${err.message}

日志文件位置:
${logFilePath}`);
  }
}
function createWindow() {
  const isDev = process.env.NODE_ENV === "development";
  const preloadPath = isDev ? path.join(__dirname$1, "preload.js") : path.join(app.getAppPath(), "dist-electron", "preload.js");
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
    icon: path.join(__dirname$1, "../resources/icon.ico")
  });
  if (isDev) {
    mainWindow.loadURL("http://localhost:9001");
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(path.join(__dirname$1, "../dist/index.html"));
  }
  mainWindow.on("closed", () => {
    mainWindow = null;
  });
  const template = [
    {
      label: "帮助",
      submenu: [
        {
          label: "打开日志文件",
          click: () => {
            shell.openPath(logFilePath);
          }
        },
        {
          label: "打开日志文件夹",
          click: () => {
            shell.showItemInFolder(logFilePath);
          }
        },
        {
          type: "separator"
        },
        {
          label: "关于",
          click: () => {
            dialog.showMessageBox(mainWindow, {
              type: "info",
              title: "关于",
              message: "Git Web Client",
              detail: `版本: ${app.getVersion()}
日志: ${logFilePath}`
            });
          }
        }
      ]
    }
  ];
  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);
}
app.whenReady().then(() => {
  log(`应用启动 - 版本: ${app.getVersion()}`);
  startBackendAPI();
  setTimeout(() => {
    createWindow();
  }, 2e3);
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
app.on("quit", () => {
  if (apiProcess) {
    console.log("正在关闭后端进程...");
    apiProcess.kill();
  }
});
process.on("uncaughtException", (error) => {
  console.error("未捕获的异常:", error);
});
