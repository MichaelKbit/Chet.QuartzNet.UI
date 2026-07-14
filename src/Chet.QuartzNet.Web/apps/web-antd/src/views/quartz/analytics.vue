<script setup lang="ts">
import { ref, shallowRef, onMounted, computed } from 'vue';
import { Page } from '@vben/common-ui';
import { Card, Row, Col, Skeleton } from 'ant-design-vue';
import { Package, Zap, Clock, Layers } from '@vben/icons';
import type { EChartsOption } from 'echarts';

// 导入Vben插件与组件
import type { EchartsUIType } from '@vben/plugins/echarts';
import { EchartsUI, useEcharts } from '@vben/plugins/echarts';

// 导入i18n
import { $t } from '#/locales';

// 导入API和类型
import {
  getSchedulerStatus,
  getJobStats,
  getJobExecutionTrend,
  getJobExecutionTime,
  getJobStatusDistribution,
  getJobTypeDistribution,
} from '../../api/quartz/job';
import type {
  JobStats,
  JobExecutionTrend,
  JobExecutionTime,
  StatsQueryDto,
  JobStatusDistribution,
  JobTypeDistribution,
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
  totalExecutions: 0,
  successCount: 0,
  failedCount: 0,
});

// 使用 shallowRef 存储数组数据
const jobExecutionTrend = shallowRef<JobExecutionTrend[]>([]);
const jobExecutionTimeData = shallowRef<JobExecutionTime[]>([]);
const jobStatusDistribution = shallowRef<JobStatusDistribution[]>([]);
const jobTypeDistribution = shallowRef<JobTypeDistribution[]>([]);

// 派生数据：KPI 副指标集中计算，避免模板中重复 find
const normalCount = computed(
  () => jobStatusDistribution.value.find((d) => d.status === 'Normal')?.count || 0,
);
const pausedCount = computed(
  () => jobStatusDistribution.value.find((d) => d.status === 'Paused')?.count || 0,
);
const normalPercentage = computed(
  () => jobStatusDistribution.value.find((d) => d.status === 'Normal')?.percentage || 0,
);
const dllCount = computed(
  () => jobTypeDistribution.value.find((d) => d.type === 'DLL')?.count || 0,
);
const apiCount = computed(
  () => jobTypeDistribution.value.find((d) => d.type === 'API')?.count || 0,
);
const dllPercentage = computed(
  () => jobTypeDistribution.value.find((d) => d.type === 'DLL')?.percentage || 0,
);
const apiPercentage = computed(
  () => jobTypeDistribution.value.find((d) => d.type === 'API')?.percentage || 0,
);
const enabledRatio = computed(() =>
  (statsOverview.value.enabledJobs / (statsOverview.value.totalJobs || 1)) * 100,
);
const successRate = computed(() =>
  (
    (statsOverview.value.successCount / (statsOverview.value.totalExecutions || 1)) *
    100
  ).toFixed(1),
);
const successRatio = computed(() =>
  (statsOverview.value.successCount / (statsOverview.value.totalExecutions || 1)) * 100,
);

/**
 * 图表配置生成器 (抽离配置逻辑，保持 fetch 函数纯粹)
 * 配色统一使用 vben CSS 变量，自动适配暗色主题
 */
const getCssVarRaw = (name: string) =>
  getComputedStyle(document.documentElement).getPropertyValue(name).trim();

const getChartColors = () => {
  const raw = {
    foreground: getCssVarRaw('--foreground'),
    muted: getCssVarRaw('--muted-foreground'),
    border: getCssVarRaw('--border'),
    accent: getCssVarRaw('--accent'),
    primary: getCssVarRaw('--primary'),
    success: getCssVarRaw('--success'),
    destructive: getCssVarRaw('--destructive'),
  };
  const hsl = (v: string) => `hsl(${v})`;
  const hsla = (v: string, a: number) => `hsl(${v} / ${a})`;
  return {
    foreground: hsl(raw.foreground),
    muted: hsl(raw.muted),
    border: hsl(raw.border),
    accent: hsl(raw.accent),
    primary: hsl(raw.primary),
    success: hsl(raw.success),
    destructive: hsl(raw.destructive),
    successAlpha: hsla(raw.success, 0.18),
    destructiveAlpha: hsla(raw.destructive, 0.18),
    primaryAlpha: hsla(raw.primary, 0.4),
  };
};

