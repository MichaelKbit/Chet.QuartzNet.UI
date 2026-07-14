<script setup lang="ts">
import { ref, computed, reactive } from 'vue';
import { formatDateTime } from '@vben/utils';
import { Page } from '@vben/common-ui';
// 导入 vbenadmin 的 Vxe Table 适配器
import { useVbenVxeGrid } from '@vben/plugins/vxe-table';
import type { VxeTableGridOptions } from '@vben/plugins/vxe-table';
import {
  Button,
  Input,
  Select,
  Space,
  Modal,
  Form,
  Switch,
  message,
  Tag,
  Row,
  Col,
  Tooltip,
  InputNumber,
  Alert,
} from 'ant-design-vue';
import type { FormInstance } from 'ant-design-vue';

// 导入i18n
import { $t } from '#/locales';

import {
  NotificationStatusEnum,
  getPushPlusConfig,
  savePushPlusConfig,
  sendTestNotification,
  getNotifications,
  deleteNotification,
  clearNotifications,
} from '../../api/quartz/notification';
import type {
  PushPlusConfigDto,
  QuartzNotificationDto,
  NotificationQueryDto,
} from '../../api/quartz/notification';

// 通知状态映射
const notificationStatusMap = {
  [NotificationStatusEnum.Pending]: { text: () => $t('page.quartz.notificationPage.statusPending'), status: 'default' },
  [NotificationStatusEnum.Sent]: { text: () => $t('page.quartz.notificationPage.statusSent'), status: 'success' },
  [NotificationStatusEnum.Failed]: { text: () => $t('page.quartz.notificationPage.statusFailed'), status: 'error' },
};

// 响应式数据
const loading = ref(false);
const saveLoading = ref(false);

// 详情对话框
const detailModalVisible = ref(false);
const currentNotification = ref<QuartzNotificationDto | null>(null);

// 搜索条件由 VbenForm 自动注入到 query 的 formValues

// 详情顶部状态条颜色：已发送绿 / 失败红 / 待发送琥珀
const notificationStatusColor = computed(() => {
  const status = currentNotification.value?.status;
  if (status === NotificationStatusEnum.Sent) return '#52c41a';
  if (status === NotificationStatusEnum.Failed) return '#ff4d4f';
  return '#faad14';
});

// 详情页元数据 label 在上、value 在下，去掉 i18n 文案末尾冒号
const stripColon = (s: string) => (s || '').replace(/[:：]\s*$/, '');

// 编辑对话框
const configModalVisible = ref(false);
const configForm = reactive<PushPlusConfigDto>({
  token: '',
  channel: 'wechat',
  template: 'html',
  topic: '',
  option: '',
  to: '',
  callbackUrl: '',
  timestamp: undefined,
  enable: false,
  strategy: {
    notifyOnJobSuccess: false,
    notifyOnJobFailure: true,
    notifyOnSchedulerError: true,
  },
});

const formRef = ref<FormInstance>();
const advancedVisible = ref(false);

// Option 动态占位符（根据渠道变化）
const optionPlaceholder = computed(() => {
  const placeholders: Record<string, string> = {
    webhook: $t('page.quartz.notificationPage.optionPlaceholderWebhook'),
    cp: $t('page.quartz.notificationPage.optionPlaceholderCp'),
    mail: $t('page.quartz.notificationPage.optionPlaceholderMail'),
  };
  return placeholders[configForm.channel] || '';
});

// 是否显示渠道参数区域
const showChannelParams = computed(() => {
  return ['webhook', 'cp', 'mail', 'wechat'].includes(configForm.channel);
});

// 渠道提示信息
const channelTipMessage = computed(() => {
  const tips: Record<string, string> = {
    webhook: $t('page.quartz.notificationPage.channelTipWebhook'),
    cp: $t('page.quartz.notificationPage.channelTipCp'),
    mail: $t('page.quartz.notificationPage.channelTipMail'),
    wechat: $t('page.quartz.notificationPage.channelTipWechat'),
  };
  return tips[configForm.channel] || '';
});

