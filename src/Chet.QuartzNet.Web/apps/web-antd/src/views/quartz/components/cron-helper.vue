<template>
  <Modal :open="visible" title="Cron 表达式帮助手册 💡" @cancel="handleCancel" width="850px" :footer="null" :z-index="10000"
    centered destroyOnClose>
    <div class="cron-helper-container">
      <section class="section-box">
        <div class="section-title">常用表达式示例</div>
        <Table :columns="cronColumns" :data-source="cronExamples" :pagination="false" size="middle"
          class="custom-table">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'expression'">
              <code class="cron-code">{{ record.expression }}</code>
            </template>
            <template v-if="column.key === 'action'">
              <Button type="link" size="small" @click="handleSelectCron(record)">选择</Button>
            </template>
          </template>
        </Table>
      </section>

      <section class="section-box mt-6">
        <div class="section-title">Cron 格式详解</div>
        <Alert class="custom-alert mb-4" type="info" show-icon>
          <template #message>
            标准格式：<span class="format-tag">[秒] [分] [时] [日] [月] [周] [年]</span>
          </template>
        </Alert>

        <div class="format-grid">
          <div v-for="item in formatInfo" :key="item.field" class="format-card">
            <div class="card-header">
              <span class="field-name">{{ item.field }}</span>
              <span class="range-tag">{{ item.range }}</span>
            </div>
            <div class="card-body">
              <div class="symbols">支持：<code>{{ item.symbols }}</code></div>
            </div>
          </div>
        </div>
      </section>
    </div>
  </Modal>
</template>

<script setup lang="ts">
import { Modal, Card, Button, Table, Alert, Tag } from 'ant-design-vue';
import type { ColumnsType } from 'ant-design-vue';

// ... Props & Emits 定义保持一致 ...
const props = defineProps<{ visible: boolean }>();
const emit = defineEmits(['cancel', 'select', 'update:visible']);

const cronExamples = [
  { id: '1', name: '每秒执行', expression: '*/1 * * * * ?', description: '系统最高频率触发' },
  { id: '2', name: '每分钟', expression: '0 */1 * * * ?', description: '每分钟的 0 秒触发' },
  { id: '3', name: '每小时', expression: '0 0 */1 * * ?', description: '整点触发' },
  { id: '4', name: '每天凌晨', expression: '0 0 0 * * ?', description: '每天 00:00:00 执行' },
  { id: '5', name: '每周一', expression: '0 0 0 ? * MON', description: '周一凌晨执行' },
  { id: '6', name: '每月1号', expression: '0 0 0 1 * ?', description: '月初凌晨执行' },
];

const formatInfo = [
  { field: '秒', range: '0-59', symbols: '*, -, ,, /' },
  { field: '分', range: '0-59', symbols: '*, -, ,, /' },
  { field: '时', range: '0-23', symbols: '*, -, ,, /' },
  { field: '日', range: '1-31', symbols: '*, -, ,, /, ?, L, W' },
  { field: '月', range: '1-12/JAN-DEC', symbols: '*, -, ,, /' },
  { field: '周', range: '1-7/SUN-SAT', symbols: '*, -, ,, /, ?, L, #' },
];

const cronColumns: ColumnsType<any> = [
  { title: '业务场景', dataIndex: 'name', key: 'name', width: 140 },
  { title: '表达式', dataIndex: 'expression', key: 'expression', width: 180 },
  { title: '执行逻辑', dataIndex: 'description', key: 'description' },
  { title: '操作', key: 'action', width: 80, align: 'center' },
];

const handleSelectCron = (record: any) => {
  emit('select', record.expression);
  emit('update:visible', false);
};

const handleCancel = () => emit('update:visible', false);
</script>

<style scoped lang="less">
.cron-helper-container {
  padding: 8px 4px;

  .section-box {
    margin-bottom: 24px;

    .section-title {
      font-size: 16px;
      font-weight: 600;
      margin-bottom: 16px;
      padding-left: 8px;
      border-left: 4px solid #1890ff;
      color: var(--ant-text-color, #262626);
    }
  }

  // 表达式代码块样式
  .cron-code {
    padding: 2px 8px;
    background: #f5f5f5;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    color: #c41d7f;
    font-family: 'Courier New', Courier, monospace;
    font-weight: bold;
  }

  // 格式卡片布局
  .format-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;

    .format-card {
      background: #ffffff;
      border: 1px solid #f0f0f0;
      border-radius: 8px;
      padding: 12px;

      .card-header {
        display: flex;
        justify-content: space-between;
        margin-bottom: 8px;

        .field-name {
          font-weight: bold;
          color: #262626;
        }

        .range-tag {
          font-size: 11px;
          background: #e6f7ff;
          color: #1890ff;
          padding: 0 6px;
          border-radius: 4px;
        }
      }

      .card-body {
        font-size: 12px;
        color: #8c8c8c;

        code {
          color: #1890ff;
        }
      }
    }
  }
}

/* ======================================================
   核心修复：Vben / Ant Design 暗色主题强制覆盖
   ====================================================== */
:where(.dark) {

  // 1. 容器整体文字颜色
  .cron-helper-container {
    color: rgba(255, 255, 255, 0.85);
  }

  // 2. 表格背景与文字 (解决表格浅色问题)
  :deep(.ant-table) {
    background: #1f1f1f !important;
    color: rgba(255, 255, 255, 0.85);
  }

  :deep(.ant-table-thead > tr > th) {
    background: #262626 !important;
    color: rgba(255, 255, 255, 0.85);
    border-bottom: 1px solid #303030;
  }

  :deep(.ant-table-tbody > tr > td) {
    border-bottom: 1px solid #303030;
  }

  :deep(.ant-table-tbody > tr:hover > td) {
    background: #262626 !important;
  }

  // 3. 表达式代码块暗色适配
  .cron-code {
    background: #2a2a2a !important;
    border-color: #434343 !important;
    color: #ff7adb !important; // 暗色下用亮粉色更清晰
  }

  // 4. Alert 组件暗色适配
  :deep(.custom-alert) {
    background-color: #111b26 !important;
    border: 1px solid #153450 !important;

    .ant-alert-message {
      color: rgba(255, 255, 255, 0.85) !important;
    }
  }

  // 5. 格式详解卡片暗色适配
  .format-card {
    background: #1f1f1f !important;
    border-color: #303030 !important;

    .card-header {
      .field-name {
        color: rgba(255, 255, 255, 0.85) !important;
      }

      .range-tag {
        background: #111b26 !important;
        color: #177ddc !important;
      }
    }

    .card-body {
      color: rgba(255, 255, 255, 0.45) !important;

      code {
        color: #177ddc !important;
      }
    }
  }

  .section-title {
    color: rgba(255, 255, 255, 0.85) !important;
  }
}
</style>
