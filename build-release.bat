@echo off
chcp 65001 >nul
title Git Web Client - 生产环境打包脚本
color 0B

echo.
echo ========================================
echo   Git Web Client 生产环境打包脚本
echo ========================================
echo.

:: 检查 .NET SDK
echo [检查] 正在检查 .NET SDK...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 .NET SDK，请先安装 .NET SDK 10.0+
    pause
    exit /b 1
)
echo [完成] .NET SDK:
dotnet --version
echo.

:: 检查 Node.js
echo [检查] 正在检查 Node.js...
where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 Node.js，请先安装 Node.js 18.0+
    pause
    exit /b 1
)
echo [完成] Node.js:
node -v
echo.

:: 清理旧的构建文件
echo [清理] 正在清理旧的构建文件...
if exist "server\bin\Release\" rmdir /s /q "server\bin\Release\"
if exist "git-web\dist\" rmdir /s /q "git-web\dist\"
if exist "git-web\dist-electron\" rmdir /s /q "git-web\dist-electron\"
if exist "git-web\release\" rmdir /s /q "git-web\release\"
echo [完成] 清理完成
echo.

:: 步骤 1: 发布后端 API (自包含模式)
echo ========================================
echo [1/2] 正在发布后端 API...
echo ========================================
echo.
echo 配置:
echo   - 模式: Release
echo   - 平台: win-x64
echo   - 类型: 自包含 (包含 .NET 运行时)
echo.

cd server
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
if %errorlevel% neq 0 (
    echo.
    echo [错误] 后端发布失败
    cd ..
    pause
    exit /b 1
)
cd ..

echo.
echo [完成] 后端发布完成
echo        输出目录: server\bin\Release\net10.0\win-x64\publish\
echo.
timeout /t 2 /nobreak >nul

:: 步骤 2: 构建前端并打包 Electron 应用
echo ========================================
echo [2/2] 正在构建前端并打包 Electron 应用...
echo ========================================
echo.
echo 打包过程包括:
echo   1. Vite 构建前端代码
echo   2. 编译 Electron 主进程
echo   3. electron-builder 打包为 Windows 安装程序
echo.
echo 这可能需要几分钟时间，请耐心等待...
echo.

cd git-web
call npm run electron:build
if %errorlevel% neq 0 (
    echo.
    echo [错误] Electron 应用打包失败
    cd ..
    pause
    exit /b 1
)
cd ..

echo.
echo ========================================
echo   打包完成！
echo ========================================
echo.
echo 安装程序位置:
echo   git-web\release\Git-Web-Client-Setup-1.0.0.exe
echo.
echo 文件大小:
dir "git-web\release\*.exe" | find ".exe"
echo.
echo 安装程序包含:
echo   - 前端 Vue 应用
echo   - Electron 运行时
echo   - .NET 10 运行时
echo   - 后端 API 服务
echo.
echo 用户只需双击安装即可使用，无需额外安装依赖！
echo.

:: 询问是否打开输出目录
set /p openDir="是否打开输出目录？(Y/N): "
if /i "%openDir%"=="Y" (
    start explorer "git-web\release\"
)

echo.
echo 按任意键退出...
pause >nul