// 列配置
const columns = [
  { type: 'seq', width: 60, title: '#', fixed: 'left' },
  {
    field: 'title',
    title: $t('page.quartz.notificationPage.title'),
    minWidth: 200,
    showOverflow: true,
  },
  {
    field: 'triggeredBy',
    title: $t('page.quartz.notificationPage.triggeredBy'),
    minWidth: 120,
    showOverflow: true,
  },
  {
    field: 'status',
    title: $t('page.quartz.notificationPage.status'),
    width: 100,
    align: 'center' as const,
    slots: { default: 'status' },
  },
  {
    field: 'sendTime',
    title: $t('page.quartz.notificationPage.sendTime'),
    width: 170,
    align: 'center' as const,
    sortable: true,
    slots: { default: 'datetime' },
  },
  {
    field: 'duration',
    title: $t('page.quartz.notificationPage.duration'),
    width: 110,
    align: 'right' as const,
    sortable: true,
    slots: { default: 'duration' },
  },
  {
    field: 'createTime',
    title: $t('page.quartz.notificationPage.createTime'),
    width: 170,
    align: 'center' as const,
    sortable: true,
    slots: { default: 'datetime' },
  },
  {
    title: $t('page.quartz.notificationPage.action'),
    width: 80,
    align: 'center' as const,
    fixed: 'right',
    slots: { default: 'action' },
  },
];

// 构造 Vxe Grid 配置
const gridOptions: VxeTableGridOptions<QuartzNotificationDto> = {
  columns: columns as any,
  height: 'auto',
  showOverflow: true,
  rowConfig: { keyField: 'notificationId', isHover: true },
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
        // 保持原有行为：sortOrder 使用 ascend/descend 形式
        const sortOrder =
          sort?.order === 'asc' ? 'ascend' : sort?.order === 'desc' ? 'descend' : undefined;
        const params: NotificationQueryDto = {
          status: formValues?.status,
          triggeredBy: formValues?.triggeredBy,
          pageIndex: page.currentPage || 1,
          pageSize: page.pageSize || 20,
          sortBy: sort?.field ?? '',
          sortOrder,
        };

        try {
          const response = await getNotifications(params);
          if (response.success) {
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
          message.error(response.message || $t('page.quartz.notificationPage.loadListFailed'));
          return { result: [], page: { total: 0 } };
        } catch (error) {
          console.error($t('page.quartz.notificationPage.loadListFailed'), error);
          message.error(
            typeof error === 'object' && error !== null && 'message' in error
              ? String((error as any).message)
              : $t('page.quartz.notificationPage.loadListFailed'),
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
        component: 'Select',
        componentProps: {
          allowClear: true,
          placeholder: $t('page.quartz.notificationPage.placeholderStatus'),
          options: [
            { label: $t('page.quartz.notificationPage.statusPending'), value: NotificationStatusEnum.Pending },
            { label: $t('page.quartz.notificationPage.statusSent'), value: NotificationStatusEnum.Sent },
            { label: $t('page.quartz.notificationPage.statusFailed'), value: NotificationStatusEnum.Failed },
          ],
        },
        fieldName: 'status',
        label: $t('page.quartz.notificationPage.notificationStatus'),
      },
      {
        component: 'Input',
        componentProps: { placeholder: $t('page.quartz.notificationPage.placeholderTriggeredBy') },
        fieldName: 'triggeredBy',
        label: $t('page.quartz.notificationPage.triggeredBy'),
      },
    ],
    showCollapseButton: false,
    submitOnChange: false,
    submitOnEnter: true,
  },
});

// 搜索/重置由 VbenForm 内置提交按钮触发，无需手动处理

// 打开配置对话框
const handleOpenConfigModal = async () => {
  try {
    const response = await getPushPlusConfig() as any;
    Object.assign(configForm, response.data || response);
    configModalVisible.value = true;
  } catch (error) {
    message.error($t('page.quartz.notificationPage.getConfigFailed'));
    console.error($t('page.quartz.notificationPage.getConfigFailed'), error);
  }
};

