<script setup lang="ts">
import { ref, computed } from 'vue';
// 导入日期格式化工具
import { formatDateTime } from '@vben/utils';
import { Page } from '@vben/common-ui';
// 导入 vbenadmin 的 Vxe Table 适配器
import { useVbenVxeGrid } from '@vben/plugins/vxe-table';
import type { VxeTableGridOptions } from '@vben/plugins/vxe-table';
import {
  Button,
  Modal,
  Tag,
  message,
} from 'ant-design-vue';

// 导入i18n
import { $t } from '#/locales';

// 导入日志相关类型和API
import {
  LogStatusEnum,
  getLogList,
  clearLogs,
} from '../../api/quartz/log';
import type { LogQueryParams, LogResponseDto } from '../../api/quartz/log';

// 日志状态映射
const logStatusMap = {
  [LogStatusEnum.SUCCESS]: { text: () => $t('page.quartz.logPage.statusSuccess'), status: 'success' },
  [LogStatusEnum.ERROR]: { text: () => $t('page.quartz.logPage.statusError'), status: 'error' },
  [LogStatusEnum.RUNNING]: { text: () => $t('page.quartz.logPage.statusRunning'), status: 'processing' },
};

// 响应式数据

// 详情对话框
const detailModalVisible = ref(false);
const logDetail = ref<LogResponseDto | null>(null);

// 搜索条件由 VbenForm 自动注入到 query 的 formValues

// 详情顶部状态条颜色：成功绿 / 错误红 / 运行中蓝
const logStatusColor = computed(() => {
  const status = logDetail.value?.status;
  if (status === LogStatusEnum.SUCCESS) return '#52c41a';
  if (status === LogStatusEnum.ERROR) return '#ff4d4f';
  return '#1890ff';
});

// 详情页元数据 label 在上、value 在下，去掉 i18n 文案末尾冒号
const stripColon = (s: string) => (s || '').replace(/[:：]\s*$/, '');

// 列配置
const columns = [
  { type: 'seq', width: 60, title: '#', fixed: 'left' },
  {
    field: 'jobName',
    title: $t('page.quartz.logPage.jobName'),
    minWidth: 160,
    showOverflow: true,
  },
  {
    field: 'jobGroup',
    title: $t('page.quartz.logPage.jobGroup'),
    minWidth: 120,
    showOverflow: true,
  },
  {
    field: 'status',
    title: $t('page.quartz.logPage.status'),
    width: 100,
    align: 'center' as const,
    slots: { default: 'status' },
  },
  {
    field: 'startTime',
    title: $t('page.quartz.logPage.startTime'),
    width: 170,
    align: 'center' as const,
    sortable: true,
    slots: { default: 'datetime' },
  },
  {
    field: 'endTime',
    title: $t('page.quartz.logPage.endTime'),
    width: 170,
    align: 'center' as const,
    sortable: true,
    slots: { default: 'datetime' },
  },
  {
    field: 'duration',
    title: $t('page.quartz.logPage.duration'),
    width: 130,
    align: 'center' as const,
    sortable: true,
    slots: { default: 'duration' },
  },
  {
    title: $t('page.quartz.logPage.action'),
    width: 70,
    align: 'center' as const,
    fixed: 'right',
    slots: { default: 'action' },
  },
];

