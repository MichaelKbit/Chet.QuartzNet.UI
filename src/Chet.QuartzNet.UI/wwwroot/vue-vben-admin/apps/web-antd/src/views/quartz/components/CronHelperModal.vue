<template>
  <Modal title="Cron表达式助手" v-model:visible="visible" @ok="handleOk" @cancel="handleCancel" width="600px">
    <div class="cron-helper">
      <div class="cron-description">
        <h4>Cron表达式说明：</h4>
        <p>Cron表达式格式：秒 分 时 日 月 周 [年]</p>
        <ul>
          <li><strong>秒(0-59)</strong>: 允许值范围: 0-59, 允许特殊字符: , - * /</li>
          <li><strong>分(0-59)</strong>: 允许值范围: 0-59, 允许特殊字符: , - * /</li>
          <li><strong>时(0-23)</strong>: 允许值范围: 0-23, 允许特殊字符: , - * /</li>
          <li><strong>日(1-31)</strong>: 允许值范围: 1-31, 允许特殊字符: , - * ? / L W</li>
          <li><strong>月(1-12)</strong>: 允许值范围: 1-12, 允许特殊字符: , - * /</li>
          <li><strong>周(1-7)</strong>: 允许值范围: 1-7, 允许特殊字符: , - * ? / L #</li>
        </ul>
      </div>

      <div class="cron-examples">
        <h4>常用Cron表达式：</h4>
        <List :data-source="commonCronExpressions">
          <template #renderItem="{ item }">
            <div class="cron-item" @click="selectExpression(item.expression)">
              <div class="cron-expression">{{ item.expression }}</div>
              <div>{{ item.description }}</div>
            </div>
          </template>
        </List>
      </div>
    </div>
  </Modal>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';
import { Modal } from 'ant-design-vue';
import { List } from 'ant-design-vue';

interface CronExpression {
  expression: string;
  description: string;
}

export default defineComponent({
  name: 'CronHelperModal',
  props: {
    visible: {
      type: Boolean,
      default: false,
    },
  },
  emits: ['update:visible', 'select'],
  setup(props, { emit }) {
    // 常用Cron表达式
    const commonCronExpressions: CronExpression[] = [
      { expression: '0 0 0 * * ?', description: '每天凌晨0点执行' },
      { expression: '0 0 12 * * ?', description: '每天中午12点执行' },
      { expression: '0 0 18 * * ?', description: '每天下午6点执行' },
      { expression: '0 0 0 * * MON', description: '每周一凌晨0点执行' },
      { expression: '0 0 0 1 * ?', description: '每月1日凌晨0点执行' },
      { expression: '0 0 0 1 1 ?', description: '每年1月1日凌晨0点执行' },
      { expression: '0 0/1 * * * ?', description: '每小时执行一次' },
      { expression: '0 0/5 * * * ?', description: '每5分钟执行一次' },
      { expression: '0 0/30 * * * ?', description: '每30分钟执行一次' },
      { expression: '0 * * * * ?', description: '每分钟执行一次' },
      { expression: '*/10 * * * * ?', description: '每10秒执行一次' },
      { expression: '0 0 2,14 * * ?', description: '每天2点和14点执行' },
      { expression: '0 0 9-17 * * ?', description: '每天9点到17点每小时执行' },
      { expression: '0 0 0 ? * 1-5', description: '工作日凌晨0点执行' },
      { expression: '0 0 0 ? * 6-7', description: '周末凌晨0点执行' },
    ];

    // 选择表达式
    const selectExpression = (expression: string) => {
      emit('select', expression);
      handleOk();
    };

    // 确定按钮处理
    const handleOk = () => {
      emit('update:visible', false);
    };

    // 取消按钮处理
    const handleCancel = () => {
      emit('update:visible', false);
    };

    return {
      visible: computed(() => props.visible),
      commonCronExpressions,
      selectExpression,
      handleOk,
      handleCancel,
    };
  },
});
</script>

<style lang="less" scoped>
.cron-helper {
  max-height: 400px;
  overflow-y: auto;

  .cron-description {
    margin-bottom: 20px;

    h4 {
      margin-bottom: 10px;
      color: #262626;
    }

    p {
      margin-bottom: 10px;
      color: #262626;
    }

    ul {
      margin: 0;
      padding-left: 20px;

      li {
        margin-bottom: 5px;
        color: #595959;

        strong {
          color: #262626;
        }
      }
    }
  }

  .cron-examples {
    h4 {
      margin-bottom: 10px;
      color: #262626;
    }

    .cron-item {
      cursor: pointer;
      transition: all 0.3s;
      padding: 8px 0;
      border-bottom: 1px solid #f0f0f0;

      &:hover {
        background-color: #f5f5f5;
      }

      .cron-expression {
        font-family: monospace;
        font-weight: bold;
        color: #1890ff;
        margin-bottom: 4px;
      }
    }
  }
}
</style>