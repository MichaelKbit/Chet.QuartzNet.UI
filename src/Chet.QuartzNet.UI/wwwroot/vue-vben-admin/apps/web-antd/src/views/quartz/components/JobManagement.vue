<template>
  <div class="job-management">
    <!-- 搜索栏 -->
    <div class="search-bar">
      <Form layout="inline" :model="searchForm" @submit.prevent>
        <FormItem label="作业名称">
          <Input v-model:value="searchForm.jobName" placeholder="请输入作业名称" />
        </FormItem>
        <FormItem label="作业分组">
          <Input v-model:value="searchForm.jobGroup" placeholder="请输入作业分组" />
        </FormItem>
        <FormItem label="状态">
          <Select v-model:value="searchForm.status" placeholder="请选择状态">
            <SelectOption value="">全部</SelectOption>
            <SelectOption :value="0">正常</SelectOption>
            <SelectOption :value="1">暂停</SelectOption>
          </Select>
        </FormItem>
        <FormItem>
          <Button type="primary" @click="handleSearch">搜索</Button>
        </FormItem>
        <FormItem>
          <Button @click="resetSearch">重置</Button>
        </FormItem>
      </Form>
      <Button type="primary" @click="showAddModal" style="margin-top: 16px;">
        <template #icon>+</template>
        添加作业
      </Button>
    </div>

    <!-- 作业表格 -->
    <div class="table-container">
      <Table
        :columns="jobColumns"
        :data-source="jobsData"
        :row-key="(record) => `${record.jobName}-${record.jobGroup}`"
        :pagination="jobPagination"
        @change="handleTableChange"
      >
        <template #status="{ text }">
          <Tag :color="getStatusColor(text)">{{ getStatusText(text) }}</Tag>
        </template>
        <template #action="{ record }">
          <Space size="middle">
              <Button type="link" @click="showEditModal(record)">编辑</Button>
              <Button type="link" danger @click="handleDelete(record)">删除</Button>
              <Button type="link" @click="record.status === 0 ? handlePause(record) : handleResume(record)">
                {{ record.status === 0 ? '暂停' : '恢复' }}
              </Button>
              <Button type="link" @click="handleTrigger(record)">触发</Button>
            </Space>
        </template>
      </Table>
    </div>

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

