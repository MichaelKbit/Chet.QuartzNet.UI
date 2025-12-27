using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 错误处理作业
    /// </summary>
    [QuartzJob("ErrorHandlingJob", "DEFAULT", "0 0/3 * * * ?", Description = "错误处理作业，每3分钟执行一次，演示异常处理")]
    public class ErrorHandlingJob : IJob
    {
        private readonly ILogger<ErrorHandlingJob> _logger;

        public ErrorHandlingJob(ILogger<ErrorHandlingJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("错误处理作业开始执行: {Time}", DateTime.Now);
            
            try
            {
                // 模拟业务逻辑
                await Task.Delay(1000);
                
                // 模拟随机错误
                if (DateTime.Now.Second % 2 == 0)
                {
                    throw new InvalidOperationException("模拟随机错误");
                }
                
                _logger.LogInformation("错误处理作业执行成功: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "错误处理作业执行失败: {Time}", DateTime.Now);
                throw; // 抛出异常，让Quartz记录失败状态
            }
        }
    }
}