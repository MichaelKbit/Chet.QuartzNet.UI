using Chet.QuartzNet.Core.Attributes;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// 数据处理作业
    /// </summary>
    [QuartzJob("DataProcessingJob", "DEFAULT", "0 0/6 * * * ?", Description = "数据处理作业，每6分钟执行一次")]
    public class DataProcessingJob : IJob
    {
        private readonly ILogger<DataProcessingJob> _logger;

        public DataProcessingJob(ILogger<DataProcessingJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("数据处理作业开始执行: {Time}", DateTime.Now);

            // 模拟数据处理过程
            var processedCount = 0;
            var totalItems = 100;

            for (int i = 0; i < totalItems; i++)
            {
                // 模拟处理单个数据项
                await Task.Delay(50);
                processedCount++;

                // 定期记录进度
                if (i % 20 == 0)
                {
                    _logger.LogInformation("数据处理进度: {Processed}/{Total}", processedCount, totalItems);
                }
            }

            _logger.LogInformation("数据处理作业执行完成，共处理: {Count} 条数据: {Time}", processedCount, DateTime.Now);
        }
    }
}