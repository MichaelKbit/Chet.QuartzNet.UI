using Chet.QuartzNet.Core.Attributes;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 长时间运行作业
    /// </summary>
    [QuartzJob("LongRunningJob", "DEFAULT", "0 0/10 * * * ?", Description = "长时间运行作业，每10分钟执行一次，演示长时间任务处理")]
    public class LongRunningJob : IJob
    {
        private readonly ILogger<LongRunningJob> _logger;

        public LongRunningJob(ILogger<LongRunningJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("长时间运行作业开始执行: {Time}", DateTime.Now);

            // 模拟长时间运行的业务逻辑
            for (int i = 1; i <= 5; i++)
            {
                _logger.LogInformation("长时间运行作业进度: {Progress}%", i * 20);
                await Task.Delay(2000); // 每次延迟2秒，总共10秒
            }

            _logger.LogInformation("长时间运行作业执行完成: {Time}", DateTime.Now);
        }
    }
}