// 保存配置
const handleSaveConfig = async () => {
  if (!formRef.value) return;

  try {
    await formRef.value.validateFields();
    saveLoading.value = true;

    const response = await savePushPlusConfig(configForm);
    if (response.success) {
      message.success($t('page.quartz.notificationPage.saveConfigSuccess'));
      configModalVisible.value = false;
    } else {
      message.error(response.message || $t('page.quartz.notificationPage.saveConfigFailed'));
    }
  } catch (error: any) {
    if (error.errorFields) {
      return;
    }
    const errorMessage = error.message || $t('page.quartz.notificationPage.saveConfigFailed');
    message.error(errorMessage);
    console.error($t('page.quartz.notificationPage.saveConfigFailed'), error);
  } finally {
    saveLoading.value = false;
  }
};

// 发送测试通知
const handleSendTest = async () => {
  try {
    loading.value = true;
    const response = await sendTestNotification();
    if (response.success) {
      message.success($t('page.quartz.notificationPage.testSendSuccess'));
      gridApi.query();
    } else {
      message.error(response.message || $t('page.quartz.notificationPage.testSendFailed'));
    }
  } catch (error) {
    message.error($t('page.quartz.notificationPage.testSendFailed'));
    console.error($t('page.quartz.notificationPage.testSendFailed'), error);
  } finally {
    loading.value = false;
  }
};

// 查看详情
const handleDetail = (notification: QuartzNotificationDto) => {
  currentNotification.value = notification;
  detailModalVisible.value = true;
};

// 删除通知
const handleDelete = (notification: QuartzNotificationDto) => {
  Modal.confirm({
    title: $t('page.quartz.notificationPage.confirmDelete'),
    content: $t('page.quartz.notificationPage.confirmDeleteContent'),
    okText: $t('page.quartz.notificationPage.ok'),
    okType: 'danger',
    cancelText: $t('page.quartz.notificationPage.cancel'),
    async onOk() {
      try {
        const response = await deleteNotification(notification.notificationId);
        if (response.success) {
          message.success($t('page.quartz.notificationPage.deleteSuccess'));
          gridApi.query();
        } else {
          message.error(response.message || $t('page.quartz.notificationPage.deleteFailed'));
        }
      } catch (error) {
        message.error($t('page.quartz.notificationPage.deleteFailed'));
        console.error($t('page.quartz.notificationPage.deleteFailed'), error);
      }
    },
  });
};

// 清空通知
const handleClearNotifications = () => {
  Modal.confirm({
    title: $t('page.quartz.notificationPage.confirmClear'),
    content: $t('page.quartz.notificationPage.confirmClearContent'),
    okText: $t('page.quartz.notificationPage.ok'),
    okType: 'danger',
    cancelText: $t('page.quartz.notificationPage.cancel'),
    async onOk() {
      try {
        const formValues = await gridApi.formApi.getValues();
        const response = await clearNotifications({
          pageIndex: 1,
          pageSize: 1,
          status: formValues?.status,
          triggeredBy: formValues?.triggeredBy,
        });
        if (response.success) {
          message.success($t('page.quartz.notificationPage.clearSuccess'));
          gridApi.query();
        } else {
          message.error(response.message || $t('page.quartz.notificationPage.clearFailed'));
        }
      } catch (error) {
        message.error($t('page.quartz.notificationPage.clearFailed'));
        console.error($t('page.quartz.notificationPage.clearFailed'), error);
      }
    },
  });
};
</script>

