using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 复杂作业
    /// </summary>
    [QuartzJob("ComplexJob", "DEFAULT", "0 0/10 * * * ?", Description = "复杂作业，每10分钟执行一次")]
    public class ComplexJob : IJob
    {
        private readonly ILogger<ComplexJob> _logger;

        public ComplexJob(ILogger<ComplexJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("复杂作业开始执行: {Time}", DateTime.Now);

            try
            {
                // 获取作业数据
                var jobData = context.JobDetail.JobDataMap.GetJobData<Dictionary<string, object>>();
                var complexParams = jobData?.TryGetValue("ComplexParams", out var paramValue) == true ? paramValue.ToString() : "default";

                _logger.LogInformation("复杂作业参数: {Params}", complexParams);

                // 步骤1: 数据准备
                await Step1_DataPreparation();

                // 步骤2: 业务处理
                await Step2_BusinessProcessing();

                // 步骤3: 结果验证
                await Step3_ResultValidation();

                // 步骤4: 结果通知
                await Step4_ResultNotification();

                _logger.LogInformation("复杂作业执行完成: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复杂作业执行失败: {Time}", DateTime.Now);
                throw;
            }
        }

        private async Task Step1_DataPreparation()
        {
            _logger.LogInformation("复杂作业 - 步骤1: 数据准备开始");
            await Task.Delay(800);
            _logger.LogInformation("复杂作业 - 步骤1: 数据准备完成");
        }

        private async Task Step2_BusinessProcessing()
        {
            _logger.LogInformation("复杂作业 - 步骤2: 业务处理开始");
            await Task.Delay(1200);
            _logger.LogInformation("复杂作业 - 步骤2: 业务处理完成");
        }

        private async Task Step3_ResultValidation()
        {
            _logger.LogInformation("复杂作业 - 步骤3: 结果验证开始");
            await Task.Delay(600);
            _logger.LogInformation("复杂作业 - 步骤3: 结果验证完成");
        }

        private async Task Step4_ResultNotification()
        {
            _logger.LogInformation("复杂作业 - 步骤4: 结果通知开始");
            await Task.Delay(400);
            _logger.LogInformation("复杂作业 - 步骤4: 结果通知完成");
        }
    }
}