@echo off
chcp 65001 >nul
title Git Web Client - 开发环境启动器
color 0A

echo.
echo ========================================
echo   Git Web Client 开发环境启动器
echo ========================================
echo.

:: 检查 Node.js
echo [检查] 正在检查 Node.js...
where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 Node.js，请先安装 Node.js 18.0+
    echo        下载地址: https://nodejs.org/
    pause
    exit /b 1
)
node -v
echo.

:: 检查 .NET SDK
echo [检查] 正在检查 .NET SDK...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 .NET SDK，请先安装 .NET SDK 10.0+
    echo        下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
dotnet --version
echo.

:: 检查是否首次运行（检查 node_modules）
if not exist "git-web\node_modules\" (
    echo [首次运行] 检测到首次运行，正在安装前端依赖...
    cd git-web
    call npm install
    if %errorlevel% neq 0 (
        echo [错误] 前端依赖安装失败
        pause
        exit /b 1
    )
    cd ..
    echo [完成] 前端依赖安装完成
    echo.
)

:: 启动后端 API
echo [1/3] 正在启动后端 API（端口 9002）...
start "Git Web Client - Backend API" cmd /k "cd server && dotnet run --urls http://localhost:9002"
timeout /t 3 /nobreak >nul
echo       后端 API 已在新窗口中启动
echo.

:: 启动前端开发服务器
echo [2/3] 正在启动前端开发服务器（端口 9001）...
start "Git Web Client - Frontend" cmd /k "cd git-web && npm run electron:dev"
timeout /t 8 /nobreak >nul
echo       前端开发服务器已在新窗口中启动
echo.

:: 启动 Electron 应用
echo [3/3] 正在启动 Electron 桌面应用...
cd git-web
start "Git Web Client - Electron" cmd /k "npm run electron:start"
cd ..
echo.

echo ========================================
echo   启动完成！
echo ========================================
echo.
echo   应用窗口应该已经打开
echo   如果没有，请检查以下终端窗口的错误信息：
echo   - Git Web Client - Backend API
echo   - Git Web Client - Frontend
echo   - Git Web Client - Electron
echo.
echo   开发者工具快捷键: F12
echo   刷新应用快捷键: Ctrl + R
echo.
echo   关闭所有服务：关闭打开的终端窗口即可
echo.
pause
