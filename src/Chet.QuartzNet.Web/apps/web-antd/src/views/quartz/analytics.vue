<script setup lang="ts">
import { ref, shallowRef, onMounted } from 'vue';
import { Page } from '@vben/common-ui';
import { Card, Row, Col, Skeleton } from 'ant-design-vue';
import type { EChartsOption } from 'echarts';

// 导入Vben插件与组件
import type { EchartsUIType } from '@vben/plugins/echarts';
import { EchartsUI, useEcharts } from '@vben/plugins/echarts';

// 导入API和类型
import {
  getSchedulerStatus,
  getJobStats,
  getJobExecutionTrend,
  getJobExecutionTime,
} from '../../api/quartz/job';
import type {
  JobStats,
  JobExecutionTrend,
  JobExecutionTime,
  StatsQueryDto,
} from '../../api/quartz/job';

/**
 * 状态与数据初始化
 * 使用 shallowRef 优化性能，防止大型图表数据被过度代理
 */
const loading = ref(false);
const executionTrendChartRef = ref<EchartsUIType | null>(null);
const executionTimeChartRef = ref<EchartsUIType | null>(null);

const { renderEcharts: renderExecutionTrend } = useEcharts(executionTrendChartRef);
const { renderEcharts: renderExecutionTime } = useEcharts(executionTimeChartRef);

const statsOverview = ref<JobStats>({
  totalJobs: 0,
  enabledJobs: 0,
  disabledJobs: 0,
  executingJobs: 0,
  successCount: 0,
  failedCount: 0,
  pausedCount: 0,
  blockedCount: 0,
});

// 使用 shallowRef 存储数组数据
const jobExecutionTrend = shallowRef<JobExecutionTrend[]>([]);
const jobExecutionTimeData = shallowRef<JobExecutionTime[]>([]);

/**
 * 图表配置生成器 (抽离配置逻辑，保持 fetch 函数纯粹)
 */
const getExecutionTrendOption = (data: JobExecutionTrend[]): EChartsOption => {
  const hasData = data.length > 0;
  const colors = {
    success: { line: '#52c41a', area: 'rgba(82, 196, 26, 0.1)' },
    failed: { line: '#ff4d4f', area: 'rgba(255, 77, 79, 0.1)' },
    total: { line: '#1890ff', area: 'rgba(24, 144, 255, 0.05)' }
  };

  return {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      extraCssText: 'backdrop-filter: blur(4px); box-shadow: 0 4px 12px rgba(0,0,0,0.1);',
      formatter: (params: any) => {
        let html = `<div style="margin-bottom: 8px; font-weight: 500; color: #595959">${params[0].axisValue}</div>`;
        params.forEach((item: any) => {
          html += `
            <div style="display: flex; align-items: center; justify-content: space-between; min-width: 120px; margin-bottom: 4px;">
              <span style="font-size: 13px; color: #8c8c8c">
                <span style="display:inline-block; width: 8px; height: 8px; border-radius: 50%; background: ${item.color}; margin-right: 8px;"></span>
                ${item.seriesName}
              </span>
              <span style="font-weight: 600; color: #262626;">${item.value}</span>
            </div>`;
        });
        return html;
      }
    },
    legend: { icon: 'rect', itemWidth: 10, itemHeight: 4, right: 0, top: 0 },
    grid: { left: '1%', right: '2%', bottom: '5%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: hasData ? data.map(i => i.time) : ['无数据'],
      axisLine: { lineStyle: { color: '#f0f0f0' } },
      axisLabel: { color: '#8c8c8c' }
    },
    yAxis: { type: 'value', splitLine: { lineStyle: { color: '#f5f5f5' } } },
    series: [
      {
        name: '成功',
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map(i => i.successCount),
        itemStyle: { color: colors.success.line },
        areaStyle: {
          color: {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [{ offset: 0, color: colors.success.area }, { offset: 1, color: 'transparent' }]
          }
        }
      },
      {
        name: '失败',
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map(i => i.failedCount),
        itemStyle: { color: colors.failed.line },
        areaStyle: {
          color: {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [{ offset: 0, color: colors.failed.area }, { offset: 1, color: 'transparent' }]
          }
        }
      },
      {
        name: '总数',
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map(i => i.totalCount),
        itemStyle: { color: colors.total.line },
        lineStyle: { width: 2, type: 'dashed', opacity: 0.5 }
      }
    ]
  };
};

