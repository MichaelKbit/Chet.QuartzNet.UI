using Chet.QuartzNet.Core.Attributes;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 状态监控作业
    /// </summary>
    [QuartzJob("StatusMonitorJob", "MONITORING", "0 0/1 * * * ?", Description = "状态监控作业，每分钟执行一次")]
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
                // 模拟系统状态监控
                var systemStatus = new Dictionary<string, object>
                {
                    { "CPU使用率", $"{new Random().Next(10, 80)}%" },
                    { "内存使用率", $"{new Random().Next(20, 70)}%" },
                    { "磁盘空间", $"{new Random().Next(40, 90)}%" },
                    { "网络状态", "正常" },
                    { "服务状态", "运行中" }
                };

                // 记录系统状态
                foreach (var status in systemStatus)
                {
                    _logger.LogInformation("系统状态 - {Key}: {Value}", status.Key, status.Value);
                    await Task.Delay(100);
                }

                // 模拟警报检查
                if (new Random().Next(10) > 8)
                {
                    _logger.LogWarning("状态监控作业发现异常: {Time}", DateTime.Now);
                }

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