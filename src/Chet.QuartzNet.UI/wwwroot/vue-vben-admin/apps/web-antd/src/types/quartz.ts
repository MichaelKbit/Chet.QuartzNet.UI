// Quartz任务相关类型定义

// 调度器状态
export interface SchedulerStatus {
  status: string; // Started, Stopped
  runningSince?: string;
}

// 作业信息
export interface JobInfo {
  jobName: string;
  jobGroup: string;
  cronExpression: string;
  jobClassName: string;
  jobData?: string;
  description?: string;
  status: number; // 0: 正常, 1: 暂停, 2: 完成, 3: 错误, 4: 阻塞
}

// 作业列表响应
export interface JobListResponse {
  items: JobInfo[];
  total: number;
  page: number;
  pageSize: number;
}

// 作业日志
export interface JobLog {
  id: string;
  jobName: string;
  jobGroup: string;
  startTime: string;
  endTime?: string;
  status: number; // 0: 运行中, 1: 成功, 2: 失败
  errorMsg?: string;
}

// 日志列表响应
export interface JobLogResponse {
  items: JobLog[];
  total: number;
  page: number;
  pageSize: number;
}

// 作业操作参数
export interface JobOperationParams {
  jobName: string;
  jobGroup: string;
}

// 作业保存参数
export interface JobSaveParams {
  jobName: string;
  jobGroup: string;
  cronExpression: string;
  jobClassName: string;
  jobData?: string;
  description?: string;
}