import { app, BrowserWindow } from "electron";
import { spawn } from "child_process";
import path from "path";
import { fileURLToPath } from "url";
const __filename$1 = fileURLToPath(import.meta.url);
const __dirname$1 = path.dirname(__filename$1);
let mainWindow;
let apiProcess;
function startBackendAPI() {
  const isDev = process.env.NODE_ENV === "development";
  if (isDev) {
    console.log('开发模式：请手动启动后端 (dotnet run --urls "http://localhost:9002")');
    return;
  }
  const serverPath = path.join(process.resourcesPath, "server", "server.exe");
  console.log("启动后端服务:", serverPath);
  apiProcess = spawn(serverPath, ["--urls", "http://localhost:9002"]);
  apiProcess.stdout.on("data", (data) => {
    console.log(`[API] ${data}`);
  });
  apiProcess.stderr.on("data", (data) => {
    console.error(`[API Error] ${data}`);
  });
  apiProcess.on("error", (err) => {
    console.error("后端启动失败:", err);
  });
  apiProcess.on("exit", (code) => {
    console.log(`后端进程退出，代码: ${code}`);
  });
}
function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: path.join(__dirname$1, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false
    },
    icon: path.join(__dirname$1, "../resources/icon.ico")
  });
  const isDev = process.env.NODE_ENV === "development";
  if (isDev) {
    mainWindow.loadURL("http://localhost:9001");
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(path.join(__dirname$1, "../dist/index.html"));
  }
  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}
app.whenReady().then(() => {
  console.log("Electron 应用启动中...");
  startBackendAPI();
  setTimeout(() => {
    console.log("创建主窗口...");
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
