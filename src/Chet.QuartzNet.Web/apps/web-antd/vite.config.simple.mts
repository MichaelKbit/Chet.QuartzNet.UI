import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  base: '/vbenadmin/',
  build: {
    // 禁用代码分割，生成单个JS文件
    rollupOptions: {
      // 排除Node.js内置模块
      external: [
        'node:module',
        'node:os',
        'node:fs',
        'node:url',
        'node:path',
        'node:process',
        'node:events',
        'node:crypto',
        'node:util',
        'node:buffer',
        // 匹配所有node:开头的模块
        /^node:/,
      ],
      output: {
        manualChunks: undefined, // 禁用代码分割
        // 简单的文件命名格式，类似quartz-ui
        assetFileNames: 'lib/[name].[ext]',
        chunkFileNames: 'lib/[name].js',
        entryFileNames: 'app.js', // 主入口文件命名为app.js
      },
    },
    // 输出到指定目录
    outDir: '../../../Chet.QuartzNet.UI/wwwroot/vbenadmin',
    // 禁用源映射
    sourcemap: false,
    // 压缩代码
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
        // 忽略未使用的导入警告
        pure_funcs: ['console.log', 'debugger'],
      },
    },
    // 允许使用未解析的模块（处理node:模块）
    commonjsOptions: {
      ignoreDynamicRequires: true,
    },
  },
});
