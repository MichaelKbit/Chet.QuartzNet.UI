<template>
  <div class="job-management-page">
    <PageHeader :ghost="false" title="作业管理" />
    
    <Card :bordered="false" class="mt-4">
      <!-- 工具栏 -->
      <template #extra>
        <Button type="primary" @click="showAddModal">
          <template #icon><PlusOutlined /></template>
          添加作业
        </Button>
      </template>
      
      <!-- 表格区域 -->
      <div ref="gridRef" class="mt-4"></div>
    </Card>

    <!-- 添加/编辑作业模态框 -->
    <Modal
      v-model:visible="jobModalVisible"
      :title="isEditMode ? '编辑作业' : '添加作业'"
      @ok="handleSave"
      @cancel="handleCancel"
    >
      <Form layout="vertical" :model="currentJob">
        <FormItem label="作业名称" required>
          <Input v-model:value="currentJob.jobName" placeholder="请输入作业名称" />
        </FormItem>
        <FormItem label="作业分组" required>
          <Input v-model:value="currentJob.jobGroup" placeholder="请输入作业分组" />
        </FormItem>
        <FormItem label="Cron表达式" required>
          <InputGroup compact>
            <Input v-model:value="currentJob.cronExpression" placeholder="请输入Cron表达式" />
            <Button @click="showCronHelper">帮助</Button>
          </InputGroup>
        </FormItem>
        <FormItem label="作业类名" required>
          <Select v-model:value="currentJob.jobClassName" placeholder="请选择作业类名">
            <SelectOption v-for="cls in jobClasses" :key="cls" :value="cls">
              {{ cls }}
            </SelectOption>
          </Select>
        </FormItem>
        <FormItem label="作业参数">
          <Input
            v-model:value="currentJob.jobData"
            placeholder="请输入JSON格式的作业参数"
            type="textarea"
            :rows="3"
          />
        </FormItem>
        <FormItem label="描述">
          <Input
            v-model:value="currentJob.description"
            placeholder="请输入作业描述"
            type="textarea"
            :rows="2"
          />
        </FormItem>
      </Form>
    </Modal>

    <!-- Cron表达式帮助模态框 -->
    <CronHelperModal
      v-model:visible="cronHelperVisible"
      @select="selectCronExpression"
    />
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, onMounted, nextTick } from 'vue';
import { Card, PageHeader, Button, Modal, Form, FormItem, Input, InputGroup, Select, SelectOption, message } from 'ant-design-vue';
import { PlusOutlined } from '@ant-design/icons-vue';
import { useVbenVxeGrid } from '@vben/plugins/vxe-table';
import CronHelperModal from './CronHelperModal.vue';

// 表格引用
const gridRef = ref();
const vxeGridRef = ref();

// 作业数据
const jobsData = ref([]);
const jobClasses = ref([]);
const loading = ref(false);

// 模态框状态
const jobModalVisible = ref(false);
const cronHelperVisible = ref(false);
const isEditMode = ref(false);

// 当前作业
const currentJob = reactive({
  jobName: '',
  jobGroup: '',
  cronExpression: '',
  status: 0,
  description: '',
  jobClassName: '',
});

