# Git Web Client - 文档索引

本项目包含完整的开发和使用文档，以下是文档清单和快速导航。

---

## 📚 核心文档

### [README.MD](./README.MD)
**完整的项目文档**

包含内容：
- 项目简介和界面预览
- 核心特性列表
- 技术栈介绍
- 项目结构说明
- 开发环境搭建
- 生产环境打包
- 应用使用说明
- 常见问题解答
- 版本规划

**适合人群**：所有用户、开发者、贡献者

---

### [QUICK_START.md](./QUICK_START.md)
**快速上手指南**

包含内容：
- 前置条件检查
- 快速启动步骤（开发模式）
- 一键启动脚本使用
- 打包发布步骤
- 首次使用指南
- 常见操作说明
- 开发调试技巧

**适合人群**：新用户、希望快速体验的开发者

---

### [ELECTRON_INTEGRATION.md](./ELECTRON_INTEGRATION.md)
**Electron 集成技术文档**

包含内容：
- Electron 架构设计
- 集成方案详细说明
- 实施步骤（从安装依赖到打包）
- 核心代码实现
- 端口配置（9001、9002）
- 目录结构设计
- 测试清单
- 技术细节和注意事项

**适合人群**：开发者、技术人员、需要深入了解实现细节的人

---

## 🚀 快捷脚本

### [start-dev.bat](./start-dev.bat)
**开发环境一键启动脚本（Windows）**

功能：
- 自动检查 Node.js 和 .NET SDK
- 首次运行自动安装依赖
- 自动启动后端 API（端口 9002）
- 自动启动前端开发服务器（端口 9001）
- 自动启动 Electron 桌面应用
- 在独立终端窗口中运行各服务

使用方法：
```bash
双击运行 start-dev.bat
```

---

### [build-release.bat](./build-release.bat)
**生产环境打包脚本（Windows）**

功能：
- 自动检查环境依赖
- 清理旧的构建文件
- 发布后端为自包含 .NET 应用
- 构建前端并打包 Electron 应用
- 生成 Windows 安装程序（.exe）
- 显示详细的打包进度和结果

使用方法：
```bash
双击运行 build-release.bat
```

输出文件：`git-web/release/Git-Web-Client-Setup-1.0.0.exe`

---

## 📂 目录结构

### 前端项目（git-web/）

```
git-web/
├── electron/              # Electron 主进程代码
│   ├── main.js           # 主进程入口（ESM）
│   └── preload.js        # 预加载脚本（ESM）
├── src/                  # Vue 3 源码
│   ├── components/       # 组件目录
│   ├── api.js           # API 配置（自动检测 Electron 环境）
│   ├── App.vue          # 根组件
│   └── main.js          # 入口文件
├── resources/            # 应用资源
│   └── icon.ico         # Windows 应用图标（需手动添加）
├── dist/                 # Vite 构建输出（前端）
├── dist-electron/        # Electron 主进程构建输出
├── release/              # electron-builder 打包输出
├── vite.config.js        # Vite 配置（集成 Electron 插件）
├── electron-builder.json # Electron 打包配置
└── package.json          # 项目配置（包含 Electron 脚本）
```

### 后端项目（server/）

```
server/
├── Controllers/          # API 控制器
│   ├── GitController.cs # Git 操作接口
│   └── RepoController.cs # 仓库管理接口
├── Services/             # 业务逻辑服务
│   ├── GitService.cs    # Git 操作服务（LibGit2Sharp）
│   └── RepoConfigService.cs # 仓库配置服务
├── Program.cs            # 启动入口（配置端口 9002）
└── bin/Release/          # 发布输出目录
    └── net10.0/
        └── win-x64/
            └── publish/  # 自包含发布文件
```

---

## 🔧 配置文件说明

### git-web/vite.config.js
**Vite 构建配置**

关键配置：
- 前端开发端口：9001
- Electron 插件集成
- 主进程和预加载脚本编译
- ESM 格式输出
- API 代理配置（/api → http://localhost:9002）