// 统一 tooltip 模板
const buildTooltipFormatter = (params: any, colors: ReturnType<typeof getChartColors>) => {
  let html = `<div style="margin-bottom: 8px; font-weight: 600; color: ${colors.foreground}; font-size: 13px;">${params[0].axisValue}</div>`;
  params.forEach((item: any) => {
    html += `
      <div style="display: flex; align-items: center; justify-content: space-between; min-width: 140px; line-height: 22px;">
        <span style="font-size: 12px; color: ${colors.muted}">
          <span style="display:inline-block; width: 8px; height: 8px; border-radius: 50%; background: ${item.color}; margin-right: 8px; vertical-align: middle;"></span>
          ${item.seriesName}
        </span>
        <span style="font-weight: 600; color: ${colors.foreground}; font-variant-numeric: tabular-nums;">${item.value}</span>
      </div>`;
  });
  return html;
};

const getExecutionTrendOption = (data: JobExecutionTrend[]): EChartsOption => {
  const hasData = data.length > 0;
  const c = getChartColors();

  return {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      borderWidth: 0,
      padding: [10, 14],
      backgroundColor: c.accent,
      textStyle: { fontSize: 12, color: c.foreground },
      extraCssText: 'backdrop-filter: blur(8px); box-shadow: 0 6px 16px rgba(0,0,0,0.08);',
      formatter: (params: any) => buildTooltipFormatter(params, c),
    },
    legend: {
      icon: 'circle',
      itemWidth: 8,
      itemHeight: 8,
      right: 0,
      top: 0,
      textStyle: { color: c.muted, fontSize: 12 },
    },
    grid: { left: '1%', right: '2%', bottom: '3%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: hasData ? data.map((i) => i.time) : [$t('page.quartz.analyticsPage.noData')],
      axisLine: { lineStyle: { color: c.border } },
      axisTick: { show: false },
      axisLabel: { color: c.muted, fontSize: 12 },
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { color: c.border, type: 'dashed' } },
      axisLabel: { color: c.muted, fontSize: 12 },
    },
    series: [
      {
        name: $t('page.quartz.analyticsPage.success'),
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map((i) => i.successCount),
        itemStyle: { color: c.success },
        lineStyle: { width: 2.5 },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: c.successAlpha },
              { offset: 1, color: 'transparent' },
            ],
          },
        },
      },
      {
        name: $t('page.quartz.analyticsPage.failed'),
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map((i) => i.failedCount),
        itemStyle: { color: c.destructive },
        lineStyle: { width: 2.5 },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: c.destructiveAlpha },
              { offset: 1, color: 'transparent' },
            ],
          },
        },
      },
      {
        name: $t('page.quartz.analyticsPage.total'),
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map((i) => i.totalCount),
        itemStyle: { color: c.primary },
        lineStyle: { width: 2, type: 'dashed', opacity: 0.55 },
      },
    ],
  };
};

