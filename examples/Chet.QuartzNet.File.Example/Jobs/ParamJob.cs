using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 参数处理作业
    /// </summary>
    [QuartzJob("ParamJob", "DEFAULT", "0 0/2 * * * ?", Description = "参数处理作业，每2分钟执行一次")]
    public class ParamJob : IJob
    {
        private readonly ILogger<ParamJob> _logger;

        public ParamJob(ILogger<ParamJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("参数处理作业开始执行: {Time}", DateTime.Now);

            // 获取作业数据
            var jobData = context.JobDetail.JobDataMap.GetJobData<Dictionary<string, object>>();
            if (jobData != null)
            {
                _logger.LogInformation("获取到作业参数: {JobData}", jobData);

                // 处理参数逻辑
                if (jobData.TryGetValue("Message", out var message))
                {
                    _logger.LogInformation("消息参数: {Message}", message);
                }

                if (jobData.TryGetValue("Count", out var countObj) && int.TryParse(countObj?.ToString(), out var count))
                {
                    _logger.LogInformation("计数参数: {Count}", count);
                }
            }

            await Task.Delay(1000);
            _logger.LogInformation("参数处理作业执行完成: {Time}", DateTime.Now);
        }
    }
}