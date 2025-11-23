<template>
  <div class="log-management-page">
    <PageHeader :ghost="false" title="日志管理" />
    
    <Card :bordered="false" class="mt-4">
      <!-- 工具栏 -->
      <template #extra>
        <Button type="primary" danger @click="handleClearLogs">
          清空日志
        </Button>
      </template>
      
      <!-- 表格区域 -->
      <div ref="gridRef" class="mt-4"></div>
    </Card>
  </div>
</template>

<script lang="ts" setup>
import { ref, onMounted, nextTick } from 'vue';
import { Card, PageHeader, Button, message, Modal } from 'ant-design-vue';
import { useVbenVxeGrid } from '@vben/plugins/vxe-table';

// 表格引用
const gridRef = ref();
const vxeGridRef = ref();

// 日志数据
const logsData = ref([]);
const loading = ref(false);

// 表格列配置
const columns = [
  {
    field: 'jobName',
    title: '作业名称',
    minWidth: 180,
    ellipsis: true,
  },
  {
    field: 'jobGroup',
    title: '作业分组',
    minWidth: 150,
    ellipsis: true,
  },
  {
    field: 'fireTime',
    title: '执行时间',
    minWidth: 200,
    ellipsis: true,
  },
  {
    field: 'timeTaken',
    title: '执行耗时(ms)',
    width: 130,
    align: 'center',
  },
  {
    field: 'status',
    title: '状态',
    width: 100,
    align: 'center',
    formatter: (cellValue) => {
      const statusMap = {
        0: { text: '成功', color: '#52c41a' },
        1: { text: '失败', color: '#ff4d4f' },
      };
      const status = statusMap[cellValue] || { text: '未知', color: '#1890ff' };
      return `<span style="color: ${status.color}">${status.text}</span>`;
    },
  },
  {
    field: 'exception',
    title: '异常信息',
    minWidth: 250,
    ellipsis: true,
    // 使用格式化函数来渲染简单的HTML
      formatter: (cellValue, row) => {
        if (!cellValue) return '';
        // 直接返回HTML字符串，不使用JSX
        return `<a href="javascript:void(0)" class="vxe-link-btn vxe-link-btn--small" onclick="showExceptionDetail('${row.exception.replace(/'/g, "\\'")}')">查看详情</a>`;
      },
  },
  {
    field: 'createTime',
    title: '创建时间',
    minWidth: 200,
    ellipsis: true,
  },
];

// 表单配置
const formSchema = [
  {
    field: 'jobName',
    component: 'Input',
    label: '作业名称',
    colProps: {
      span: 8,
    },
  },
  {
    field: 'jobGroup',
    component: 'Input',
    label: '作业分组',
    colProps: {
      span: 8,
    },
  },
  {
    field: 'status',
    component: 'Select',
    label: '状态',
    colProps: {
      span: 8,
    },
    componentProps: {
      placeholder: '请选择状态',
      options: [
        { label: '全部', value: '' },
        { label: '成功', value: 0 },
        { label: '失败', value: 1 },
      ],
    },
  },
  {
    field: 'startTime',
    component: 'DatePicker',
    label: '开始时间',
    colProps: {
      span: 12,
    },
    componentProps: {
      placeholder: '请选择开始时间',
      showTime: true,
      format: 'YYYY-MM-DD HH:mm:ss',
      valueFormat: 'YYYY-MM-DD HH:mm:ss',
    },
  },
  {
    field: 'endTime',
    component: 'DatePicker',
    label: '结束时间',
    colProps: {
      span: 12,
    },
    componentProps: {
      placeholder: '请选择结束时间',
      showTime: true,
      format: 'YYYY-MM-DD HH:mm:ss',
      valueFormat: 'YYYY-MM-DD HH:mm:ss',
    },
  },
];

