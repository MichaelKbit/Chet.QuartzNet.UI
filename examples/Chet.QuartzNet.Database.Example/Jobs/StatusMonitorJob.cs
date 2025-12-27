using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 状态监控作业
    /// </summary>
    [QuartzJob("StatusMonitorJob", "MONITOR", "0 0/3 * * * ?", Description = "状态监控作业，每3分钟执行一次，演示系统状态监控")]
    public class StatusMonitorJob : IJob
    {
        private readonly ILogger<StatusMonitorJob> _logger;

        public StatusMonitorJob(ILogger<StatusMonitorJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("状态监控作业开始执行: {Time}", DateTime.Now);
            
            try
            {
                // 模拟系统状态检查
                var cpuUsage = new Random().Next(10, 90);
                var memoryUsage = new Random().Next(20, 85);
                var diskUsage = new Random().Next(30, 80);
                
                _logger.LogInformation("CPU使用率: {CpuUsage}%", cpuUsage);
                _logger.LogInformation("内存使用率: {MemoryUsage}%", memoryUsage);
                _logger.LogInformation("磁盘使用率: {DiskUsage}%", diskUsage);
                
                // 模拟异常情况
                if (cpuUsage > 80 || memoryUsage > 80 || diskUsage > 80)
                {
                    _logger.LogWarning("系统资源使用率过高，需要关注");
                }
                
                await Task.Delay(1800);
                _logger.LogInformation("状态监控作业执行完成: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "状态监控作业执行失败: {Time}", DateTime.Now);
                throw;
            }
        }
    }
}