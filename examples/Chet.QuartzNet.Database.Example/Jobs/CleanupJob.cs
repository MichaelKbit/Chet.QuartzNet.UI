using Chet.QuartzNet.Core.Attributes;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 定时清理作业
    /// </summary>
    [QuartzJob("CleanupJob", "MAINTENANCE", "0 0 0 * * ?", Description = "定时清理作业，每天凌晨执行，演示资源清理")]
    public class CleanupJob : IJob
    {
        private readonly ILogger<CleanupJob> _logger;

        public CleanupJob(ILogger<CleanupJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("定时清理作业开始执行: {Time}", DateTime.Now);

            try
            {
                // 模拟清理逻辑
                _logger.LogInformation("开始清理临时文件");
                await Task.Delay(1000);

                _logger.LogInformation("开始清理日志文件");
                await Task.Delay(1500);

                _logger.LogInformation("开始清理缓存数据");
                await Task.Delay(1200);

                _logger.LogInformation("定时清理作业执行完成: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时清理作业执行失败: {Time}", DateTime.Now);
                throw;
            }
        }
    }
}