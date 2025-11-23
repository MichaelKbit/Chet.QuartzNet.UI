// 模拟defHttp函数
const defHttp = {
  get: async (_config?: any) => ({ data: {} }),
  post: async (_config?: any) => ({ data: {} }),
  put: async (_config?: any) => ({ data: {} }),
  delete: async (_config?: any) => ({ data: {} })
};
// 定义实际使用的接口
interface JobSaveParams {}
interface JobOperationParams {}

// 暂时注释掉未使用的接口
// interface SchedulerStatus {}
// interface JobListResponse {}
// interface JobLogResponse {}


// API基础路径
const prefix = '/api/quartz';

// 创建一个配置了超时和重试的http实例
const quartzHttp = defHttp;

// 获取调度器状态
export function getSchedulerStatus() {
  return quartzHttp.get({
    url: `${prefix}/GetSchedulerStatus`,
    timeout: 10000, // 10秒超时
    retryCount: 3,
    retryDelay: 500,
  });
}

// 切换调度器状态
export function toggleSchedulerStatus(params: {
  status: string;
}) {
  return quartzHttp.post({
    url: `${prefix}/ToggleSchedulerStatus`,
    params,
    timeout: 15000, // 15秒超时
  });
}

// 获取作业列表
export function getJobs(params: {
  page?: number;
  pageSize?: number;
  jobName?: string;
  jobGroup?: string;
  status?: string;
  sortField?: string;
  sortOrder?: string;
}) {
  // 标准化参数名，确保与后端兼容
  const normalizedParams = {
    page: params.page || 1,
    pageSize: params.pageSize || 10,
    jobName: params.jobName || '',
    jobGroup: params.jobGroup || '',
    status: params.status || '',
    sortField: params.sortField || 'jobName',
    sortOrder: params.sortOrder || 'asc',
  };

  return quartzHttp.get({
    url: `${prefix}/GetJobs`,
    params: normalizedParams,
    timeout: 10000,
  });
}

// 获取作业日志
export function getJobLogs(params: {
  page?: number;
  pageSize?: number;
  jobName?: string;
  jobGroup?: string;
  status?: string;
  startTime?: string;
  endTime?: string;
  sortField?: string;
  sortOrder?: string;
}) {
  // 标准化参数名，确保与后端兼容
  const normalizedParams = {
    page: params.page || 1,
    pageSize: params.pageSize || 10,
    jobName: params.jobName || '',
    jobGroup: params.jobGroup || '',
    status: params.status || '',
    startTime: params.startTime || '',
    endTime: params.endTime || '',
    sortField: params.sortField || 'createTime',
    sortOrder: params.sortOrder || 'desc',
  };

  return quartzHttp.get({
    url: `${prefix}/GetJobLogs`,
    params: normalizedParams,
    timeout: 10000,
  });
}

// 获取作业类名列表
export function getJobClasses() {
  return quartzHttp.get({
    url: `${prefix}/GetJobClasses`,
    timeout: 10000,
    retryCount: 3,
  });
}

// 添加作业
export function addJob(params: JobSaveParams) {
  return quartzHttp.post({
    url: `${prefix}/AddJob`,
    params,
    timeout: 10000,
  });
}

// 更新作业
export function updateJob(params: JobSaveParams) {
  return quartzHttp.post({
    url: `${prefix}/UpdateJob`,
    params,
    timeout: 10000,
  });
}

// 删除作业
export function deleteJob(params: JobOperationParams) {
  return quartzHttp.post({
    url: `${prefix}/DeleteJob`,
    params,
    timeout: 10000,
  });
}

// 暂停作业
export function pauseJob(params: JobOperationParams) {
  return quartzHttp.post({
    url: `${prefix}/PauseJob`,
    params,
    timeout: 10000,
  });
}

// 恢复作业
export function resumeJob(params: JobOperationParams) {
  return quartzHttp.post({
    url: `${prefix}/ResumeJob`,
    params,
    timeout: 10000,
  });
}

// 触发作业
export function triggerJob(params: JobOperationParams) {
  return quartzHttp.post({
    url: `${prefix}/TriggerJob`,
    params,
    timeout: 15000, // 触发作业可能需要更长时间
  });
}

// 通用错误处理函数
export function handleApiError(error: any, defaultMessage: string = '操作失败') {
  if (error.response) {
    // 服务器返回错误状态码
    const status = error.response.status;
    const data = error.response.data;

    if (status >= 500) {
      return '服务器内部错误，请稍后重试';
    } else if (status >= 400) {
      return data.message || data.error || defaultMessage;
    }
  } else if (error.request) {
    // 请求已发出但没有收到响应
    return '网络异常，请检查网络连接';
  }
  return defaultMessage;
}