const getExecutionTimeOption = (data: JobExecutionTime[]): EChartsOption => {
  const xAxisData =
    data.length > 0
      ? data.map((i) => i.timeRange)
      : [$t('page.quartz.analyticsPage.noData')];
  const c = getChartColors();

  return {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      borderWidth: 0,
      padding: [10, 14],
      backgroundColor: c.accent,
      textStyle: { fontSize: 12, color: c.foreground },
      extraCssText: 'backdrop-filter: blur(8px); box-shadow: 0 6px 16px rgba(0,0,0,0.08);',
      formatter: (params: any) => buildTooltipFormatter(params, c),
    },
    grid: { left: '1%', right: '2%', bottom: '3%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      data: xAxisData,
      axisLabel: {
        color: c.muted,
        fontSize: 12,
        rotate: xAxisData.length > 6 ? 30 : 0,
      },
      axisLine: { lineStyle: { color: c.border } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { type: 'dashed', color: c.border } },
      axisLabel: { color: c.muted, fontSize: 12 },
    },
    series: [
      {
        name: $t('page.quartz.analyticsPage.jobCount'),
        type: 'bar',
        barWidth: 24,
        data: data.map((i) => i.count),
        itemStyle: {
          borderRadius: [6, 6, 0, 0],
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: c.primary },
              { offset: 1, color: c.primaryAlpha },
            ],
          },
        },
      },
    ],
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
    const [
      statsRes,
      trendRes,
      timeRes,
      schedulerRes,
      statusDistributionRes,
      typeDistributionRes,
    ] = await Promise.all([
      getJobStats(query),
      getJobExecutionTrend(query),
      getJobExecutionTime(query),
      getSchedulerStatus(),
      getJobStatusDistribution(query),
      getJobTypeDistribution(query),
    ]);

    // 更新基础统计 (优先使用 statsRes, schedulerRes 作为补充)
    if (statsRes.success) {
      statsOverview.value = statsRes.data;
    }
    if (schedulerRes.success) {
      // 若总数为空则使用调度器数据
      if (!statsOverview.value.totalJobs)
        statsOverview.value.totalJobs = schedulerRes.data.jobCount || 0;
    }

    // 更新趋势图数据
    jobExecutionTrend.value = trendRes?.success ? trendRes.data : [];
    renderExecutionTrend(getExecutionTrendOption(jobExecutionTrend.value));

    // 更新耗时图数据
    jobExecutionTimeData.value = timeRes?.success ? timeRes.data : [];
    renderExecutionTime(getExecutionTimeOption(jobExecutionTimeData.value));

    // 更新作业状态分布数据
    jobStatusDistribution.value = statusDistributionRes?.success
      ? statusDistributionRes.data
      : [];

    // 更新作业类型分布数据
    jobTypeDistribution.value = typeDistributionRes?.success
      ? typeDistributionRes.data
      : [];
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
    <!-- KPI 概览：保留 Card 质感 + 图标视觉，用 vben token 统一配色 -->
    <Row :gutter="[16, 16]">
      <Col :xs="24" :sm="12" :lg="6">
        <Card class="stat-card" :loading="loading" :bordered="false">
          <div class="stat-content">
            <div class="stat-main">
              <span class="stat-title">{{ $t('page.quartz.analyticsPage.totalJobs') }}</span>
              <span class="stat-number">
                {{ statsOverview.totalJobs }}
                <small class="stat-unit">{{ $t('page.quartz.analyticsPage.unit') }}</small>
              </span>
            </div>
            <div class="stat-icon stat-icon--blue">
              <Package class="stat-icon__svg" />
            </div>
          </div>
          <div class="stat-sub">
            <div class="stat-sub__label">
              <span class="sub-label">{{ $t('page.quartz.analyticsPage.enabledDisabled') }}</span>
              <span class="sub-value">
                <i class="dot dot--success"></i>{{ statsOverview.enabledJobs }}
                <i class="dot dot--muted"></i>{{ statsOverview.disabledJobs }}
              </span>
            </div>
            <div class="mini-bar">
              <div class="mini-bar__fill mini-bar__fill--blue" :style="{ width: enabledRatio + '%' }"></div>
            </div>
          </div>
        </Card>
      </Col>

      <Col :xs="24" :sm="12" :lg="6">
        <Card class="stat-card" :loading="loading" :bordered="false">
          <div class="stat-content">
            <div class="stat-main">
              <span class="stat-title">{{ $t('page.quartz.analyticsPage.totalExecutions') }}</span>
              <span class="stat-number">
                {{ statsOverview.totalExecutions }}
                <small class="stat-unit">{{ $t('page.quartz.analyticsPage.times') }}</small>
              </span>
            </div>
            <div class="stat-icon stat-icon--green">
              <Zap class="stat-icon__svg" />
            </div>
          </div>
          <div class="stat-sub">
            <div class="stat-sub__label">
              <span class="sub-label">{{ $t('page.quartz.analyticsPage.successRate') }}</span>
              <span class="sub-value sub-value--success">{{ successRate }}%</span>
            </div>
            <div class="mini-bar">
              <div class="mini-bar__fill mini-bar__fill--green" :style="{ width: successRatio + '%' }"></div>
            </div>
          </div>
        </Card>
      </Col>

      <Col :xs="24" :sm="12" :lg="6">
        <Card class="stat-card" :loading="loading" :bordered="false">
          <div class="stat-content">
            <div class="stat-main">
              <span class="stat-title">{{ $t('page.quartz.analyticsPage.normalRunning') }}</span>
              <span class="stat-number">
                {{ normalCount }}
                <small class="stat-unit">{{ $t('page.quartz.analyticsPage.unit') }}</small>
              </span>
            </div>
            <div class="stat-icon stat-icon--orange">
              <Clock class="stat-icon__svg" />
            </div>
          </div>
          <div class="stat-sub">
            <div class="stat-sub__label">
              <span class="sub-label">{{ $t('page.quartz.analyticsPage.normalPaused') }}</span>
              <span class="sub-value">{{ normalCount }} / {{ pausedCount }}</span>
            </div>
            <div class="mini-bar">
              <div class="mini-bar__fill mini-bar__fill--orange" :style="{ width: normalPercentage + '%' }"></div>
            </div>
          </div>
        </Card>
      </Col>

      <Col :xs="24" :sm="12" :lg="6">
        <Card class="stat-card" :loading="loading" :bordered="false">
          <div class="stat-content">
            <div class="stat-main">
              <span class="stat-title">{{ $t('page.quartz.analyticsPage.jobTypeDistribution') }}</span>
              <div class="dual-numbers">
                <span class="dual-item dual-item--dll">
                  <small>DLL</small>
                  <b>{{ dllCount }}</b>
                </span>
                <span class="dual-item dual-item--api">
                  <small>API</small>
                  <b>{{ apiCount }}</b>
                </span>
              </div>
            </div>
            <div class="stat-icon stat-icon--purple">
              <Layers class="stat-icon__svg" />
            </div>
          </div>
          <div class="stat-sub">
            <div class="stat-sub__label">
              <span class="sub-label">DLL {{ dllPercentage.toFixed(0) }}%</span>
              <span class="sub-value">API {{ apiPercentage.toFixed(0) }}%</span>
            </div>
            <div class="mini-bar mini-bar--dual">
              <div class="mini-bar__fill mini-bar__fill--purple" :style="{ width: dllPercentage + '%' }"></div>
              <div class="mini-bar__fill mini-bar__fill--cyan" :style="{ width: apiPercentage + '%' }"></div>
            </div>
          </div>
        </Card>
      </Col>

      <Col :span="24">
        <section class="chart-section">
          <div class="chart-header">
            <span class="chart-title">{{ $t('page.quartz.analyticsPage.executionTrend') }}</span>
          </div>
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTrendChartRef" style="height: 380px" />
          </Skeleton>
        </section>
      </Col>

      <Col :span="24">
        <section class="chart-section">
          <div class="chart-header">
            <span class="chart-title">{{ $t('page.quartz.analyticsPage.executionTime') }}</span>
          </div>
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTimeChartRef" style="height: 380px" />
          </Skeleton>
        </section>
      </Col>
    </Row>
  </Page>
