import { defineConfig } from 'vite'
import vuetify from 'vite-plugin-vuetify'
import vue from '@vitejs/plugin-vue'
import electron from 'vite-plugin-electron'
import renderer from 'vite-plugin-electron-renderer'

export default defineConfig({
    plugins: [
        vue(),
        vuetify({ autoImport: true }),
        electron([
            {
                // 主进程入口
                entry: 'electron/main.js',
                onstart(options) {
                    options.startup()
                },
                vite: {
                    build: {
                        outDir: 'dist-electron',
                        rollupOptions: {
                            output: {
                                format: 'es'
                            }
                        }
                    }
                }
            },
            {
                // 预加载脚本入口
                entry: 'electron/preload.js',
                onstart(options) {
                    options.reload()
                },
                vite: {
                    build: {
                        outDir: 'dist-electron',
                        rollupOptions: {
                            output: {
                                format: 'es'
                            }
                        }
                    }
                }
            }
        ]),
        renderer()
    ],
    // 设置为相对路径，适配 Electron 文件协议
    base: './',
    server: {
        port: 9001,
        strictPort: true,
        proxy: {
            '/api': {
                target: 'http://localhost:9002',
                changeOrigin: true
            }
        }
    },
    // 构建配置
    build: {
        outDir: 'dist',
        emptyOutDir: true
    }
})