<script setup>
import { reactive, ref, onMounted } from 'vue';
import { Table } from 'ant-design-vue';
import { Form, FormItem } from 'ant-design-vue';
import { Input, InputGroup } from 'ant-design-vue';
import { Button } from 'ant-design-vue';
import { Select, SelectOption } from 'ant-design-vue';
import { Modal } from 'ant-design-vue';
import { Tag } from 'ant-design-vue';
import { Space } from 'ant-design-vue';
import { message } from 'ant-design-vue';
import CronHelperModal from './CronHelperModal.vue';

    
    // 搜索表单
    const searchForm = reactive({
      jobName: '',
      jobGroup: '',
      status: '',
    });
    
    // 分页配置
    const jobPagination = reactive({
      current: 1,
      pageSize: 10,
      total: 0,
      showSizeChanger: true,
      showQuickJumper: true,
      showTotal: (total: number) => `共 ${total} 条记录`,
    });
    
    // 作业数据
    const jobsData = ref<JobInfo[]>([]);
    const jobClasses = ref<string[]>([]);
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
      jobClassName: '',
      jobData: '',
      description: '',
    });
    
    // 表格列配置
    const jobColumns = [
      {
        title: '作业名称',
        dataIndex: 'jobName',
        key: 'jobName',
        ellipsis: true,
      },
      {
        title: '作业分组',
        dataIndex: 'jobGroup',
        key: 'jobGroup',
        ellipsis: true,
      },
      {
        title: 'Cron表达式',
        dataIndex: 'cronExpression',
        key: 'cronExpression',
        ellipsis: true,
      },
      {
        title: '作业类名',
        dataIndex: 'jobClassName',
        key: 'jobClassName',
        ellipsis: true,
      },
      {
          title: '状态',
          dataIndex: 'status',
          key: 'status',
          slots: {
            customRender: 'status'
          }
        },
        {
          title: '描述',
          dataIndex: 'description',
          key: 'description',
          ellipsis: true,
        },
        {
          title: '操作',
          key: 'action',
          fixed: 'right',
          width: 220,
          slots: {
            customRender: 'action'
          }
        },
    ];
    
    // 获取作业列表 - 使用mock数据
    const fetchJobs = async (page = 1, pageSize = 10, _sorter?: any) => {
      try {
        loading.value = true;
        // Mock数据
        const mockJobs: JobInfo[] = [
          { jobName: '测试作业1', jobGroup: 'DEFAULT', cronExpression: '0 0/5 * * * ?', jobClassName: 'TestJob', status: 0, description: '每5分钟执行一次' },
          { jobName: '测试作业2', jobGroup: 'DEFAULT', cronExpression: '0 0/10 * * * ?', jobClassName: 'TestJob2', status: 1, description: '每10分钟执行一次' },
        ];
        
        // 简单过滤
        let filteredJobs = mockJobs.filter(job => {
          if (searchForm.jobName && !job.jobName.includes(searchForm.jobName)) return false;
          if (searchForm.jobGroup && !job.jobGroup.includes(searchForm.jobGroup)) return false;
          if (searchForm.status !== '' && job.status !== Number(searchForm.status)) return false;
          return true;
        });
        
        // 简单分页
        const start = (page - 1) * pageSize;
        const end = start + pageSize;
        
        jobsData.value = filteredJobs.slice(start, end);
        jobPagination.total = filteredJobs.length;
      } catch (error: any) {
        console.error('获取作业列表失败:', error);
        message.error('获取作业列表失败');
        jobsData.value = [];
        jobPagination.total = 0;
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
          'DatabaseBackupJob'
        ];
      } catch (error: any) {
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
      });
      jobModalVisible.value = true;
    };
    
    // 显示编辑模态框
    const showEditModal = (job: JobInfo) => {
      isEditMode.value = true;
      Object.assign(currentJob, job);
      jobModalVisible.value = true;
    };
    
    // 显示Cron帮助
    const showCronHelper = () => {
      cronHelperVisible.value = true;
    };
    
    // 选择Cron表达式
    const selectCronExpression = (expression: string) => {
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
        fetchJobs(jobPagination.current, jobPagination.pageSize);
      } catch (error: any) {
        console.error('保存作业失败:', error);
        message.error(isEditMode.value ? '更新作业失败' : '添加作业失败');
      } finally {
        loading.value = false;
      }
    };
    
    // 删除作业 - 模拟实现
    const handleDelete = async (record: JobInfo) => {
      try {
        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        message.success('删除作业成功');
        // 乐观更新，立即从列表中移除
        const index = jobsData.value.findIndex(job => 
          job.jobName === record.jobName && job.jobGroup === record.jobGroup
        );
        if (index > -1) {
          jobsData.value.splice(index, 1);
          jobPagination.total -= 1;
        }
      } catch (error: any) {
        console.error('删除作业失败:', error);
        message.error('删除作业失败');
      }
    };
    
    // 暂停作业 - 模拟实现
    const handlePause = async (record: JobInfo) => {
      try {
        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        message.success('暂停作业成功');
        // 乐观更新
        const job = jobsData.value.find(j => 
          j.jobName === record.jobName && j.jobGroup === record.jobGroup
        );
        if (job) {
          job.status = 1; // 暂停状态
        }
      } catch (error: any) {
        console.error('暂停作业失败:', error);
        message.error('暂停作业失败');
      }
    };
    
    // 恢复作业 - 模拟实现
    const handleResume = async (record: JobInfo) => {
      try {
        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        message.success('恢复作业成功');
        // 乐观更新
        const job = jobsData.value.find(j => 
          j.jobName === record.jobName && j.jobGroup === record.jobGroup
        );
        if (job) {
          job.status = 0; // 运行状态
        }
      } catch (error: any) {
        console.error('恢复作业失败:', error);
        message.error('恢复作业失败');
      }
    };
    
    // 触发作业 - 模拟实现
    const handleTrigger = async (_record: JobInfo) => {
      try {
        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        message.success('触发作业成功');
      } catch (error: any) {
        console.error('触发作业失败:', error);
        message.error('触发作业失败');
      }
    };
    
    // 搜索
    const handleSearch = () => {
      jobPagination.current = 1;
      fetchJobs(1, jobPagination.pageSize);
    };
    
    // 重置搜索
    const resetSearch = () => {
      Object.assign(searchForm, {
        jobName: '',
        jobGroup: '',
        status: '',
      });
      jobPagination.current = 1;
      fetchJobs(1, jobPagination.pageSize);
    };
    
    // 表格变化处理
    const handleTableChange = (pagination: any, _filters: any, sorter: any) => {
      jobPagination.current = pagination.current;
      jobPagination.pageSize = pagination.pageSize;
      fetchJobs(pagination.current, pagination.pageSize, sorter);
    };
    
    // 初始化
    onMounted(() => {
      fetchJobs();
      fetchJobClasses();
    });
    
    // 获取状态文本
    const getStatusText = (status: number): string => {
      const statusMap: Record<number, string> = {
        0: '正常',
        1: '暂停',
      };
      return statusMap[status] || '未知';
    };

    // 获取状态颜色
    const getStatusColor = (status: number): string => {
      const colorMap: Record<number, string> = {
        0: 'success',
        1: 'warning',
      };
      return colorMap[status] || 'default';
    };

    return {
      searchForm,
      jobPagination,
      jobsData,
      jobClasses,
      loading,
      jobModalVisible,
      cronHelperVisible,
      isEditMode,
      currentJob,
      jobColumns,
      showAddModal,
      showEditModal,
      showCronHelper,
      selectCronExpression,
      handleCancel,
      handleSave,
      handleDelete,
      handlePause,
      handleResume,
      handleTrigger,
      handleSearch,
      resetSearch,
      handleTableChange,
      getStatusText,
      getStatusColor
    };
  },
</script>

<style lang="less" scoped>
.job-management {
  padding: 16px;
  
  .search-bar {
    margin-bottom: 16px;
    padding: 16px;
    background-color: #ffffff;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  }
  
  .table-container {
    background-color: #ffffff;
    border-radius: 8px;
    padding: 16px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  }
}



@media (max-width: 768px) {
  .job-management {
    padding: 8px;
    
    .search-bar {
      padding: 12px;
    }
    
    .table-container {
      padding: 8px;
    }
  }
}
</style>