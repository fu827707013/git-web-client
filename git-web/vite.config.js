import { defineConfig } from 'vite'
import vuetify from 'vite-plugin-vuetify'
import vue from '@vitejs/plugin-vue'
import electron from 'vite-plugin-electron'
import renderer from 'vite-plugin-electron-renderer'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

export default defineConfig({
    plugins: [
        vue(),
        vuetify({ autoImport: true }),
        AutoImport({
            resolvers: [ElementPlusResolver()],
        }),
        Components({
            resolvers: [ElementPlusResolver()],
        }),
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
                                format: 'cjs'  // preload 必须使用 CommonJS 格式
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