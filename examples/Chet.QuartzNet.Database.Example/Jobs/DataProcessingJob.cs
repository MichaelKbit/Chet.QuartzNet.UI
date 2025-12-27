using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.Database.Example.Jobs
{
    /// <summary>
    /// 数据处理作业
    /// </summary>
    [QuartzJob("DataProcessingJob", "DATA", "0 0/7 * * * ?", Description = "数据处理作业，每7分钟执行一次，演示数据处理逻辑")]
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
            
            // 模拟数据处理
            var data = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                data.Add(i);
            }
            
            // 模拟复杂数据处理
            var processedData = data.Where(x => x % 2 == 0)
                                   .Select(x => x * 2)
                                   .ToList();
            
            await Task.Delay(1500);
            
            _logger.LogInformation("数据处理完成: 原始数据 {OriginalCount} 条，处理后 {ProcessedCount} 条", 
                                   data.Count, processedData.Count);
        }
    }
}