// 构造 Vxe Grid 配置
const gridOptions: VxeTableGridOptions<LogResponseDto> = {
  columns: columns as any,
  height: 'auto',
  showOverflow: true,
  rowConfig: { keyField: 'logId', isHover: true },
  sortConfig: {
    trigger: 'cell',
    remote: true,
    defaultSort: undefined as any,
  },
  columnConfig: { resizable: true },
  pagerConfig: { enabled: true },
  proxyConfig: {
    enabled: true,
    autoLoad: true,
    ajax: {
      query: async ({ page, sort }: any, formValues: any) => {
        // 保持原有行为：sortOrder 使用 asc/desc 形式
        const sortOrder =
          sort?.order === 'asc' ? 'asc' : sort?.order === 'desc' ? 'desc' : '';
        // RangePicker 返回 Day.js 数组 [begin, end]，拆分为后端范围参数
        // startTimeRange 查 StartTime 字段范围，endTimeRange 查 EndTime 字段范围
        const startTimeRange = formValues?.startTimeRange;
        const endTimeRange = formValues?.endTimeRange;
        const params = {
          jobName: formValues?.jobName,
          jobGroup: formValues?.jobGroup,
          status: formValues?.status,
          startStartTime: startTimeRange?.[0]?.format('YYYY-MM-DDTHH:mm:ss'),
          endStartTime: startTimeRange?.[1]?.format('YYYY-MM-DDTHH:mm:ss'),
          startEndTime: endTimeRange?.[0]?.format('YYYY-MM-DDTHH:mm:ss'),
          endEndTime: endTimeRange?.[1]?.format('YYYY-MM-DDTHH:mm:ss'),
          pageIndex: page.currentPage || 1,
          pageSize: page.pageSize || 10,
          sortBy: sort?.field ?? '',
          sortOrder,
        } as LogQueryParams;

        try {
          const response = await getLogList(params);
          if (response.success) {
            // 根据API定义，响应数据应该包含data字段，其中包含items和totalCount，现在还包含totalPages
            if (
              response.data &&
              response.data.items &&
              Array.isArray(response.data.items)
            ) {
              return {
                result: response.data.items,
                page: {
                  total: response.data.totalCount || 0,
                },
              };
            }
            return { result: [], page: { total: 0 } };
          }
          // 处理错误情况，包括可能的errorCode
          const errorMsg = response.errorCode
            ? `${response.message || $t('page.quartz.logPage.loadListFailed')} (${$t('page.quartz.logPage.errorCode')}: ${response.errorCode})`
            : response.message || $t('page.quartz.logPage.loadListFailed');
          message.error(errorMsg);
          return { result: [], page: { total: 0 } };
        } catch (error) {
          console.log($t('page.quartz.logPage.loadListFailed'), error);
          message.error(
            typeof error === 'object' && error !== null && 'message' in error
              ? String((error as any).message)
              : $t('page.quartz.logPage.loadListFailed'),
          );
          return { result: [], page: { total: 0 } };
        }
      },
    },
    sort: true,
  },
  toolbarConfig: {
    custom: true,
    refresh: true,
    zoom: true,
  },
};

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions,
  formOptions: {
    schema: [
      {
        component: 'Input',
        componentProps: { placeholder: $t('page.quartz.logPage.placeholderJobName') },
        fieldName: 'jobName',
        label: $t('page.quartz.logPage.jobName'),
      },
      {
        component: 'Input',
        componentProps: { placeholder: $t('page.quartz.logPage.placeholderJobGroup') },
        fieldName: 'jobGroup',
        label: $t('page.quartz.logPage.jobGroup'),
      },
      {
        component: 'Select',
        componentProps: {
          allowClear: true,
          placeholder: $t('page.quartz.logPage.placeholderStatus'),
          options: [
            { label: $t('page.quartz.logPage.statusSuccess'), value: LogStatusEnum.SUCCESS },
            { label: $t('page.quartz.logPage.statusError'), value: LogStatusEnum.ERROR },
            { label: $t('page.quartz.logPage.statusRunning'), value: LogStatusEnum.RUNNING },
          ],
        },
        fieldName: 'status',
        label: $t('page.quartz.logPage.executionStatus'),
      },
      {
        component: 'RangePicker',
        componentProps: { showTime: true },
        fieldName: 'startTimeRange',
        label: $t('page.quartz.logPage.startTime'),
      },
      {
        component: 'RangePicker',
        componentProps: { showTime: true },
        fieldName: 'endTimeRange',
        label: $t('page.quartz.logPage.endTime'),
      },
    ],
    showCollapseButton: true,
    collapsed: true,
    submitOnChange: false,
    submitOnEnter: true,
  },
});

// 搜索/重置由 VbenForm 内置提交按钮触发，无需手动处理

// 清空日志
const handleClear = () => {
  Modal.confirm({
    title: $t('page.quartz.logPage.confirmClear'),
    content: $t('page.quartz.logPage.confirmClearContent'),
    onOk: async () => {
      try {
        // 传递空的查询参数，清空所有日志，而不是使用当前搜索条件
        const response = await clearLogs({
          jobName: '',
          jobGroup: '',
          status: undefined,
          startTime: undefined,
          endTime: undefined,
        });
        if (response.success) {
          message.success($t('page.quartz.logPage.clearSuccess'));
          // 清空后重新加载日志列表
          await gridApi.query();
        } else {
          message.error(response.message || $t('page.quartz.logPage.clearFailed'));
        }
      } catch (error: any) {
        console.error($t('page.quartz.logPage.clearFailed'), error);
        message.error(error.message || $t('page.quartz.logPage.clearFailed'));
      }
    },
  });
};