// 表格列配置 - 使用更简单的方式实现
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
    field: 'cronExpression',
    title: 'Cron表达式',
    minWidth: 200,
    ellipsis: true,
  },
  {
    field: 'jobClassName',
    title: '作业类名',
    minWidth: 250,
    ellipsis: true,
  },
  {
    field: 'status',
    title: '状态',
    width: 100,
    align: 'center',
    // 使用格式化函数
    formatter: (cellValue) => {
      const statusMap = {
        0: { text: '正常', color: '#52c41a' },
        1: { text: '暂停', color: '#faad14' },
      };
      const status = statusMap[cellValue] || { text: '未知', color: '#1890ff' };
      return `<span style="color: ${status.color}">${status.text}</span>`;
    },
  },
  {
    field: 'description',
    title: '描述',
    minWidth: 200,
    ellipsis: true,
  },
  {
    field: 'action',
    title: '操作',
    width: 220,
    fixed: 'right',
    align: 'center',
    // 使用格式化函数渲染HTML
    formatter: (cellValue, row) => {
      // 使用简单的HTML和行ID，避免复杂的JSON字符串转义
      return `
        <a href="javascript:void(0)" class="vxe-link-btn vxe-link-btn--small" onclick="window.quartzActions.showEditModal('${row.id}')">编辑</a>
        <a href="javascript:void(0)" class="vxe-link-btn vxe-link-btn--small text-danger" onclick="window.quartzActions.handleDelete('${row.id}')">删除</a>
        <a href="javascript:void(0)" class="vxe-link-btn vxe-link-btn--small" onclick="window.quartzActions.handlePauseResume('${row.id}')">${row.status === 0 ? '暂停' : '恢复'}</a>
        <a href="javascript:void(0)" class="vxe-link-btn vxe-link-btn--small" onclick="window.quartzActions.handleTrigger('${row.id}')">触发</a>
      `;
    },
  },
];

// 表单配置
const formSchema = [
  {
    field: 'jobName',
    component: 'Input',
    label: '作业名称',
    colProps: {
      span: 24,
    },
  },
  {
    field: 'jobGroup',
    component: 'Input',
    label: '作业分组',
    colProps: {
      span: 24,
    },
  },
  {
    field: 'status',
    component: 'Select',
    label: '状态',
    colProps: {
      span: 24,
    },
    componentProps: {
      placeholder: '请选择状态',
      options: [
        { label: '全部', value: '' },
        { label: '正常', value: 0 },
        { label: '暂停', value: 1 },
      ],
    },
  },
];

// 获取作业列表 - 使用mock数据
const fetchJobs = async (page = 1, pageSize = 10, filters) => {
  try {
    loading.value = true;
    // Mock数据
    const mockJobs = [
      { jobName: '测试作业1', jobGroup: 'DEFAULT', cronExpression: '0 0/5 * * * ?', jobClassName: 'TestJob', status: 0, description: '每5分钟执行一次' },
      { jobName: '测试作业2', jobGroup: 'DEFAULT', cronExpression: '0 0/10 * * * ?', jobClassName: 'TestJob2', status: 1, description: '每10分钟执行一次' },
      { jobName: '备份作业', jobGroup: 'BACKUP', cronExpression: '0 0 2 * * ?', jobClassName: 'DatabaseBackupJob', status: 0, description: '每天凌晨2点执行数据库备份' },
      { jobName: '清理作业', jobGroup: 'MAINTENANCE', cronExpression: '0 0 3 * * ?', jobClassName: 'LogCleanupJob', status: 0, description: '每天凌晨3点清理日志' },
    ];
    
    // 简单过滤
    let filteredJobs = mockJobs.filter(job => {
      if (filters?.jobName && !job.jobName.includes(filters.jobName)) return false;
      if (filters?.jobGroup && !job.jobGroup.includes(filters.jobGroup)) return false;
      if (filters?.status !== undefined && filters.status !== '' && job.status !== Number(filters.status)) return false;
      return true;
    });
    
    // 简单分页
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    
    jobsData.value = filteredJobs.slice(start, end);
    
    return {
      list: filteredJobs.slice(start, end),
      total: filteredJobs.length,
    };
  } catch (error) {
    console.error('获取作业列表失败:', error);
    message.error('获取作业列表失败');
    return { list: [], total: 0 };
  } finally {
    loading.value = false;
  }
};

// 获取作业类名列表 - 使用mock数据
const fetchJobClasses = async () => {
  try {
    // Mock数据
    jobClasses.value = [
      'TestJob',
      'TestJob2',
      'EmailNotificationJob',
      'DatabaseBackupJob',
      'LogCleanupJob',
      'ReportGenerationJob',
    ];
  } catch (error) {
    console.error('获取作业类名失败:', error);
    message.error('获取作业类名失败');
    jobClasses.value = [];
  }
};

