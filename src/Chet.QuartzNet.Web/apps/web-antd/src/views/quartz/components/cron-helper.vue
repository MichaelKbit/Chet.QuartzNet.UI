<template>
  <Modal
    :open="visible"
    :title="$t('page.quartz.jobPage.cronHelper')"
    width="860px"
    :footer="null"
    :z-index="10000"
    centered
    destroyOnClose
    wrapClassName="quartz-cron-helper-modal"
    @cancel="handleCancel"
  >
    <div class="cron-doc">
      <!-- 常用表达式 -->
      <section class="doc-section">
        <header class="doc-section__head">
          <h3 class="doc-section__title">
            <span class="doc-section__bar"></span>
            常用表达式示例
          </h3>
          <span class="doc-section__hint">点击「选择」即可填入</span>
        </header>
        <Table
          :columns="cronColumns"
          :data-source="cronExamples"
          :pagination="false"
          size="middle"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'expression'">
              <code class="cron-code">{{ record.expression }}</code>
            </template>
            <template v-if="column.key === 'action'">
              <Button type="link" size="small" @click="handleSelectCron(record)">
                选择
              </Button>
            </template>
          </template>
        </Table>
      </section>

      <!-- Cron 格式详解 -->
      <section class="doc-section">
        <header class="doc-section__head">
          <h3 class="doc-section__title">
            <span class="doc-section__bar"></span>
            Cron 格式详解
          </h3>
        </header>

        <div class="format-banner">
          <span class="format-banner__icon">ℹ</span>
          <div class="format-banner__content">
            <span class="format-banner__label">标准格式</span>
            <code class="format-banner__pattern">[秒] [分] [时] [日] [月] [周] [年]</code>
            <span class="format-banner__note">年可省略</span>
          </div>
        </div>

        <div class="format-grid">
          <div v-for="item in formatInfo" :key="item.field" class="format-card">
            <div class="format-card__head">
              <span class="format-card__name">{{ item.field }}</span>
              <span class="format-card__range">{{ item.range }}</span>
            </div>
            <div class="format-card__body">
              <span class="format-card__support">支持</span>
              <code class="format-card__symbols">{{ item.symbols }}</code>
            </div>
          </div>
        </div>

        <div class="symbol-legend">
          <div class="symbol-legend__title">符号说明</div>
          <div class="symbol-legend__grid">
            <div v-for="s in symbolLegend" :key="s.symbol" class="symbol-legend__item">
              <code class="symbol-legend__symbol">{{ s.symbol }}</code>
              <span class="symbol-legend__desc">{{ s.desc }}</span>
            </div>
          </div>
        </div>
      </section>
    </div>
  </Modal>
</template>

<script setup lang="ts">
import { toRef } from 'vue';
import { Modal, Button, Table } from 'ant-design-vue';
import type { ColumnsType } from 'ant-design-vue/es/table';
import { useDraggableModal } from '../composables/use-draggable-modal';

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits(['cancel', 'select', 'update:visible']);

// 对话框支持拖动
useDraggableModal(toRef(props, 'visible'), 'quartz-cron-helper-modal');

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
  { field: '月', range: '1-12 / JAN-DEC', symbols: '*, -, ,, /' },
  { field: '周', range: '1-7 / SUN-SAT', symbols: '*, -, ,, /, ?, L, #' },
];

const symbolLegend = [
  { symbol: '*', desc: '任意值' },
  { symbol: ',', desc: '枚举多个值' },
  { symbol: '-', desc: '范围' },
  { symbol: '/', desc: '步长' },
  { symbol: '?', desc: '不指定（仅日/周）' },
  { symbol: 'L', desc: '最后（仅日/周）' },
  { symbol: 'W', desc: '最近工作日（仅日）' },
  { symbol: '#', desc: '第几周（仅周）' },
];

const cronColumns: ColumnsType<any> = [
  { title: '业务场景', dataIndex: 'name', key: 'name', width: 120 },
  { title: '表达式', dataIndex: 'expression', key: 'expression', width: 180 },
  { title: '执行逻辑', dataIndex: 'description', key: 'description' },
  { title: '操作', key: 'action', width: 70, align: 'center' },
];

const handleSelectCron = (record: any) => {
  emit('select', record.expression);
  emit('update:visible', false);
};

const handleCancel = () => emit('update:visible', false);
</script>

<style scoped lang="less">
.cron-doc {
  padding: 4px 0 8px;
  color: hsl(var(--foreground));
  font-size: 13px;
  line-height: 1.6;
}

.doc-section {
  margin-bottom: 24px;

  &:last-child {
    margin-bottom: 0;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 14px;
  }

  &__title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 15px;
    font-weight: 600;
    margin: 0;
    color: hsl(var(--foreground));
  }

  &__bar {
    width: 3px;
    height: 16px;
    background: hsl(var(--primary));
    border-radius: 2px;
  }

  &__hint {
    font-size: 12px;
    color: hsl(var(--muted-foreground));
  }
}

/* 表达式代码块 */
.cron-code {
  display: inline-block;
  padding: 3px 10px;
  background: hsl(var(--primary) / 0.1);
  border-radius: 4px;
  color: hsl(var(--primary));
  font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
  font-size: 12px;
  font-weight: 500;
}

/* 格式横幅 */
.format-banner {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: hsl(var(--primary) / 0.06);
  border: 1px solid hsl(var(--primary) / 0.15);
  border-radius: 8px;
  margin-bottom: 16px;

  &__icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    font-size: 14px;
    font-weight: 700;
    font-style: italic;
    flex-shrink: 0;
  }

  &__content {
    display: flex;
    align-items: baseline;
    gap: 8px;
    flex-wrap: wrap;
  }

  &__label {
    font-size: 12px;
    color: hsl(var(--muted-foreground));
  }

  &__pattern {
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    font-size: 14px;
    color: hsl(var(--foreground));
    font-weight: 600;
    letter-spacing: 0.02em;
  }

  &__note {
    font-size: 11px;
    color: hsl(var(--muted-foreground));
    padding: 1px 6px;
    background: hsl(var(--accent));
    border-radius: 3px;
  }
}

/* 格式卡片网格 */
.format-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-bottom: 16px;
}

.format-card {
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  padding: 12px 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    border-color: hsl(var(--primary) / 0.3);
    box-shadow: 0 2px 8px hsl(var(--primary) / 0.08);
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
    padding-bottom: 8px;
    border-bottom: 1px solid hsl(var(--border));
  }

  &__name {
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--foreground));
  }

  &__range {
    font-size: 11px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    padding: 1px 8px;
    border-radius: 10px;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
  }

  &__body {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 12px;
  }

  &__support {
    color: hsl(var(--muted-foreground));
  }

  &__symbols {
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    color: hsl(var(--foreground));
    font-weight: 500;
  }
}

/* 符号说明 */
.symbol-legend {
  padding: 14px 16px;
  background: hsl(var(--accent) / 0.5);
  border-radius: 8px;

  &__title {
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    margin-bottom: 10px;
    letter-spacing: 0.04em;
  }

  &__grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 8px 20px;
  }

  &__item {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 12px;
  }

  &__symbol {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 22px;
    height: 22px;
    padding: 0 6px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 4px;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    color: hsl(var(--primary));
    font-weight: 600;
    flex-shrink: 0;
  }

  &__desc {
    color: hsl(var(--muted-foreground));
  }
}

/* 响应式 */
@media (max-width: 768px) {
  .format-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 576px) {
  .format-grid {
    grid-template-columns: 1fr;
  }

  .symbol-legend__grid {
    grid-template-columns: 1fr;
  }
}
</style>
