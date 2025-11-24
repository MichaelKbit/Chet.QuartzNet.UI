<script setup lang="ts">
import { ref, computed, reactive, h } from 'vue';
import { Page } from '@vben/common-ui';
import { Table, Card, Form } from 'ant-design-vue';
import type { ColumnsType, FormInstance } from 'ant-design-vue';
import {
  Button,
  Input,
  Select,
  Space,
  Modal,
  Tag,
  message,
  DatePicker,
  Typography,
  Alert,
} from 'ant-design-vue';
import {
  SearchOutlined,
  DownloadOutlined,
  DeleteOutlined,
} from '@ant-design/icons-vue';
import type { PaginationProps } from 'ant-design-vue';
import type { Dayjs } from 'dayjs';

// 导入日志相关类型和API
import {
  LogStatusEnum,
  getLogList,
  getLogDetail,
  exportLogList,
  clearLogs,
  getLogStatistics,
} from '../../api/quartz/log';
import type { LogQueryParams, LogResponseDto } from '../../api/quartz/log';
import type { ProColumns } from '@vben/common-ui';

// 日志状态映射
const logStatusMap = {
  [LogStatusEnum.SUCCESS]: { text: '成功', status: 'success' },
  [LogStatusEnum.FAILURE]: { text: '失败', status: 'error' },
  [LogStatusEnum.RUNNING]: { text: '运行中', status: 'processing' },
  [LogStatusEnum.CANCELLED]: { text: '已取消', status: 'warning' },
};

// 响应式数据
const loading = ref(false);
const dataSource = ref<LogResponseDto[]>([]);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 搜索条件
// 添加表单实例引用
const searchFormRef = ref<FormInstance>();
const searchForm = reactive<Omit<LogQueryParams, 'pageNum' | 'pageSize'>>({
  jobId: '',
  jobName: '',
  jobGroup: '',
  status: undefined,
  startTime: undefined,
  endTime: undefined,
});

// 详情对话框
const detailModalVisible = ref(false);
const detailModalTitle = ref('日志详情');
const logDetail = ref<any>(null);

// 统计信息
const statistics = ref<{
  totalLogs: number;
  successCount: number;
  failureCount: number;
  runningCount: number;
  cancelledCount: number;
}>({
  totalLogs: 0,
  successCount: 0,
  failureCount: 0,
  runningCount: 0,
  cancelledCount: 0,
});

// 列配置
const columns: ColumnsType<LogResponseDto>[] = [
  {
    title: '作业ID',
    dataIndex: 'jobId',
    width: 150,
    ellipsis: true,
  },
  {
    title: '作业名称',
    dataIndex: 'jobName',
    width: 180,
    ellipsis: true,
  },
  {
    title: '作业分组',
    dataIndex: 'jobGroup',
    width: 150,
    ellipsis: true,
  },
  {
    title: '状态',
    dataIndex: 'status',
    width: 100,
    customRender: ({ record }) => {
      const status = logStatusMap[record.status];
      return h(Tag, { color: status.status }, status.text);
    },
  },
  {
    title: '执行开始时间',
    dataIndex: 'startTime',
    width: 180,
    ellipsis: true,
  },
  {
    title: '执行结束时间',
    dataIndex: 'endTime',
    width: 180,
    ellipsis: true,
  },
  {
    title: '执行时长(ms)',
    dataIndex: 'duration',
    width: 120,
    ellipsis: true,
  },
  {
    title: '操作',
    valueType: 'option',
    width: 120,
    render: ({ record }) => {
      return h(Space, { size: 'middle' }, [
        h(
          Button,
          {
            type: 'link',
            onClick: () => handleDetail(record),
            disabled: loading.value,
          },
          '详情',
        ),
      ]);
    },
  },
];

// 分页配置
const pagination = computed<PaginationProps>(() => ({
  current: currentPage.value,
  pageSize: pageSize.value,
  total: total.value,
  showSizeChanger: true,
  showQuickJumper: true,
  showTotal: (total, range) => `${range[0]}-${range[1]} 共 ${total} 条`,
  pageSizeOptions: ['10', '20', '50', '100'],
}));

// 加载日志列表
const loadLogList = async () => {
  loading.value = true;
  try {
    const response = await getLogList({
      ...searchForm,
      pageNum: currentPage.value,
      pageSize: pageSize.value,
    });

    dataSource.value = response.data || [];
    total.value = response.total || 0;
  } catch (error) {
    message.error('获取日志列表失败');
    console.error('获取日志列表失败:', error);
  } finally {
    loading.value = false;
  }
};

