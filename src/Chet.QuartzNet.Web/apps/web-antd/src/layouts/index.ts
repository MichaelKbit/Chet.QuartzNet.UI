const BasicLayout = () => import('./basic.vue');
const AuthPageLayout = () => import('./auth.vue');

// 移除IFrameView，减少项目体积
// const IFrameView = () => import('@vben/layouts').then((m) => m.IFrameView);

export { AuthPageLayout, BasicLayout };