<template>
  <Page auto-content-height>
    <template #default>
      <!-- 通知列表 -->
      <Grid>
        <!-- 工具栏：配置/测试/清空按钮 -->
        <template #toolbar-actions>
          <div class="flex w-full items-center justify-between">
            <Space>
              <Button type="primary" @click="handleOpenConfigModal">{{ $t('page.quartz.notificationPage.notificationConfig') }}</Button>
              <Button type="default" :loading="loading" @click="handleSendTest">{{ $t('page.quartz.notificationPage.sendTestNotification') }}</Button>
            </Space>
            <Button danger @click="handleClearNotifications">{{ $t('page.quartz.notificationPage.clearAll') }}</Button>
          </div>
        </template>

        <!-- 通知状态 -->
        <template #status="{ row }">
          <Tag :color="notificationStatusMap[row.status as NotificationStatusEnum]?.status || 'default'">
            {{ notificationStatusMap[row.status as NotificationStatusEnum]?.text?.() || $t('page.quartz.notificationPage.unknown') }}
          </Tag>
        </template>

        <!-- 通用日期时间渲染 -->
        <template #datetime="{ row, column }">
          {{ (row as any)[column.field] ? formatDateTime((row as any)[column.field]) : '-' }}
        </template>

        <!-- 发送时长 -->
        <template #duration="{ row }">
          {{ row.duration != null ? `${row.duration} ms` : '-' }}
        </template>

        <!-- 操作列 -->
        <template #action="{ row }">
          <div class="flex items-center justify-center gap-2">
            <Tooltip :title="$t('page.quartz.notificationPage.detail')">
              <i class="vxe-icon-eye-fill text-primary cursor-pointer hover:opacity-80" @click="handleDetail(row)"></i>
            </Tooltip>
            <Tooltip :title="$t('page.quartz.notificationPage.delete')">
              <i class="vxe-icon-delete cursor-pointer hover:opacity-80" style="color: var(--ant-color-error)" @click="handleDelete(row)"></i>
            </Tooltip>
          </div>
        </template>
      </Grid>

      <!-- 配置对话框 -->
      <Modal v-model:open="configModalVisible" :title="$t('page.quartz.notificationPage.notificationConfig')" width="720px" destroyOnClose
        @cancel="configModalVisible = false" centered>
        <div class="config-modal-content">
          <Alert :message="$t('page.quartz.notificationPage.configPushPlusDesc')" type="info" show-icon class="config-tip-alert" />

          <Form ref="formRef" :model="configForm" layout="vertical" class="custom-form" size="small">
            <!-- 基础配置 -->
            <section class="form-section">
              <div class="section-header">
                <span class="title">{{ $t('page.quartz.notificationPage.basicConfig') }}</span>
                <div class="header-action">
                  <span class="label">{{ $t('page.quartz.notificationPage.serviceEnableStatus') }}</span>
                  <Switch v-model:checked="configForm.enable" size="small" />
                </div>
              </div>

              <Row :gutter="12" align="middle">
                <Col :span="16">
                  <Form.Item label="PushPlus Token" name="token"
                    :rules="[{ required: configForm.enable, message: $t('page.quartz.notificationPage.tokenRequired') }]">
                    <Input.Password v-model:value="configForm.token" :placeholder="$t('page.quartz.notificationPage.tokenPlaceholder')" autocomplete="off" />
                  </Form.Item>
                </Col>
                <Col :span="8">
                  <Form.Item :label="$t('page.quartz.notificationPage.topicLabel')" name="topic">
                    <Input v-model:value="configForm.topic" :placeholder="$t('page.quartz.notificationPage.topicPlaceholder')" />
                  </Form.Item>
                </Col>
                <Col :span="8">
                  <Form.Item :label="$t('page.quartz.notificationPage.pushChannel')" name="channel">
                    <Select v-model:value="configForm.channel">
                      <Select.Option value="wechat">{{ $t('page.quartz.notificationPage.channelWechat') }}</Select.Option>
                      <Select.Option value="cp">{{ $t('page.quartz.notificationPage.channelWechatWork') }}</Select.Option>
                      <Select.Option value="webhook">{{ $t('page.quartz.notificationPage.channelWebhook') }}</Select.Option>
                      <Select.Option value="mail">{{ $t('page.quartz.notificationPage.channelEmail') }}</Select.Option>
                      <Select.Option value="sms">{{ $t('page.quartz.notificationPage.channelSms') }}</Select.Option>
                      <Select.Option value="voice">{{ $t('page.quartz.notificationPage.channelVoice') }}</Select.Option>
                      <Select.Option value="extension">{{ $t('page.quartz.notificationPage.channelExtension') }}</Select.Option>
                      <Select.Option value="app">{{ $t('page.quartz.notificationPage.channelApp') }}</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col :span="8">
                  <Form.Item :label="$t('page.quartz.notificationPage.messageTemplate')" name="template">
                    <Select v-model:value="configForm.template">
                      <Select.Option value="html">{{ $t('page.quartz.notificationPage.templateHtml') }}</Select.Option>
                      <Select.Option value="txt">{{ $t('page.quartz.notificationPage.templateTxt') }}</Select.Option>
                      <Select.Option value="json">{{ $t('page.quartz.notificationPage.templateJson') }}</Select.Option>
                      <Select.Option value="markdown">{{ $t('page.quartz.notificationPage.templateMarkdown') }}</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col v-if="['webhook', 'cp', 'mail'].includes(configForm.channel)" :span="8">
                  <Form.Item :label="$t('page.quartz.notificationPage.optionLabel')" name="option"
                    :rules="[{ required: ['webhook', 'cp'].includes(configForm.channel), message: $t('page.quartz.notificationPage.optionRequired') }]">
                    <Input v-model:value="configForm.option" :placeholder="optionPlaceholder" />
                  </Form.Item>
                </Col>
                <Col v-if="['wechat', 'cp'].includes(configForm.channel)" :span="8">
                  <Form.Item :label="$t('page.quartz.notificationPage.toLabel')" name="to">
                    <Input v-model:value="configForm.to" :placeholder="$t('page.quartz.notificationPage.toPlaceholder')" />
                  </Form.Item>
                </Col>
              </Row>

              <Alert v-if="showChannelParams" :message="channelTipMessage" type="warning" show-icon class="channel-tip" />
            </section>

            <!-- 通知策略 -->
            <section class="form-section last">
              <div class="section-header">
                <span class="title">{{ $t('page.quartz.notificationPage.notificationStrategy') }}</span>
              </div>

              <div class="strategy-grid">
                <div class="strategy-item">
                  <div class="strategy-info">
                    <div class="name">{{ $t('page.quartz.notificationPage.jobSuccess') }}</div>
                    <div class="desc">{{ $t('page.quartz.notificationPage.jobSuccessDesc') }}</div>
                  </div>
                  <Switch v-model:checked="configForm.strategy.notifyOnJobSuccess" />
                </div>

                <div class="strategy-item">
                  <div class="strategy-info">
                    <div class="name">{{ $t('page.quartz.notificationPage.jobFailure') }}</div>
                    <div class="desc">{{ $t('page.quartz.notificationPage.jobFailureDesc') }}</div>
                  </div>
                  <Switch v-model:checked="configForm.strategy.notifyOnJobFailure" />
                </div>

                <div class="strategy-item">
                  <div class="strategy-info">
                    <div class="name">{{ $t('page.quartz.notificationPage.schedulerError') }}</div>
                    <div class="desc">{{ $t('page.quartz.notificationPage.schedulerErrorDesc') }}</div>
                  </div>
                  <Switch v-model:checked="configForm.strategy.notifyOnSchedulerError" />
                </div>
              </div>
            </section>

            <!-- 高级配置 -->
            <div class="advanced-section">
              <div class="section-header" @click="advancedVisible = !advancedVisible">
                <span class="title">{{ $t('page.quartz.notificationPage.advancedConfig') }}</span>
                <span class="toggle-icon" :class="{ expanded: advancedVisible }">›</span>
              </div>
              <div v-show="advancedVisible" class="advanced-body">
                <Row :gutter="12">
                  <Col :span="16">
                    <Form.Item :label="$t('page.quartz.notificationPage.callbackUrlLabel')" name="callbackUrl">
                      <Input v-model:value="configForm.callbackUrl" :placeholder="$t('page.quartz.notificationPage.callbackUrlPlaceholder')" />
                    </Form.Item>
                  </Col>
                  <Col :span="8">
                    <Form.Item :label="$t('page.quartz.notificationPage.timestampLabel')" name="timestamp">
                      <InputNumber v-model:value="configForm.timestamp" :placeholder="$t('page.quartz.notificationPage.timestampPlaceholder')"
                        :precision="0" :min="0" style="width: 100%" />
                    </Form.Item>
                  </Col>
                </Row>
              </div>
            </div>
          </Form>
        </div>

        <template #footer>
          <div class="modal-footer">
            <Button @click="configModalVisible = false">{{ $t('page.quartz.notificationPage.cancel') }}</Button>
            <Button type="primary" :loading="saveLoading" @click="handleSaveConfig">{{ $t('page.quartz.notificationPage.saveConfig') }}</Button>
          </div>
        </template>
      </Modal>

      <!-- 详情对话框 -->
      <Modal v-model:open="detailModalVisible" :title="$t('page.quartz.notificationPage.notificationDetail')" width="720px"
        :footer="null" :destroyOnClose="true" centered>
        <div v-if="currentNotification" class="notification-detail">
          <!-- 顶部：状态色条 + 主标识 + 状态标签 -->
          <div class="detail-head">
            <div class="status-bar" :style="{ backgroundColor: notificationStatusColor }"></div>
            <div class="head-inner">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <div class="meta-label">{{ $t('page.quartz.notificationPage.notificationDetail') }}</div>
                  <div class="head-title">{{ currentNotification.title }}</div>
                </div>
                <Tag :color="notificationStatusMap[currentNotification.status].status" class="detail-tag">
                  {{ notificationStatusMap[currentNotification.status].text() }}
                </Tag>
              </div>
            </div>
          </div>

          <!-- 元数据：定义列表式 -->
          <div class="meta-grid meta-grid-4">
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.notificationPage.triggerSource')) }}</div>
              <div class="meta-value">{{ currentNotification.triggeredBy || '—' }}</div>
            </div>
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.notificationPage.sendDateTime')) }}</div>
              <div class="meta-value">{{ currentNotification.sendTime ? formatDateTime(currentNotification.sendTime) : '—' }}</div>
            </div>
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.notificationPage.sendDuration')) }}</div>
              <div class="meta-value">{{ currentNotification.duration ? `${currentNotification.duration} ms` : '—' }}</div>
            </div>
            <div class="meta-item">
              <div class="meta-label">{{ stripColon($t('page.quartz.notificationPage.createDateTime')) }}</div>
              <div class="meta-value">{{ formatDateTime(currentNotification.createTime) }}</div>
            </div>
          </div>

          <!-- 内容区 -->
          <div class="detail-body">
            <section class="detail-section">
              <div class="section-title">{{ $t('page.quartz.notificationPage.notificationContent') }}</div>
              <div class="content-panel" v-html="currentNotification.content"></div>
            </section>

            <section v-if="currentNotification.errorMessage" class="detail-section">
              <div class="section-title">
                {{ $t('page.quartz.notificationPage.errorInfo') }}
                <span class="section-tag section-tag-error">Error</span>
              </div>
              <pre class="code-panel code-panel-error">{{ currentNotification.errorMessage }}</pre>
            </section>
          </div>

          <!-- 底部按钮 -->
          <div class="detail-footer">
            <Button @click="detailModalVisible = false" type="primary">
              {{ $t('page.quartz.notificationPage.close') }}
            </Button>
          </div>
        </div>
      </Modal>
    </template>
  </Page>