// 查看详情
const handleDetail = (log: LogResponseDto) => {
  try {
    logDetail.value = log;
    detailModalVisible.value = true;
  } catch (error) {
    message.error($t('page.quartz.logPage.showDetailFailed'));
    console.log($t('page.quartz.logPage.showDetailFailed'), error);
  }
};
</script>

<template>
  <Page auto-content-height>
    <template #default>
      <!-- 日志列表 -->
      <Grid>
        <!-- 工具栏：清空日志按钮 -->
        <template #toolbar-actions>
          <div class="flex w-full items-center justify-end">
            <Button danger @click="handleClear">{{ $t('page.quartz.logPage.clearLogs') }}</Button>
          </div>
        </template>

        <!-- 日志状态 -->
        <template #status="{ row }">
          <Tag :color="logStatusMap[row.status as LogStatusEnum]?.status || 'default'">
            {{ logStatusMap[row.status as LogStatusEnum]?.text?.() || $t('page.quartz.logPage.unknown') }}
          </Tag>
        </template>

        <!-- 通用日期时间渲染 -->
        <template #datetime="{ row, column }">
          {{ (row as any)[column.field] ? formatDateTime((row as any)[column.field]) : '-' }}
        </template>

        <!-- 执行时长 -->
        <template #duration="{ row }">
          {{ row.duration != null ? `${row.duration} ms` : '-' }}
        </template>

        <!-- 操作列 -->
        <template #action="{ row }">
          <div class="flex items-center justify-center gap-1">
            <Tooltip :title="$t('page.quartz.logPage.detail')">
              <i class="vxe-icon-eye-fill text-primary cursor-pointer hover:opacity-80 px-1" @click="handleDetail(row)"></i>
            </Tooltip>
          </div>
        </template>
      </Grid>

      <!-- 详情对话框 -->
      <Modal v-model:open="detailModalVisible" :title="$t('page.quartz.logPage.logDetail')" width="720px" :footer="null"
        :destroyOnClose="true" centered>
        <div v-if="logDetail" class="log-detail">
          <!-- 顶部：状态色条 + 主标识 + 状态标签 -->
          <div class="detail-head">
            <div class="status-bar" :style="{ backgroundColor: logStatusColor }"></div>
            <div class="head-inner">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <div class="meta-label">{{ $t('page.quartz.logPage.jobName') }} / {{ $t('page.quartz.logPage.jobGroup') }}</div>
                  <div class="head-title">{{ logDetail.jobName }} · {{ logDetail.jobGroup }}</div>
                </div>
                <Tag :color="logStatusMap[logDetail.status].status" class="detail-tag">
                  {{ logStatusMap[logDetail.status].text() }}
                </Tag>
              </div>
            </div>
          </div>

          <!-- 元数据：定义列表式 -->
          <div class="meta-grid">
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.logPage.executionDuration')) }}</div>
              <div class="meta-value">{{ logDetail.duration || 0 }} <span class="meta-unit">ms</span></div>
            </div>
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.logPage.startDateTime')) }}</div>
              <div class="meta-value">{{ formatDateTime(logDetail.startTime) }}</div>
            </div>
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.logPage.endDateTime')) }}</div>
              <div class="meta-value">{{ logDetail.endTime ? formatDateTime(logDetail.endTime) : '—' }}</div>
            </div>
          </div>

          <!-- 内容区 -->
          <div class="detail-body">
            <section class="detail-section">
              <div class="section-title">{{ $t('page.quartz.logPage.executionInfo') }}</div>
              <pre class="code-panel">{{ logDetail.message || $t('page.quartz.logPage.noExecutionInfo') }}</pre>
            </section>

            <section v-if="logDetail.errorMessage" class="detail-section">
              <div class="section-title">
                {{ $t('page.quartz.logPage.errorInfo') }}
                <span class="section-tag section-tag-error">Error</span>
              </div>
              <pre class="code-panel code-panel-error">{{ logDetail.errorMessage }}</pre>
            </section>

            <section v-if="logDetail.exception" class="detail-section">
              <div class="section-title">
                {{ $t('page.quartz.logPage.exceptionInfo') }}
                <span class="section-tag section-tag-error">Exception</span>
              </div>
              <pre class="code-panel code-panel-error">{{ logDetail.exception }}</pre>
            </section>

            <section v-if="logDetail.result" class="detail-section">
              <div class="section-title">
                {{ $t('page.quartz.logPage.executionResult') }}
                <span class="section-tag section-tag-success">Result</span>
              </div>
              <pre class="code-panel">{{ typeof logDetail.result === 'string' ? logDetail.result : JSON.stringify(logDetail.result, null, 2) }}</pre>
            </section>

            <section v-if="logDetail.jobData" class="detail-section">
              <div class="section-title">{{ $t('page.quartz.logPage.jobData') }}</div>
              <pre class="code-panel">{{ typeof logDetail.jobData === 'string' ? logDetail.jobData : JSON.stringify(logDetail.jobData, null, 2) }}</pre>
            </section>
          </div>

          <!-- 底部按钮 -->
          <div class="detail-footer">
            <Button @click="detailModalVisible = false" type="primary">
              {{ $t('page.quartz.logPage.close') }}
            </Button>
          </div>
        </div>
      </Modal>
    </template>
  </Page>