### git-web/electron-builder.json
**Electron 打包配置**

关键配置：
- 应用 ID 和名称
- 输出目录：release/
- 打包资源：前端 dist/、主进程 dist-electron/、后端 publish/
- Windows NSIS 安装程序配置
- 创建桌面和开始菜单快捷方式

### git-web/package.json
**前端项目配置**

关键配置：
- 主入口：dist-electron/main.js
- 开发脚本：
  - `npm run dev` - 启动 Vite（仅前端）
  - `npm run electron:dev` - 启动 Vite + Electron
  - `npm run electron:start` - 启动 Electron
  - `npm run electron:build` - 构建并打包

### server/Program.cs
**后端启动配置**

关键配置：
- API 监听端口：9002
- CORS 配置：允许前端端口 9001
- 控制器和服务注册

---

## 📋 端口配置总览

| 服务 | 端口 | 说明 | 配置文件 |
|------|------|------|----------|
| 前端开发服务器 | 9001 | Vite Dev Server | vite.config.js |
| 后端 API | 9002 | .NET Web API | Program.cs |

**修改端口**：
- 前端：修改 `git-web/vite.config.js` → `server.port`
- 后端：修改 `server/Program.cs` → `UseUrls` 参数

---

## 🎯 使用场景导航

### 场景 1：我是新用户，想快速体验
1. 查看 [QUICK_START.md](./QUICK_START.md)
2. 使用 [start-dev.bat](./start-dev.bat) 一键启动
3. 参考"首次使用指南"添加仓库

### 场景 2：我是开发者，想了解技术细节
1. 阅读 [README.MD](./README.MD) 了解整体架构
2. 阅读 [ELECTRON_INTEGRATION.md](./ELECTRON_INTEGRATION.md) 了解 Electron 集成
3. 查看项目目录结构和配置文件

### 场景 3：我想打包生产环境应用
1. 查看 [README.MD](./README.MD) 的"生产环境打包"章节
2. 使用 [build-release.bat](./build-release.bat) 一键打包
3. 在 `git-web/release/` 获取安装程序

### 场景 4：我遇到了问题
1. 查看 [README.MD](./README.MD) 的"常见问题"章节
2. 查看 [QUICK_START.md](./QUICK_START.md) 的"常见问题"章节
3. 检查相关配置文件

### 场景 5：我想贡献代码
1. 阅读 [README.MD](./README.MD) 的"贡献指南"章节
2. Fork 项目并创建特性分支
3. 参考代码规范提交 Pull Request

---

## 🔗 外部链接

- **[Vue.js 文档](https://vuejs.org/)** - Vue 3 官方文档
- **[Vuetify 文档](https://vuetifyjs.com/)** - Vuetify UI 组件库
- **[Electron 文档](https://www.electronjs.org/docs)** - Electron 桌面应用框架
- **[Vite 文档](https://vitejs.dev/)** - Vite 构建工具
- **[.NET 文档](https://docs.microsoft.com/dotnet/)** - .NET 开发平台
- **[LibGit2Sharp 文档](https://github.com/libgit2/libgit2sharp)** - .NET Git 操作库

---

## 📝 文档更新记录

- **2025-11-18** - 初始版本
  - 创建完整的 README.MD
  - 添加 QUICK_START.md 快速上手指南
  - 完善 ELECTRON_INTEGRATION.md 技术文档
  - 添加 start-dev.bat 和 build-release.bat 脚本
  - 创建 DOCS_INDEX.md 文档索引

---

## 💡 提示

- 所有文档均为 Markdown 格式，可在 VS Code、GitHub 等工具中查看
- 批处理脚本（.bat）仅支持 Windows 系统
- 如需 Linux/macOS 脚本，可参考 .bat 内容自行编写 shell 脚本
- 建议首次使用先阅读 QUICK_START.md，开发时参考 README.MD

---

**文档齐全，开发顺畅！祝您使用愉快！ 🎉**
