<script setup lang="ts">
import { ref, shallowRef, onMounted, computed } from 'vue';
import { Page } from '@vben/common-ui';
import { Card, Row, Col, Skeleton } from 'ant-design-vue';
import { CircleCheckBig, Package, RotateCw, Layers } from '@vben/icons';
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
import { useSystemConfig } from '../../composables/use-system-config';

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
 */
const getExecutionTrendOption = (data: JobExecutionTrend[]): EChartsOption => {
  const hasData = data.length > 0;
  // 语义色：success 绿 / failed 红 / total 蓝
  const colors = {
    success: '#52c41a',
    failed: '#ff4d4f',
    total: '#1890ff',
  };

  return {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      borderWidth: 0,
      padding: [10, 14],
      textStyle: { fontSize: 12, color: '#595959' },
      extraCssText: 'backdrop-filter: blur(8px); box-shadow: 0 6px 16px rgba(0,0,0,0.08);',
      formatter: (params: any) => {
        let html = `<div style="margin-bottom: 8px; font-weight: 600; color: #262626; font-size: 13px;">${params[0].axisValue}</div>`;
        params.forEach((item: any) => {
          html += `
            <div style="display: flex; align-items: center; justify-content: space-between; min-width: 140px; line-height: 22px;">
              <span style="font-size: 12px; color: #8c8c8c">
                <span style="display:inline-block; width: 8px; height: 8px; border-radius: 50%; background: ${item.color}; margin-right: 8px; vertical-align: middle;"></span>
                ${item.seriesName}
              </span>
              <span style="font-weight: 600; color: #262626; font-variant-numeric: tabular-nums;">${item.value}</span>
            </div>`;
        });
        return html;
      },
    },
    legend: {
      icon: 'circle',
      itemWidth: 8,
      itemHeight: 8,
      right: 0,
      top: 0,
      textStyle: { color: '#8c8c8c', fontSize: 12 },
    },
    grid: { left: '1%', right: '2%', bottom: '3%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: hasData ? data.map((i) => i.time) : [$t('page.quartz.analyticsPage.noData')],
      axisLine: { lineStyle: { color: '#f0f0f0' } },
      axisTick: { show: false },
      axisLabel: { color: '#8c8c8c', fontSize: 12 },
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { color: '#f5f5f5', type: 'dashed' } },
      axisLabel: { color: '#8c8c8c', fontSize: 12 },
    },
    series: [
      {
        name: $t('page.quartz.analyticsPage.success'),
        type: 'line',
        smooth: 0.4,
        showSymbol: false,
        data: data.map((i) => i.successCount),
        itemStyle: { color: colors.success },
        lineStyle: { width: 2.5 },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: 'rgba(82, 196, 26, 0.18)' },
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
        itemStyle: { color: colors.failed },
        lineStyle: { width: 2.5 },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: 'rgba(255, 77, 79, 0.18)' },
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
        itemStyle: { color: colors.total },
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
  const isDark = document.documentElement.classList.contains('dark');
  const labelColor = isDark ? 'rgba(255,255,255,0.45)' : '#8c8c8c';
  const lineColor = isDark ? '#303030' : '#f0f0f0';
  const splitColor = isDark ? '#303030' : '#f5f5f5';

  return {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      borderWidth: 0,
      padding: [10, 14],
      textStyle: { fontSize: 12 },
      extraCssText: 'backdrop-filter: blur(8px); box-shadow: 0 6px 16px rgba(0,0,0,0.08);',
    },
    grid: { left: '1%', right: '2%', bottom: '3%', top: '15%', containLabel: true },
    xAxis: {
      type: 'category',
      data: xAxisData,
      axisLabel: {
        color: labelColor,
        fontSize: 12,
        rotate: xAxisData.length > 6 ? 30 : 0,
      },
      axisLine: { lineStyle: { color: lineColor } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { type: 'dashed', color: splitColor } },
      axisLabel: { color: labelColor, fontSize: 12 },
    },
    series: [
      {
        name: $t('page.quartz.analyticsPage.jobCount'),
        type: 'bar',
        barWidth: 24,
        data: data.map((i) => i.count),
        itemStyle: {
          borderRadius: [6, 6, 0, 0],
          color: (params: any) => {
            // 按耗时档位映射语义色：极速蓝 → 正常绿 → 偏慢黄 → 极慢红
            const ratio = params.dataIndex / (xAxisData.length - 1 || 1);
            let color;
            if (ratio < 0.25) {
              color = '#1890ff';
            } else if (ratio < 0.5) {
              color = '#52c41a';
            } else if (ratio < 0.75) {
              color = '#faad14';
            } else {
              color = '#ff4d4f';
            }
            return {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [
                { offset: 0, color },
                { offset: 1, color: color + 'AA' },
              ],
            };
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

/**
 * 系统配置：服务标识横幅
 * 使用全局共享状态，标题同步由 bootstrap.ts 统一处理
 */
const { systemConfig, loadSystemConfig } = useSystemConfig();

// 环境标签文本映射
const environmentTagMap: Record<string, () => string> = {
  DEV: () => $t('page.quartz.systemConfigPage.envDEV'),
  TEST: () => $t('page.quartz.systemConfigPage.envTEST'),
  UAT: () => $t('page.quartz.systemConfigPage.envUAT'),
  PROD: () => $t('page.quartz.systemConfigPage.envPROD'),
};

const environmentTag = computed(
  () => environmentTagMap[systemConfig.value.environment] ?? environmentTagMap.DEV!,
);

const hasServiceName = computed(() => !!systemConfig.value.serviceName);

onMounted(() => {
  loadSystemConfig();
  fetchData();
});
</script>

<template>
  <Page auto-content-height header-class="page-header-compact">
    <!-- 标题行：服务标识胶囊 = 主题色条 + 服务名 + 环境标签 -->
    <template #title>
      <div class="page-title-row">
        <div v-if="hasServiceName" class="service-chip">
          <span class="service-bar"></span>
          <span class="service-name">{{ systemConfig.serviceName }}</span>
          <span class="env-pill" :data-env="systemConfig.environment">
            <i class="env-dot"></i>{{ environmentTag() }}
          </span>
        </div>
      </div>
    </template>
    <!-- 描述：服务描述（若有） -->
    <template #description>
      <p v-if="hasServiceName && systemConfig.serviceDescription" class="service-desc">
        {{ systemConfig.serviceDescription }}
      </p>
    </template>
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
              <RotateCw class="stat-icon__svg" />
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
              <CircleCheckBig class="stat-icon__svg" />
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
        <Card class="chart-card" :bordered="false">
          <template #title>
            <div class="chart-title">
              <span class="chart-title__bar"></span>
              <span class="chart-title__text">{{ $t('page.quartz.analyticsPage.executionTrend') }}</span>
            </div>
          </template>
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTrendChartRef" style="height: 380px" />
          </Skeleton>
        </Card>
      </Col>

      <Col :span="24">
        <Card class="chart-card" :bordered="false">
          <template #title>
            <div class="chart-title">
              <span class="chart-title__bar"></span>
              <span class="chart-title__text">{{ $t('page.quartz.analyticsPage.executionTime') }}</span>
            </div>
          </template>
          <Skeleton :loading="loading" active :paragraph="{ rows: 8 }">
            <EchartsUI ref="executionTimeChartRef" style="height: 380px" />
          </Skeleton>
        </Card>
      </Col>
    </Row>
  </Page>
</template>

<style scoped>
/* ====== Page header 紧凑化 ====== */
:deep(.page-header-compact) {
  padding-top: 12px !important;
  padding-bottom: 12px !important;
}

/* ====== 标题行：服务标识区（大气版） ====== */
.page-title-row {
  display: flex;
  align-items: center;
}

/* 服务标识容器：直接铺开，不套小胶囊，更有体量感 */
.service-chip {
  display: inline-flex;
  align-items: center;
  gap: 14px;
}

/* 主题色条：加粗加高 + 渐变 + 微光，作为视觉锚点 */
.service-bar {
  flex-shrink: 0;
  width: 4px;
  height: 26px;
  background: linear-gradient(180deg,
      hsl(var(--primary)),
      hsl(var(--primary) / 0.55));
  border-radius: 3px;
  box-shadow: 0 0 8px hsl(var(--primary) / 0.35);
}

/* 服务名：大字号作为视觉主体 */
.service-name {
  font-size: 20px;
  font-weight: 600;
  color: hsl(var(--foreground));
  line-height: 1.0;
  letter-spacing: 0.01em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 360px;
}

/* 环境标签：稍大胶囊 + 语义色圆点 */
.env-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 3px 12px;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.6;
  border-radius: 999px;
  border: 1px solid transparent;
  white-space: nowrap;
}

.env-dot {
  display: inline-block;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}

/* 环境语义色映射 */
.env-pill[data-env='DEV'] {
  color: hsl(var(--foreground));
  background: hsl(var(--muted-foreground) / 0.08);
  border-color: hsl(var(--muted-foreground) / 0.2);
}

.env-pill[data-env='DEV'] .env-dot {
  background: hsl(var(--muted-foreground));
}

.env-pill[data-env='TEST'] {
  color: hsl(212 100% 45%);
  background: hsl(212 100% 45% / 0.08);
  border-color: hsl(212 100% 45% / 0.2);
}

.env-pill[data-env='TEST'] .env-dot {
  background: hsl(212 100% 45%);
}

.env-pill[data-env='UAT'] {
  color: hsl(32 95% 44%);
  background: hsl(32 95% 54% / 0.08);
  border-color: hsl(32 95% 54% / 0.2);
}

.env-pill[data-env='UAT'] .env-dot {
  background: hsl(32 95% 54%);
}

.env-pill[data-env='PROD'] {
  color: hsl(0 84% 50%);
  background: hsl(0 84% 50% / 0.08);
  border-color: hsl(0 84% 50% / 0.2);
}

.env-pill[data-env='PROD'] .env-dot {
  background: hsl(0 84% 50%);
}

.service-desc {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  line-height: 1.0;
  margin-top: 10px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ====== KPI 卡片：保留质感，token 统一配色 ====== */
.stat-card {
  border-radius: 10px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 1px 2px hsl(var(--foreground) / 0.04);
  overflow: hidden;
  min-height: 152px;
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

/* ====== 图表卡片 ====== */
.chart-card {
  border-radius: 10px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 1px 2px hsl(var(--foreground) / 0.04);
}

:deep(.chart-card .ant-card-head) {
  border-bottom: 1px solid hsl(var(--border));
  min-height: auto;
  padding: 0 20px;
}

:deep(.chart-card .ant-card-head-title) {
  padding: 14px 0;
}

:deep(.chart-card .ant-card-body) {
  padding: 16px 20px 20px;
}

.chart-title {
  display: flex;
  align-items: center;
  gap: 10px;
}

.chart-title__bar {
  width: 3px;
  height: 16px;
  background: hsl(var(--primary));
  border-radius: 2px;
}

.chart-title__text {
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
}
</style>
