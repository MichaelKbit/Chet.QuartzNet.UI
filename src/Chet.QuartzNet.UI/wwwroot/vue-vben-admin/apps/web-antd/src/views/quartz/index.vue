<template>
  <div class="quartz-container">
    <PageHeader :ghost="false" title="Quartz任务管理" />
    
    <Card title="Quartz任务调度管理" size="small" class="mb-4">
      <Row :gutter="[16, 16]" align="middle">
        <Col xs={24} sm={12} md={8} lg={6}>
          <div class="scheduler-status flex items-center space-x-2">
            <span class="status-label text-base">调度器状态：</span>
            <a-badge 
              :status="schedulerStatus === 'STARTED' ? 'success' : 'error'" 
              :text="schedulerStatus === 'STARTED' ? '运行中' : '已停止'" 
            />
          </div>
        </Col>
        <Col xs={24} sm={12} md={16} lg={18} className="text-right">
          <Space>
            <a-button disabled>
              调度器操作
            </a-button>
          </Space>
        </Col>
      </Row>
    </Card>

    <Card class="main-card" :bordered="false">
      <Tabs v-model:activeKey="activeTab" type="card" size="large" @change="handleTabChange">
        <template #default>
          <div key="jobs" tab="作业管理">
            <JobManagement />
          </div>
          <div key="logs" tab="执行日志">
            <LogManagement />
          </div>
        </template>
      </Tabs>
    </Card>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';
import { Card } from 'ant-design-vue';
import { Tabs } from 'ant-design-vue';
import { Badge } from 'ant-design-vue';
import { PageHeader } from 'ant-design-vue';
import { Row, Col } from 'ant-design-vue';
import { Space } from 'ant-design-vue';
import { Button } from 'ant-design-vue';
import JobManagement from './components/JobManagement.vue';
import LogManagement from './components/LogManagement.vue';

export default defineComponent({
  name: 'QuartzManagement',
  components: {
    PageHeader,
    Card,
    Tabs,
    JobManagement,
    LogManagement,
  },
  setup() {
    // 暂时简化实现，移除对不存在API的依赖
    const activeTab = ref('jobs');
    const schedulerStatus = ref('STOPPED');
    // 移除深色模式检测，简化实现
    
    // 标签页切换
      const handleTabChange = (key: any) => {
        activeTab.value = String(key);
      };

    return {
        activeTab,
        schedulerStatus,
        handleTabChange
      };
  },
});
</script>

<style lang="less" scoped>
.quartz-container {
  padding: 20px;
}

.status-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
}

.scheduler-status {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  padding: 8px 0;
  
  .status-label {
    font-weight: 500;
    font-size: 14px;
  }
}

.main-card {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.mb-4 {
  margin-bottom: 16px;
}

// 响应式设计优化
@media (max-width: 576px) {
  .quartz-container {
    padding: 8px;
  }
}
</style>