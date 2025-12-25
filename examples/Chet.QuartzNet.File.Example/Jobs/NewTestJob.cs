using Chet.QuartzNet.Core.Attributes;
using Quartz;
using System.Text.Json;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 新的测试ClassJob实现
    /// </summary>
    [QuartzJob("NewTestJob", "ExampleGroup", "0/5 * * * * ?", "新的测试ClassJob，每5秒执行一次", Enabled = true)]
    public class NewTestJob : IJob
    {
        private readonly ILogger<NewTestJob> _logger;

        /// <summary>
        /// 构造函数，注入日志记录器
        /// </summary>
        /// <param name="logger"></param>
        public NewTestJob(ILogger<NewTestJob> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 作业执行方法
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("NewTestJob 开始执行: {Time}", DateTime.Now);

            try
            {
                // 模拟作业执行逻辑
                await Task.Delay(500);

                _logger.LogInformation("NewTestJob 执行完成: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NewTestJob 执行失败: {Time}", DateTime.Now);
                throw;
            }
        }
    }
}