// 获取日志列表 - 使用mock数据
const fetchLogs = async (page = 1, pageSize = 10, filters) => {
  try {
    loading.value = true;
    
    // Mock数据
    const mockLogs = [
      {
        logId: 1,
        jobName: '测试作业1',
        jobGroup: 'DEFAULT',
        fireTime: '2024-01-15 10:00:00',
        timeTaken: 123,
        status: 0,
        createTime: '2024-01-15 10:00:00',
      },
      {
        logId: 2,
        jobName: '测试作业2',
        jobGroup: 'DEFAULT',
        fireTime: '2024-01-15 10:10:00',
        timeTaken: 256,
        status: 1,
        exception: '执行失败：数据库连接超时',
        createTime: '2024-01-15 10:10:00',
      },
      {
        logId: 3,
        jobName: '备份作业',
        jobGroup: 'BACKUP',
        fireTime: '2024-01-15 02:00:00',
        timeTaken: 15000,
        status: 0,
        createTime: '2024-01-15 02:00:00',
      },
      {
        logId: 4,
        jobName: '清理作业',
        jobGroup: 'MAINTENANCE',
        fireTime: '2024-01-15 03:00:00',
        timeTaken: 8000,
        status: 0,
        createTime: '2024-01-15 03:00:00',
      },
      {
        logId: 5,
        jobName: '测试作业1',
        jobGroup: 'DEFAULT',
        fireTime: '2024-01-15 10:05:00',
        timeTaken: 145,
        status: 0,
        createTime: '2024-01-15 10:05:00',
      },
      {
        logId: 6,
        jobName: '测试作业1',
        jobGroup: 'DEFAULT',
        fireTime: '2024-01-15 10:10:00',
        timeTaken: 118,
        status: 0,
        createTime: '2024-01-15 10:10:00',
      },
      {
        logId: 7,
        jobName: '测试作业1',
        jobGroup: 'DEFAULT',
        fireTime: '2024-01-15 10:15:00',
        timeTaken: 203,
        status: 1,
        exception: '执行失败：参数格式错误',
        createTime: '2024-01-15 10:15:00',
      },
    ];
    
    // 简单过滤
    let filteredLogs = mockLogs.filter(log => {
      if (filters?.jobName && !log.jobName.includes(filters.jobName)) return false;
      if (filters?.jobGroup && !log.jobGroup.includes(filters.jobGroup)) return false;
      if (filters?.status !== undefined && filters.status !== '' && log.status !== Number(filters.status)) return false;
      if (filters?.startTime && log.fireTime < filters.startTime) return false;
      if (filters?.endTime && log.fireTime > filters.endTime) return false;
      return true;
    });
    
    // 简单分页
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    
    logsData.value = filteredLogs.slice(start, end);
    
    return {
      list: filteredLogs.slice(start, end),
      total: filteredLogs.length,
    };
  } catch (error) {
    console.error('获取日志列表失败:', error);
    message.error('获取日志列表失败');
    return { list: [], total: 0 };
  } finally {
    loading.value = false;
  }
};

// 显示异常详情
const showExceptionDetail = (exception) => {
  Modal.info({
    title: '异常详情',
    content: exception,
    width: 600,
  });
};

// 为了支持formatter中的onclick调用，将函数绑定到window对象
if (typeof window !== 'undefined') {
  window.showExceptionDetail = showExceptionDetail;
}

// 清空日志 - 模拟实现
const handleClearLogs = async () => {
  Modal.confirm({
    title: '确认操作',
    content: '确定要清空所有日志吗？此操作不可恢复。',
    okType: 'danger',
    onOk: async () => {
      try {
        loading.value = true;
        
        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        
        message.success('清空日志成功');
        vxeGridRef.value?.reloadData();
      } catch (error) {
        console.error('清空日志失败:', error);
        message.error('清空日志失败');
      } finally {
        loading.value = false;
      }
    },
  });
};

// 初始化表格
const initGrid = async () => {
  if (!gridRef.value) return;
  
  await nextTick();
  
  const { gridInstance, proxy } = useVbenVxeGrid({
    target: gridRef.value,
    columns,
    formSchema,
    showToolbar: true,
    showFooter: true,
    gridProps: {
      height: 500,
      rowKey: 'logId',
      sortConfig: {
        orders: [{ field: 'fireTime', type: 'desc' }],
      },
    },
    proxyConfig: {
      autoLoad: true,
      queryMethod: async (params) => {
        const { page, pageSize, filters } = params;
        return fetchLogs(page, pageSize, filters);
      },
    },
  });
  
  vxeGridRef.value = gridInstance;
};

// 初始化
onMounted(async () => {
  await initGrid();
});
</script>

<style lang="less" scoped>
.log-management-page {
  padding: 20px;
  
  :deep(.ant-card-body) {
    padding: 0;
  }
  
  .mt-4 {
    margin-top: 16px;
  }
}
</style>