// 加载统计信息
const loadLogStatistics = async () => {
  try {
    const stats = await getLogStatistics(searchForm);
    statistics.value = stats;
  } catch (error) {
    console.error('获取日志统计信息失败:', error);
  }
};

// 处理分页变化
const handlePageChange = (page: number, pageSizeVal: number) => {
  currentPage.value = page;
  pageSize.value = pageSizeVal;
  loadLogList();
};

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1;
  loadLogList();
  loadLogStatistics();
};

// 处理重置
const handleReset = () => {
  searchForm.jobId = '';
  searchForm.jobName = '';
  searchForm.jobGroup = '';
  searchForm.status = undefined;
  searchForm.startTime = undefined;
  searchForm.endTime = undefined;
  currentPage.value = 1;
  loadLogList();
  loadLogStatistics();
};

// 导出日志
const handleExport = async () => {
  loading.value = true;
  try {
    const blob = await exportLogList(searchForm);

    // 创建下载链接
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `quartz-logs-${new Date().toISOString().slice(0, 10)}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    message.success('日志导出成功');
  } catch (error) {
    message.error('日志导出失败');
    console.error('导出日志失败:', error);
  } finally {
    loading.value = false;
  }
};

// 清空日志
const handleClear = async () => {
  Modal.confirm({
    title: '确认清空',
    content: '确定要清空日志吗？此操作不可恢复！',
    onOk: async () => {
      loading.value = true;
      try {
        await clearLogs(searchForm);
        message.success('日志清空成功');
        loadLogList();
        loadLogStatistics();
      } catch (error) {
        message.error('日志清空失败');
        console.error('清空日志失败:', error);
      } finally {
        loading.value = false;
      }
    },
  });
};

// 查看详情
const handleDetail = async (log: LogResponseDto) => {
  try {
    const detail = await getLogDetail(log.logId);
    logDetail.value = detail;
    detailModalVisible.value = true;
  } catch (error) {
    message.error('获取日志详情失败');
    console.error('获取日志详情失败:', error);
  }
};

// 展开行配置
const expandableConfig = {
  expandedRowRender: (record: LogResponseDto) => {
    return h('div', { style: { padding: '16px', background: '#fafafa' } }, [
      h('div', { style: { marginBottom: '8px' } }, [
        h('strong', null, '执行信息:'),
      ]),
      h('div', { style: { marginBottom: '8px' } }, record.executionInfo),
      record.errorInfo &&
        h('div', { style: { marginBottom: '8px' } }, [
          h('strong', { style: { color: '#f5222d' } }, '错误信息:'),
          h(
            'pre',
            {
              style: {
                marginTop: '5px',
                color: '#f5222d',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              },
            },
            record.errorInfo,
          ),
        ]),
      h(
        Button,
        { type: 'link', onClick: () => handleDetail(record) },
        '查看完整详情',
      ),
    ]);
  },
};

// 初始化
const initData = async () => {
  await loadLogList();
  await loadLogStatistics();
};

// 启动时加载数据
initData();
</script>

<template>
  <Page>
    <Card style="margin-bottom: 20px;">
      <Alert 
        type="info" 
        style="margin-bottom: 16px;"
      >
        <template #message>
          日志统计信息: 共 {{ statistics.totalLogs }} 条，成功 {{ statistics.successCount }} 条，失败 {{ statistics.failureCount }} 条，运行中 {{ statistics.runningCount }} 条，已取消 {{ statistics.cancelledCount }} 条
        </template>
      </Alert>
      
      <Form
        ref="searchFormRef"
        :model="searchForm"
        layout="inline"
        :label-col="{ span: 8 }"
        :wrapper-col="{ span: 16 }"
        style="flex-wrap: wrap;"
      >
        <Form.Item label="作业ID" name="jobId" style="margin-bottom: 16px;">
          <Input placeholder="请输入作业ID" style="width: 180px;" />
        </Form.Item>
        <Form.Item label="作业名称" name="jobName" style="margin-bottom: 16px;">
          <Input placeholder="请输入作业名称" style="width: 180px;" />
        </Form.Item>
        <Form.Item label="作业分组" name="jobGroup" style="margin-bottom: 16px;">
          <Input placeholder="请输入作业分组" style="width: 180px;" />
        </Form.Item>
        <Form.Item label="执行状态" name="status" style="margin-bottom: 16px;">
          <Select placeholder="请选择状态" allowClear style="width: 120px;">
            <Select.Option :value="LogStatusEnum.SUCCESS">成功</Select.Option>
            <Select.Option :value="LogStatusEnum.FAILURE">失败</Select.Option>
            <Select.Option :value="LogStatusEnum.RUNNING">运行中</Select.Option>
            <Select.Option :value="LogStatusEnum.CANCELLED">已取消</Select.Option>
          </Select>
        </Form.Item>
        <Form.Item label="开始时间" name="startTime" style="margin-bottom: 16px;">
          <DatePicker
            showTime
            placeholder="选择开始时间"
            style="width: 200px;"
          />
        </Form.Item>
        <Form.Item label="结束时间" name="endTime" style="margin-bottom: 16px;">
          <DatePicker
            showTime
            placeholder="选择结束时间"
            style="width: 200px;"
          />
        </Form.Item>
        <Form.Item style="margin-bottom: 16px;">
          <Space>
            <Button type="primary" @click="handleSearch">
              <template #icon><SearchOutlined /></template>
              搜索
            </Button>
            <Button @click="handleReset">重置</Button>
          </Space>
        </Form.Item>
        <Form.Item style="margin-bottom: 16px;">
          <Space>
            <Button type="primary" icon="download" @click="handleExport">
              导出日志
            </Button>
            <Button danger icon="delete" @click="handleClear">清空日志</Button>
          </Space>
        </Form.Item>
      </Form>
    </Card>
    
    <Card>
      <!-- 日志列表 -->
      <Table
        :columns="columns"
        :data-source="dataSource"
        :pagination="pagination"
        :loading="loading"
        :rowKey="(record) => record.logId"
        @change="handlePageChange"
        :expandable="expandableConfig"
        style="width: 100%;"
      />
    </Card>

    <!-- 详情对话框 -->
    <Modal
      v-model:visible="detailModalVisible"
      :title="detailModalTitle"
      width="900px"
      :okButtonProps="{ style: { display: 'none' } }"
      :cancelButtonProps="{ style: { display: 'none' } }"
      :footer="[]"
    >
      <div v-if="logDetail" class="log-detail">
        <div class="detail-header">
          <Typography.Title :level="4">
            {{ logDetail.jobName }} - {{ logDetail.jobGroup }}
          </Typography.Title>
          <div style="display: flex; gap: 15px; margin-bottom: 20px">
            <div>
              <strong>执行状态:</strong>
              <Tag :color="logStatusMap[logDetail.status].status">
                {{ logStatusMap[logDetail.status].text }}
              </Tag>
            </div>
            <div><strong>执行时长:</strong> {{ logDetail.duration }} ms</div>
            <div><strong>执行时间:</strong> {{ logDetail.startTime }}</div>
          </div>
        </div>

        <div class="detail-content">
          <Typography.Title :level="5">执行信息</Typography.Title>
          <pre
            style="
              background: #f5f5f5;
              padding: 10px;
              border-radius: 4px;
              white-space: pre-wrap;
              word-break: break-word;
            "
          >
            {{ logDetail.executionInfo || '暂无执行信息' }}
          </pre>

          <div v-if="logDetail.errorInfo">
            <Typography.Title :level="5">错误信息</Typography.Title>
            <pre
              style="
                background: #fff1f0;
                padding: 10px;
                border-radius: 4px;
                color: #f5222d;
                white-space: pre-wrap;
                word-break: break-word;
              "
            >
              {{ logDetail.errorInfo }}
            </pre>
          </div>

          <div v-if="logDetail.parameters">
            <Typography.Title :level="5">执行参数</Typography.Title>
            <pre
              style="
                background: #f0f9ff;
                padding: 10px;
                border-radius: 4px;
                white-space: pre-wrap;
                word-break: break-word;
              "
            >
              {{ JSON.stringify(JSON.parse(logDetail.parameters), null, 2) }}
            </pre>
          </div>
        </div>
      </div>
    </Modal>
  </Page>
</template>

<style scoped>
/* 响应式布局调整 */
@media (max-width: 768px) {
  .ant-form-inline .ant-form-item {
    margin-right: 0;
    margin-bottom: 16px;
    width: 100%;
  }
  
  .ant-form-inline .ant-form-item-label {
    text-align: left;
  }
  
  .ant-card {
    margin-bottom: 16px !important;
  }
}
</style>