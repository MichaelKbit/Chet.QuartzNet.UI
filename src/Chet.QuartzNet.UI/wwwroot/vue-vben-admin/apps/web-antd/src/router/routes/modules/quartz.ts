import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'ion:time-outline',
      order: 100,
      title: $t('Quartz任务调度'),
    },
    name: 'Quartz',
    path: '/quartz',
    redirect: '/quartz/job-management',
    children: [
      {
        name: 'JobManagement',
        path: '/quartz/job-management',
        component: () => import('#/views/quartz/job-management.vue'),
        meta: {
          icon: 'lucide:task',
          title: $t('作业管理'),
        },
      },
      {
        name: 'LogManagement',
        path: '/quartz/log-management',
        component: () => import('#/views/quartz/log-management.vue'),
        meta: {
          icon: 'lucide:list-checks',
          title: $t('日志管理'),
        },
      },
    ],
  },
];

export default routes;