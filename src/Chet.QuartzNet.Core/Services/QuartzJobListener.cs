using Chet.QuartzNet.Core.Interfaces;
using Chet.QuartzNet.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Text.Json;

namespace Chet.QuartzNet.Core.Services
{
    /// <summary>
    /// Quartz作业监听器，用于记录作业执行日志
    /// </summary>
    public class QuartzJobListener : IJobListener
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuartzJobListener> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="scopeFactory">服务作用域工厂</param>
        /// <param name="logger">日志记录器</param>
        public QuartzJobListener(IServiceScopeFactory scopeFactory, ILogger<QuartzJobListener> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// 监听器名称
        /// </summary>
        public string Name => "QuartzJobListener";

        /// <summary>
        /// 作业执行完成后调用
        /// </summary>
        /// <param name="context">作业执行上下文</param>
        /// <param name="result">作业执行结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? result, CancellationToken cancellationToken = default)
        {
            try
            {
                var jobLog = new QuartzJobLog
                {
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    TriggerName = context.Trigger.Key.Name,
                    TriggerGroup = context.Trigger.Key.Group,
                    StartTime = context.FireTimeUtc.LocalDateTime,
                    EndTime = DateTime.Now,
                    Duration = (long)(DateTime.Now - context.FireTimeUtc.LocalDateTime).TotalMilliseconds
                };

                // 处理执行结果
                if (result == null)
                {
                    jobLog.Status = LogStatus.Success;
                    jobLog.Message = "作业执行成功";
                }
                else
                {
                    jobLog.Status = LogStatus.Failed;
                    jobLog.Message = "作业执行失败";
                    jobLog.Exception = result.ToString();
                    jobLog.ErrorMessage = result.Message;
                    jobLog.ErrorStackTrace = result.StackTrace;
                }

                // 记录作业数据
                if (context.MergedJobDataMap != null && context.MergedJobDataMap.Count > 0)
                {
                    var jobDataDict = context.MergedJobDataMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
                    jobLog.JobData = JsonSerializer.Serialize(jobDataDict);
                }

                // 创建作用域来解析IJobStorage
                using var scope = _scopeFactory.CreateScope();
                var jobStorage = scope.ServiceProvider.GetRequiredService<IJobStorage>();
                var jobService = scope.ServiceProvider.GetRequiredService<IQuartzJobService>();

                // 记录作业日志
                await jobStorage.AddJobLogAsync(jobLog, cancellationToken);

                // 更新作业的下次执行时间和上次执行时间
                await jobService.UpdateJobExecutionTimesAsync(context.JobDetail.Key.Name, context.JobDetail.Key.Group, context.Trigger, cancellationToken);

                _logger.LogInformation("作业执行日志记录成功: {JobKey}, 状态: {Status}",
                    $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}", jobLog.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录作业执行日志失败: {JobKey}",
                    $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}");
            }
        }

        /// <summary>
        /// 作业执行被否决时调用
        /// </summary>
        /// <param name="context">作业执行上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var jobLog = new QuartzJobLog
                {
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    TriggerName = context.Trigger.Key.Name,
                    TriggerGroup = context.Trigger.Key.Group,
                    StartTime = context.FireTimeUtc.LocalDateTime,
                    Status = LogStatus.Failed,
                    Message = "作业执行被否决"
                };

                // 创建作用域来解析IJobStorage
                using var scope = _scopeFactory.CreateScope();
                var jobStorage = scope.ServiceProvider.GetRequiredService<IJobStorage>();
                await jobStorage.AddJobLogAsync(jobLog, cancellationToken);

                _logger.LogInformation("作业执行被否决，日志记录成功: {JobKey}",
                    $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录作业执行否决日志失败: {JobKey}",
                    $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}");
            }
        }

        /// <summary>
        /// 作业即将执行前调用
        /// </summary>
        /// <param name="context">作业执行上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            // 这里不需要记录日志，因为我们在JobExecutionComplete中记录
            return Task.CompletedTask;
        }
    }
}