using Chet.QuartzNet.Core.Attributes;
using Chet.QuartzNet.Core.Extensions;
using Quartz;

namespace Chet.QuartzNet.File.Example.Jobs
{
    /// <summary>
    /// API调用作业
    /// </summary>
    [QuartzJob("ApiCallJob", "DEFAULT", "0 0/4 * * * ?", Description = "API调用作业，每4分钟执行一次")]
    public class ApiCallJob : IJob
    {
        private readonly ILogger<ApiCallJob> _logger;
        private readonly HttpClient _httpClient;

        public ApiCallJob(ILogger<ApiCallJob> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("API调用作业开始执行: {Time}", DateTime.Now);

            try
            {
                // 获取作业数据
                var jobData = context.JobDetail.JobDataMap.GetJobData<Dictionary<string, object>>();
                string apiUrl = jobData?.TryGetValue("ApiUrl", out var url) == true ? url.ToString() : "https://jsonplaceholder.typicode.com/todos/1";

                _logger.LogInformation("调用API: {ApiUrl}", apiUrl);
                
                // 设置超时时间
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                var response = await _httpClient.GetAsync(apiUrl, cts.Token);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogInformation("API调用成功，返回内容: {Content}", content);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("API调用作业超时: {Time}", DateTime.Now);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API调用作业执行失败: {Time}", DateTime.Now);
                throw;
            }
        }
    }
}