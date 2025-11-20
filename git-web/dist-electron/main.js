import { app as o, BrowserWindow as u, dialog as c, Menu as d, shell as h } from "electron";
import { spawn as w } from "child_process";
import s from "path";
import { fileURLToPath as $ } from "url";
import f from "fs";
const v = $(import.meta.url), m = s.dirname(v);
let i, a;
const r = s.join(o.getPath("userData"), "app.log");
function l(t) {
  const e = `[${(/* @__PURE__ */ new Date()).toISOString()}] ${t}
`;
  console.log(t);
  try {
    f.appendFileSync(r, e);
  } catch (p) {
    console.error("写入日志失败:", p);
  }
}
function P() {
  if (process.env.NODE_ENV === "development")
    return;
  const n = s.join(process.resourcesPath, "server", "GitWeb.Api.exe");
  if (!f.existsSync(n)) {
    l(`[错误] 后端文件不存在: ${n}`), c.showErrorBox("启动错误", `后端服务器文件不存在:
${n}

日志文件位置:
${r}`);
    return;
  }
  try {
    a = w(n, ["--urls", "http://localhost:9002"], {
      cwd: s.dirname(n),
      env: { ...process.env }
    }), a.on("error", (e) => {
      l(`[错误] 后端启动失败: ${e.message}`), c.showErrorBox("后端启动失败", `${e.message}

日志文件位置:
${r}`);
    }), a.on("exit", (e) => {
      e !== 0 && e !== null && l(`[错误] 后端进程异常退出，代码: ${e}`);
    }), l("后端服务已启动");
  } catch (e) {
    l(`[错误] 启动后端时发生异常: ${e.message}`), c.showErrorBox("启动错误", `${e.message}

日志文件位置:
${r}`);
  }
}
function g() {
  const t = process.env.NODE_ENV === "development", n = t ? s.join(m, "preload.js") : s.join(o.getAppPath(), "dist-electron", "preload.js");
  i = new u({
    width: 1280,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: n,
      contextIsolation: !0,
      nodeIntegration: !1
    },
    icon: s.join(m, "../resources/icon.ico")
  }), t ? (i.loadURL("http://localhost:9001"), i.webContents.openDevTools()) : i.loadFile(s.join(m, "../dist/index.html")), i.on("closed", () => {
    i = null;
  });
  const e = [
    {
      label: "帮助",
      submenu: [
        {
          label: "打开日志文件",
          click: () => {
            h.openPath(r);
          }
        },
        {
          label: "打开日志文件夹",
          click: () => {
            h.showItemInFolder(r);
          }
        },
        {
          type: "separator"
        },
        {
          label: "关于",
          click: () => {
            c.showMessageBox(i, {
              type: "info",
              title: "关于",
              message: "Git Web Client",
              detail: `版本: ${o.getVersion()}
日志: ${r}`
            });
          }
        }
      ]
    }
  ], p = d.buildFromTemplate(e);
  d.setApplicationMenu(p);
}
o.whenReady().then(() => {
  l(`应用启动 - 版本: ${o.getVersion()}`), P(), setTimeout(() => {
    g();
  }, 2e3), o.on("activate", () => {
    u.getAllWindows().length === 0 && g();
  });
});
o.on("window-all-closed", () => {
  process.platform !== "darwin" && o.quit();
});
o.on("quit", () => {
  a && (console.log("正在关闭后端进程..."), a.kill());
});
process.on("uncaughtException", (t) => {
  console.error("未捕获的异常:", t);
});
