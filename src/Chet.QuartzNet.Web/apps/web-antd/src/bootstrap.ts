import { createApp, watchEffect } from 'vue';

import { registerAccessDirective } from '@vben/access';
import { registerLoadingDirective } from '@vben/common-ui/es/loading';
import { useVbenForm } from './adapter/form';
import { setupVbenVxeTable } from '@vben/plugins/vxe-table';
import { initStores } from '@vben/stores';
import '@vben/styles';
import '@vben/styles/antd';

import { setupI18n } from '#/locales';

import { initComponentAdapter } from './adapter/component';
import { initSetupVbenForm } from './adapter/form';
import { useSystemConfig } from './composables/use-system-config';
import App from './app.vue';
import { router } from './router';

async function bootstrap(namespace: string) {
  // 初始化组件适配器
  await initComponentAdapter();

  // 初始化表单组件
  await initSetupVbenForm();

  // 初始化 Vxe Table 适配器
  setupVbenVxeTable({
    configVxeTable: (_ui) => {
      // 这里可以按需注册自定义渲染器、设置全局配置等
    },
    useVbenForm,
  });


  // // 设置弹窗的默认配置
  // setDefaultModalProps({
  //   fullscreenButton: false,
  // });
  // // 设置抽屉的默认配置
  // setDefaultDrawerProps({
  //   zIndex: 1020,
  // });

  const app = createApp(App);

  // 注册v-loading指令
  registerLoadingDirective(app, {
    loading: 'loading', // 在这里可以自定义指令名称，也可以明确提供false表示不注册这个指令
    spinning: 'spinning',
  });

  // 国际化 i18n 配置
  await setupI18n(app);

  // 配置 pinia-tore
  await initStores(app, { namespace });

  // 安装权限指令
  registerAccessDirective(app);

  // 初始化 tippy
  const { initTippy } = await import('@vben/common-ui/es/tippy');
  initTippy(app);

  // 配置路由及路由守卫
  app.use(router);

  // 配置Motion插件
  // 移除MotionPlugin，减少项目体积
  // const { MotionPlugin } = await import('@vben/plugins/motion');
  // app.use(MotionPlugin);

  // 动态更新标题：统一基于系统配置的服务名称，不显示菜单名
  const { systemConfig } = useSystemConfig();
  watchEffect(() => {
    const name = systemConfig.value.serviceName;
    document.title = name ? `${name} - Chet.QuartzNet.UI` : 'Chet.QuartzNet.UI';
  });

  app.mount('#app');
}

export { bootstrap };