</template>

<style scoped>
/* ============ 详情对话框 ============ */
.notification-detail {
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
  word-break: break-word;
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

.meta-grid-4 {
  grid-template-columns: repeat(4, 1fr);
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

/* 通知内容：富文本面板 */
.content-panel {
  padding: 0.875rem 1rem;
  background: var(--color-fill-quaternary, rgba(0, 0, 0, 0.02));
  border: 1px solid var(--color-border);
  border-radius: 8px;
  color: var(--color-text);
  font-size: 0.875rem;
  line-height: 1.7;
  word-break: break-word;
  overflow-x: auto;
  max-height: 420px;
  overflow-y: auto;
}

.content-panel :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: 4px;
}

.content-panel :deep(a) {
  color: var(--ant-color-primary, #1677ff);
}

.content-panel :deep(table) {
  max-width: 100%;
  border-collapse: collapse;
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

/* 错误类：左侧色条 + 淡红底 */
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
  .meta-grid,
  .meta-grid-4 {
    grid-template-columns: 1fr;
    gap: 0.75rem;
  }

  .head-inner {
    padding: 0.75rem 0.875rem;
  }

  .code-panel,
  .content-panel {
    font-size: 0.75rem;
  }
}
</style>

<style scoped lang="less">
.config-modal-content {
  margin-top: -8px;

  :deep(.ant-form-item) {
    margin-bottom: 12px;
  }

  :deep(.ant-form-item-label) {
    padding-bottom: 2px;
  }

  :deep(.ant-form-item-label > label) {
    font-size: 13px;
  }

  .form-section {
    padding: 12px;
    background: var(--ant-color-fill-quaternary);
    border-radius: 8px;
    margin-bottom: 12px;
    border: 1px solid var(--ant-color-border-secondary);

    .section-header {
      display: flex;
      align-items: center;
      margin-bottom: 10px;
      padding-bottom: 8px;
      border-bottom: 1px solid var(--ant-color-border-split);

      .title {
        font-size: 14px;
        font-weight: 600;
        flex: 1;
        color: var(--ant-color-text);
      }

      .header-action {
        display: flex;
        align-items: center;
        gap: 8px;

        .label {
          font-size: 12px;
          color: var(--ant-color-text-description);
        }
      }
    }

    &.last {
      margin-bottom: 0;
    }
  }

  .channel-tip {
    border-radius: 6px;
    margin-top: 4px;
  }

  .config-tip-alert {
    margin-bottom: 12px;
  }

  .advanced-section {
    margin-top: 12px;
    background: var(--ant-color-fill-quaternary);
    border-radius: 8px;
    border: 1px solid var(--ant-color-border-secondary);
    overflow: hidden;

    .section-header {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      cursor: pointer;
      user-select: none;
      transition: background 0.2s;

      &:hover {
        background: var(--ant-color-fill-tertiary);
      }

      .title {
        font-size: 13px;
        font-weight: 600;
        color: var(--ant-color-text-description);
      }

      .toggle-icon {
        margin-left: 6px;
        font-size: 16px;
        color: var(--ant-color-text-description);
        transition: transform 0.2s ease;
        display: inline-block;
        line-height: 1;

        &.expanded {
          transform: rotate(90deg);
        }
      }
    }

    .advanced-body {
      padding: 0 12px 12px;
      border-top: 1px solid var(--ant-color-border-secondary);
    }
  }

  .strategy-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;

    .strategy-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 10px;
      background: var(--ant-component-background);
      border: 1px solid var(--ant-color-border-secondary);
      border-radius: 6px;
      transition: all 0.2s ease;

      &:hover {
        border-color: var(--ant-color-primary-border);
      }

      .strategy-info {
        .name {
          font-size: 13px;
          font-weight: 500;
          color: var(--ant-color-text);
        }

        .desc {
          font-size: 11px;
          color: var(--ant-color-text-description);
          margin-top: 1px;
        }
      }
    }
  }
}

.modal-footer {
  padding: 10px 0;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

::where(.dark) {
  .config-modal-content {
    .form-section {
      background: rgba(255, 255, 255, 0.04);
      border-color: #303030;
    }

    .advanced-section {
      background: rgba(255, 255, 255, 0.04);
      border-color: #303030;
    }

    .strategy-item {
      background: #141414 !important;
      border-color: #303030 !important;

      &:hover {
        border-color: var(--ant-color-primary) !important;
      }
    }
  }
}

.mb-3 {
  margin-bottom: 12px;
}
</style>