</template>

<style scoped>
/* ============ 详情对话框 ============ */
.log-detail {
  --detail-gap: 1.25rem;
}

/* 顶部：状态色条 + 主标识 */
.detail-head {
  display: flex;
  align-items: stretch;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid var(--color-border);
  background: var(--color-fill-quaternary, rgba(0, 0, 0, 0.02));
}

.status-bar {
  width: 4px;
  flex-shrink: 0;
}

.head-inner {
  flex: 1;
  min-width: 0;
  padding: 0.875rem 1.125rem;
}

.head-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-text);
  line-height: 1.4;
  word-break: break-all;
}

.detail-tag {
  margin: 0;
  flex-shrink: 0;
}

/* 元数据：定义列表式 */
.meta-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.875rem 1.5rem;
  margin-top: var(--detail-gap);
  padding: 0.875rem 1.125rem;
  border-radius: 10px;
  border: 1px solid var(--color-border);
  background: var(--color-fill-quaternary, rgba(0, 0, 0, 0.02));
}

.meta-item {
  min-width: 0;
}

.meta-label {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
  margin-bottom: 0.25rem;
  line-height: 1.4;
}

.meta-value {
  font-size: 0.875rem;
  color: var(--color-text);
  font-weight: 500;
  word-break: break-all;
  line-height: 1.4;
}

.meta-unit {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
  font-weight: 400;
}

/* 内容区 */
.detail-body {
  margin-top: var(--detail-gap);
  display: flex;
  flex-direction: column;
  gap: 1.125rem;
}

.detail-section {
  min-width: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--color-text);
  margin-bottom: 0.5rem;
}

.section-tag {
  font-size: 0.625rem;
  font-weight: 700;
  padding: 0.0625rem 0.4375rem;
  border-radius: 4px;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  line-height: 1.5;
}

.section-tag-error {
  background: rgba(255, 77, 79, 0.1);
  color: #ff4d4f;
}

.section-tag-success {
  background: rgba(82, 196, 26, 0.1);
  color: #52c41a;
}

/* 统一中性代码面板 */
.code-panel {
  margin: 0;
  padding: 0.875rem 1rem;
  background: var(--color-fill-quaternary, rgba(0, 0, 0, 0.02));
  border: 1px solid var(--color-border);
  border-radius: 8px;
  color: var(--color-text);
  font-family: 'JetBrains Mono', 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 0.8125rem;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
  overflow-x: auto;
  max-height: 360px;
  overflow-y: auto;
}

/* 错误类：左侧色条 + 淡红底，不整块染色 */
.code-panel-error {
  border-left: 3px solid #ff4d4f;
  background: rgba(255, 77, 79, 0.04);
}

/* 底部按钮 */
.detail-footer {
  margin-top: var(--detail-gap);
  display: flex;
  justify-content: flex-end;
}

/* 响应式 */
@media (max-width: 640px) {
  .meta-grid {
    grid-template-columns: 1fr;
    gap: 0.75rem;
  }

  .head-inner {
    padding: 0.75rem 0.875rem;
  }

  .code-panel {
    font-size: 0.75rem;
  }
}

.mb-4 {
  margin-bottom: 16px;
}

.text-right {
  text-align: right;
}

.flex {
  display: flex;
}

.w-full {
  width: 100%;
}

.items-center {
  align-items: center;
}

.justify-end {
  justify-content: flex-end;
}
</style>