// 显示添加模态框
const showAddModal = () => {
  isEditMode.value = false;
  Object.assign(currentJob, {
    jobName: '',
    jobGroup: '',
    cronExpression: '',
    jobClassName: '',
    jobData: '',
    description: '',
    status: 0,
  });
  jobModalVisible.value = true;
};

// 显示编辑模态框
const showEditModal = (job) => {
  isEditMode.value = true;
  Object.assign(currentJob, job);
  jobModalVisible.value = true;
};

// 显示Cron帮助
const showCronHelper = () => {
  cronHelperVisible.value = true;
};

// 选择Cron表达式
const selectCronExpression = (expression) => {
  currentJob.cronExpression = expression;
  cronHelperVisible.value = false;
};

// 取消模态框
const handleCancel = () => {
  jobModalVisible.value = false;
};

// 保存作业（新增或更新）- 模拟实现
const handleSave = async () => {
  try {
    loading.value = true;
    
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 500));
    
    if (isEditMode.value) {
      message.success('更新作业成功');
    } else {
      message.success('添加作业成功');
    }
    
    jobModalVisible.value = false;
    vxeGridRef.value?.reloadData();
  } catch (error) {
    console.error('保存作业失败:', error);
    message.error(isEditMode.value ? '更新作业失败' : '添加作业失败');
  } finally {
    loading.value = false;
  }
};

// 删除作业 - 模拟实现
const handleDelete = async (record) => {
  try {
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 500));
    message.success('删除作业成功');
    vxeGridRef.value?.reloadData();
  } catch (error) {
    console.error('删除作业失败:', error);
    message.error('删除作业失败');
  }
};

// 暂停/恢复作业 - 统一处理
const handlePauseResume = async (record) => {
  try {
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 500));
    
    if (record.status === 0) {
      message.success('暂停作业成功');
    } else {
      message.success('恢复作业成功');
    }
    
    vxeGridRef.value?.reloadData();
  } catch (error) {
    console.error('操作作业失败:', error);
    message.error(record.status === 0 ? '暂停作业失败' : '恢复作业失败');
  }
};

// 触发作业 - 模拟实现
const handleTrigger = async (record) => {
  try {
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 500));
    message.success('触发作业成功');
  } catch (error) {
    console.error('触发作业失败:', error);
    message.error('触发作业失败');
  }
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
    showFooter: false,
    gridProps: {
      height: 500,
      rowKey: 'jobName',
    },
    proxyConfig: {
      autoLoad: true,
      queryMethod: async (params) => {
        const { page, pageSize, filters } = params;
        return fetchJobs(page, pageSize, filters);
      },
    },
  });
  
  vxeGridRef.value = gridInstance;
};

// 初始化
  onMounted(async () => {
    await fetchJobClasses();
    await initGrid();
  });

// 将函数绑定到window对象，以支持formatter中的onclick调用
if (typeof window !== 'undefined') {
  // 创建命名空间避免冲突
  window.quartzActions = {
    showEditModal: (id) => {
      const row = jobs.value.find(job => job.id === id);
      if (row) showEditModal(row);
    },
    handleDelete: (id) => {
      const row = jobs.value.find(job => job.id === id);
      if (row) handleDelete(row);
    },
    handlePauseResume: (id) => {
      const row = jobs.value.find(job => job.id === id);
      if (row) handlePauseResume(row);
    },
    handleTrigger: (id) => {
      const row = jobs.value.find(job => job.id === id);
      if (row) handleTrigger(row);
    }
  };
}
</script>

<style lang="less" scoped>
.job-management-page {
  padding: 20px;
  
  :deep(.ant-card-body) {
    padding: 0;
  }
  
  .mt-4 {
    margin-top: 16px;
  }
}
</style>