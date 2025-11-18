import { app as r, BrowserWindow as a } from "electron";
import { spawn as d } from "child_process";
import t from "path";
import { fileURLToPath as p } from "url";
const m = p(import.meta.url), l = t.dirname(m);
let n, e;
function h() {
  if (process.env.NODE_ENV === "development") {
    console.log('开发模式：请手动启动后端 (dotnet run --urls "http://localhost:9002")');
    return;
  }
  const i = t.join(process.resourcesPath, "server", "server.exe");
  console.log("启动后端服务:", i), e = d(i, ["--urls", "http://localhost:9002"]), e.stdout.on("data", (o) => {
    console.log(`[API] ${o}`);
  }), e.stderr.on("data", (o) => {
    console.error(`[API Error] ${o}`);
  }), e.on("error", (o) => {
    console.error("后端启动失败:", o);
  }), e.on("exit", (o) => {
    console.log(`后端进程退出，代码: ${o}`);
  });
}
function c() {
  n = new a({
    width: 1280,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: t.join(l, "preload.js"),
      contextIsolation: !0,
      nodeIntegration: !1
    },
    icon: t.join(l, "../resources/icon.ico")
  }), process.env.NODE_ENV === "development" ? (n.loadURL("http://localhost:9001"), n.webContents.openDevTools()) : n.loadFile(t.join(l, "../dist/index.html")), n.on("closed", () => {
    n = null;
  });
}
r.whenReady().then(() => {
  console.log("Electron 应用启动中..."), h(), setTimeout(() => {
    console.log("创建主窗口..."), c();
  }, 2e3), r.on("activate", () => {
    a.getAllWindows().length === 0 && c();
  });
});
r.on("window-all-closed", () => {
  process.platform !== "darwin" && r.quit();
});
r.on("quit", () => {
  e && (console.log("正在关闭后端进程..."), e.kill());
});
process.on("uncaughtException", (s) => {
  console.error("未捕获的异常:", s);
});