</template>

<style scoped>
/* ====== KPI 卡片：保留质感，token 统一配色 ====== */
.stat-card {
  border-radius: 10px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 1px 2px hsl(var(--foreground) / 0.04);
  transition: box-shadow 0.25s ease, transform 0.25s ease;
  overflow: hidden;
  min-height: 152px;
}

.stat-card:hover {
  box-shadow: 0 6px 20px hsl(var(--foreground) / 0.08);
  transform: translateY(-1px);
}

:deep(.stat-card .ant-card-body) {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.stat-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 14px;
}

.stat-main {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
}

.stat-title {
  color: hsl(var(--muted-foreground));
  font-size: 13px;
  margin-bottom: 8px;
  letter-spacing: 0.01em;
}

.stat-number {
  font-size: 30px;
  font-weight: 700;
  color: hsl(var(--foreground));
  line-height: 1.1;
  letter-spacing: -0.02em;
  font-variant-numeric: tabular-nums;
}

.stat-unit {
  font-size: 12px;
  font-weight: 400;
  color: hsl(var(--muted-foreground));
  margin-left: 6px;
}

/* 图标：语义色圆角背景 + lucide 图标 */
.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  position: relative;
}

.stat-icon__svg {
  width: 22px;
  height: 22px;
}

.stat-icon--blue {
  background: hsl(212 100% 45% / 0.12);
  color: hsl(212 100% 45%);
  box-shadow: 0 4px 12px hsl(212 100% 45% / 0.15);
}

