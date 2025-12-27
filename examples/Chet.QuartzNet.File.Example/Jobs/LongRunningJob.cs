using Chet.QuartzNet.Core.Attributes;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 长时间运行作业
    /// </summary>
    [QuartzJob("LongRunningJob", "DEFAULT", "0 0/5 * * * ?", Description = "长时间运行作业，每5分钟执行一次")]
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

            var cancellationToken = context.CancellationToken;
            int totalSteps = 10;

            for (int i = 0; i < totalSteps; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("长时间运行作业被取消: {Time}", DateTime.Now);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                _logger.LogInformation("长时间运行作业执行进度: {Step}/{Total} - {Time}", i + 1, totalSteps, DateTime.Now);
                await Task.Delay(1000, cancellationToken);
            }

            _logger.LogInformation("长时间运行作业执行完成: {Time}", DateTime.Now);
        }
    }
}