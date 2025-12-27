using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 多参数复杂作业
    /// </summary>
    [QuartzJob("ComplexJob", "COMPLEX", "0 0/15 * * * ?", Description = "多参数复杂作业，每15分钟执行一次，演示复杂参数处理")]
    public class ComplexJob : IJob
    {
        private readonly ILogger<ComplexJob> _logger;

        public ComplexJob(ILogger<ComplexJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("多参数复杂作业开始执行: {Time}", DateTime.Now);
            
            // 使用强类型获取作业数据
            var jobData = context.JobDetail.JobDataMap.GetJobData<ComplexJobData>();
            
            if (jobData != null)
            {
                _logger.LogInformation("作业ID: {JobId}", jobData.JobId);
                _logger.LogInformation("作业名称: {JobName}", jobData.JobName);
                _logger.LogInformation("执行次数: {ExecutionCount}", jobData.ExecutionCount);
                _logger.LogInformation("启用通知: {EnableNotification}", jobData.EnableNotification);
                
                if (jobData.Parameters != null)
                {
                    foreach (var param in jobData.Parameters)
                    {
                        _logger.LogInformation("参数: {Key} = {Value}", param.Key, param.Value);
                    }
                }
            }
            
            await Task.Delay(2000);
            _logger.LogInformation("多参数复杂作业执行完成: {Time}", DateTime.Now);
        }
    }

    /// <summary>
    /// 复杂作业数据模型
    /// </summary>
    public class ComplexJobData
    {
        public string JobId { get; set; }
        public string JobName { get; set; }
        public int ExecutionCount { get; set; }
        public bool EnableNotification { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}