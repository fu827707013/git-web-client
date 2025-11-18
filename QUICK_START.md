# Git Web Client - 快速上手指南

本文档帮助您快速启动和运行 Git Web Client 桌面应用。

---

## 📋 前置条件

在开始之前，请确保您的计算机已安装：

- ✅ **Node.js** 18.0+ （[下载地址](https://nodejs.org/)）
- ✅ **.NET SDK** 10.0+ （[下载地址](https://dotnet.microsoft.com/download)）
- ✅ **Git** 2.40+ （[下载地址](https://git-scm.com/)）

**验证安装**：

```bash
node -v      # 应显示 v18.x.x 或更高
dotnet --version  # 应显示 10.x.x 或更高
git --version     # 应显示 2.40.x 或更高
```

---

## 🚀 快速启动（开发模式）

### 方法一：一键启动脚本（Windows）

创建 `start-dev.bat` 文件并运行：

```batch
@echo off
echo Starting Git Web Client...

echo [1/3] Starting Backend API...
start cmd /k "cd server && dotnet run --urls http://localhost:9002"
timeout /t 3

echo [2/3] Starting Frontend Dev Server...
start cmd /k "cd git-web && npm run electron:dev"
timeout /t 5

echo [3/3] Starting Electron App...
cd git-web
npm run electron:start
```

双击 `start-dev.bat` 即可启动所有服务。

---

### 方法二：手动分步启动

#### 步骤 1：安装依赖（仅首次运行）

```bash
# 安装前端依赖
cd git-web
npm install

# 安装后端依赖
cd ../server
dotnet restore
```

#### 步骤 2：启动后端 API

打开**终端 1**：

```bash
cd server
dotnet run --urls "http://localhost:9002"
```

✅ **成功标志**：看到 "Now listening on: http://localhost:9002"

#### 步骤 3：启动前端

打开**终端 2**：

```bash
cd git-web
npm run electron:dev
```

✅ **成功标志**：看到 "Local: http://localhost:9001/"

#### 步骤 4：启动 Electron

打开**终端 3**：

```bash
cd git-web
npm run electron:start
```

✅ **成功标志**：Electron 窗口打开，显示应用界面

---

## 📦 打包发布（生产环境）

### 完整打包步骤

#### 1. 发布后端（自包含模式）

```bash
cd server
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

**输出位置**：`server/bin/Release/net10.0/win-x64/publish/`

#### 2. 打包 Electron 应用

```bash
cd git-web
npm run electron:build
```

**输出位置**：`git-web/release/Git-Web-Client-Setup-1.0.0.exe`

#### 3. 分发安装包

将 `Git-Web-Client-Setup-1.0.0.exe` 分发给用户，双击安装即可。

---

## 🎯 首次使用指南

### 1. 添加 Git 仓库

启动应用后：

1. 点击左侧导航栏的 **"添加新仓库"** 按钮
2. **仓库名称**：输入一个便于识别的名字（如："我的项目"）
3. **仓库路径**：输入本地 Git 仓库的完整路径
   - 示例：`D:\Projects\my-git-repo`
   - 支持粘贴路径
4. 点击 **"保存"**

### 2. 查看提交历史

1. 点击左侧列表中已保存的仓库名称
2. 右侧自动加载提交历史
3. 提交记录按时间倒序显示

### 3. 搜索提交

- **按消息搜索**：顶部搜索框输入关键词
- **按作者筛选**：点击作者下拉框选择

### 4. 查看提交详情

- **方式 1**：右键点击提交 → 选择"查看提交详情"
- **方式 2**：双击提交记录

在详情页可以看到：
- 树形文件结构
- 文件变更状态（Added、Modified、Deleted）
- 支持展开/折叠目录

### 5. 下载文件

#### 单个提交下载
- 点击提交记录右侧的 **"下载"** 按钮

#### 批量下载
1. 勾选多个提交（按住 **Shift** 可范围选择）
2. 点击顶部的 **"批量下载所选"** 按钮

---

## 🛠️ 常见操作

### 切换主题

点击顶部工具栏的 **月亮/太阳图标** 切换深色/浅色主题。

### 刷新仓库

点击仓库名称旁的 **刷新按钮** 重新加载提交历史。

### 删除仓库

右键点击仓库名称 → 选择 **"删除"** （不会删除本地文件，仅移除应用配置）。

---

## ❓ 常见问题

### Q1: 端口被占用怎么办？

**错误提示**：`Port 9001 is already in use`

**解决方法**：

```bash
# 查找占用端口的进程
netstat -ano | findstr "9001"

# 结束进程（替换 <PID> 为实际进程 ID）
taskkill /PID <PID> /F
```

### Q2: Electron 窗口显示空白？

**原因**：后端 API 未启动

**解决方法**：
1. 检查终端 1 是否显示 "Now listening on: http://localhost:9002"
2. 如果没有，手动启动后端
3. 按 **Ctrl + R** 刷新 Electron 窗口

### Q3: 找不到 Git 仓库？

**错误提示**：`Invalid Git repository`

**检查项**：
- 确认路径正确，不能有多余的引号或空格
- 确认该目录下存在 `.git` 文件夹
- 尝试在该目录执行 `git status` 验证仓库有效性

### Q4: 下载文件失败？

**可能原因**：
- 磁盘空间不足
- 下载目录没有写入权限
- 防病毒软件拦截

**解决方法**：
- 检查磁盘空间
- 临时关闭防病毒软件
- 更换下载目录

---

## 🔧 开发调试

### 打开开发者工具

在 Electron 应用中按 **F12** 打开 Chrome DevTools。

### 查看日志

- **前端日志**：DevTools → Console 面板
- **后端日志**：启动 `dotnet run` 的终端窗口
- **主进程日志**：启动 `electron:start` 的终端窗口

### 热更新

修改前端代码后，Vite 会自动热更新，无需刷新页面。

修改 Electron 主进程代码后，需要：
1. 关闭 Electron 窗口
2. 重新运行 `npm run electron:start`

---

## 📚 更多文档

- **[完整 README](./README.MD)** - 详细的项目介绍和功能说明
- **[Electron 集成文档](./ELECTRON_INTEGRATION.md)** - 技术架构和实现细节

---

## 💡 小贴士

1. **性能优化**：首次打开大型仓库可能较慢，属正常现象
2. **快捷键**：
   - `Ctrl + R` - 刷新 Electron 窗口
   - `F12` - 开启/关闭开发者工具
   - `Ctrl + F` - 搜索（在页面内）
3. **批量操作**：使用 **Shift + 点击** 可快速选择连续的提交记录
4. **路径输入**：支持直接从文件资源管理器复制路径并粘贴

---

## 🎉 开始使用

现在您已经掌握了基本操作，开始管理您的 Git 仓库吧！

如有问题，请查阅 [README.MD](./README.MD) 或提交 Issue。

---

**祝使用愉快！ 🚀**