const getExecutionTimeOption = (data: JobExecutionTime[]): EChartsOption => {
  const xAxisData = data.length > 0 ? data.map(i => i.timeRange) : ['无数据'];
  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: '1%', right: '2%', bottom: '5%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      data: xAxisData,
      axisLabel: { color: '#8c8c8c', rotate: xAxisData.length > 6 ? 30 : 0 }
    },
    yAxis: { type: 'value', splitLine: { lineStyle: { type: 'dashed', color: '#f5f5f5' } } },
    series: [{
      name: '作业数量',
      type: 'bar',
      barWidth: 22,
      data: data.map(i => i.count),
      itemStyle: {
        borderRadius: [4, 4, 0, 0],
        color: (params: any) => {
          const ratio = params.dataIndex / (xAxisData.length - 1 || 1);
          const color = ratio > 0.7 ? '#ff4d4f' : ratio > 0.4 ? '#faad14' : '#1890ff';
          return {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [{ offset: 0, color: color }, { offset: 1, color: color + '99' }]
          };
        }
      }
    }]
  };
};

/**
 * 业务逻辑：获取并渲染数据
 */
const fetchData = async () => {
  loading.value = true;
  const query: StatsQueryDto = { timeRangeType: 'last30Days' };

  try {
    // 并行请求，提高加载速度
    const [statsRes, trendRes, timeRes, schedulerRes] = await Promise.all([
      getJobStats(query),
      getJobExecutionTrend(query),
      getJobExecutionTime(query),
      getSchedulerStatus()
    ]);

    // 更新基础统计 (优先使用 statsRes, schedulerRes 作为补充)
    if (statsRes.success) {
      statsOverview.value = statsRes.data;
    }
    if (schedulerRes.success) {
      statsOverview.value.executingJobs = schedulerRes.data.executingJobCount || 0;
      // 若总数为空则使用调度器数据
      if (!statsOverview.value.totalJobs) statsOverview.value.totalJobs = schedulerRes.data.jobCount || 0;
    }

    // 更新趋势图数据
    jobExecutionTrend.value = trendRes?.success ? trendRes.data : [];
    renderExecutionTrend(getExecutionTrendOption(jobExecutionTrend.value));

    // 更新耗时图数据
    jobExecutionTimeData.value = timeRes?.success ? timeRes.data : [];
    renderExecutionTime(getExecutionTimeOption(jobExecutionTimeData.value));

  } catch (error) {
    console.error('Data Fetch Error:', error);
  } finally {
    loading.value = false;
  }
};

onMounted(fetchData);
</script>

<template>
  <Page auto-content-height>
    <Row :gutter="[20, 20]">
      <Col v-for="item in [
        { label: '总作业数', val: statsOverview.totalJobs, icon: '📊' },
        { label: '启用作业数', val: statsOverview.enabledJobs, icon: '✅' },
        { label: '禁用作业数', val: statsOverview.disabledJobs, icon: '❌' },
        { label: '正在执行', val: statsOverview.executingJobs, icon: '⏳' }
      ]" :key="item.label" :xs="24" :sm="12" :lg="6">
        <Card class="statistic-card" :loading="loading">
          <div class="statistic-content">
            <div class="statistic-prefix">{{ item.icon }}</div>
            <div class="statistic-info">
              <div class="statistic-title">{{ item.label }}</div>
              <div class="statistic-value">{{ item.val }}</div>
            </div>
          </div>
        </Card>
      </Col>

      <Col :span="24">
        <Card title="近30天作业执行趋势" class="chart-card">
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTrendChartRef" style="height: 400px" />
          </Skeleton>
        </Card>
      </Col>

      <Col :span="24">
        <Card title="近30天作业执行耗时" class="chart-card">
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTimeChartRef" style="height: 400px" />
          </Skeleton>
        </Card>
      </Col>
    </Row>
  </Page>
</template>

<style scoped>
/* 核心样式精简 */
.statistic-card, .chart-card {
  border-radius: 10px;
}

.statistic-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.statistic-prefix { font-size: 26px; }

.statistic-title {
  font-size: 13px;
  color: var(--color-text-secondary, #8c8c8c);
  margin-bottom: 4px;
}

.statistic-value {
  font-size: 22px;
  font-weight: 700;
  color: var(--color-text-primary, #262626);
}

:deep(.ant-card-head) { border-bottom: none; padding: 0 20px; }
:deep(.ant-card-head-title) { font-size: 15px; font-weight: 600; }
</style>