.stat-icon--green {
  background: hsl(144 57% 58% / 0.15);
  color: hsl(144 57% 45%);
  box-shadow: 0 4px 12px hsl(144 57% 58% / 0.15);
}

.stat-icon--orange {
  background: hsl(42 84% 61% / 0.15);
  color: hsl(42 84% 50%);
  box-shadow: 0 4px 12px hsl(42 84% 61% / 0.15);
}

.stat-icon--purple {
  background: hsl(262 83% 58% / 0.15);
  color: hsl(262 83% 55%);
  box-shadow: 0 4px 12px hsl(262 83% 58% / 0.15);
}

/* 双数值（DLL / API） */
.dual-numbers {
  display: flex;
  gap: 18px;
  align-items: baseline;
}

.dual-item {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
}

.dual-item small {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  letter-spacing: 0.04em;
  font-weight: 500;
}

.dual-item b {
  font-size: 26px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.02em;
}

.dual-item--dll b {
  color: hsl(262 83% 58%);
}

.dual-item--api b {
  color: hsl(187 100% 42%);
}

/* ====== 副指标 + mini bar ====== */
.stat-sub {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: auto;
}

.stat-sub__label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12px;
}

.sub-label {
  color: hsl(var(--muted-foreground));
}

.sub-value {
  font-weight: 600;
  color: hsl(var(--foreground));
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-variant-numeric: tabular-nums;
}

.sub-value--success {
  color: hsl(var(--success));
}

.dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  margin-right: 2px;
}

.dot--success {
  background: hsl(var(--success));
}

.dot--muted {
  background: hsl(var(--muted-foreground) / 0.5);
  margin-left: 6px;
}

.mini-bar {
  height: 6px;
  background: hsl(var(--accent));
  border-radius: 3px;
  overflow: hidden;
}

.mini-bar__fill {
  height: 100%;
  border-radius: 3px;
  transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}

.mini-bar__fill--blue {
  background: linear-gradient(90deg, #1890ff, #40a9ff);
}

.mini-bar__fill--green {
  background: linear-gradient(90deg, #52c41a, #73d13d);
}

.mini-bar__fill--orange {
  background: linear-gradient(90deg, #faad14, #ffc53d);
}

.mini-bar__fill--purple {
  background: linear-gradient(90deg, #722ed1, #9254de);
}

.mini-bar__fill--cyan {
  background: linear-gradient(90deg, #13c2c2, #36cfc9);
}

.mini-bar--dual {
  display: flex;
}

.mini-bar--dual .mini-bar__fill {
  min-width: 0;
}

/* ====== 图表区域：去 Card 化，用 section 分隔 ====== */
.chart-section {
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 10px;
  padding: 18px 20px 20px;
}

.chart-header {
  margin-bottom: 14px;
}

.chart-title {
  font-size: 15px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

/* 响应式 */
@media (max-width: 576px) {
  .stat-number {
    font-size: 26px;
  }

  .dual-item b {
    font-size: 22px;
  }

  .chart-section {
    padding: 14px 14px 16px;
  }
}
</style>
