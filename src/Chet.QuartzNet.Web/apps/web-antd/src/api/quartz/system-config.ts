import { requestClient } from '../request';

// API响应DTO
export interface ApiResponse<T> {
  /** 是否成功 */
  success: boolean;
  /** 消息 */
  message: string;
  /** 数据 */
  data?: T;
  /** 错误码 */
  errorCode?: string;
}

// 系统配置DTO
export interface SystemConfigDto {
  /** 服务名称 */
  serviceName: string;
  /** 环境标识 DEV/TEST/UAT/PROD */
  environment: string;
  /** 服务描述 */
  serviceDescription: string;
}

// 获取系统配置
export const getSystemConfig = async (): Promise<SystemConfigDto> => {
  const response = await requestClient.get('/api/quartz/GetSystemConfig');
  return response;
};

// 保存系统配置
export const saveSystemConfig = async (
  config: SystemConfigDto,
): Promise<ApiResponse<boolean>> => {
  const response = await requestClient.post('/api/quartz/SaveSystemConfig', config);
  return response;
};
