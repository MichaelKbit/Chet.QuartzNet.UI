# Chet.QuartzNet.UI 作业示例文档

## 项目介绍

本项目包含了9个不同类型的作业示例，展示了 Chet.QuartzNet.UI 框架的各种功能和使用场景。这些作业示例涵盖了从简单的基础作业到复杂的多参数作业，从短时间运行作业到长时间运行作业，从成功执行到错误处理等各种情况。

## 作业列表

### 1. 基础示例作业 (BasicSampleJob)

**功能**：简单的作业模板，演示最基本的作业实现。

**执行频率**：每1分钟执行一次

**核心代码**：
```csharp
[QuartzJob("BasicSampleJob", "DEFAULT", "0 0/1 * * * ?", Description = "基础示例作业，每1分钟执行一次")]
public class BasicSampleJob : IJob
{
    private readonly ILogger<BasicSampleJob> _logger;

    public BasicSampleJob(ILogger<BasicSampleJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("基础示例作业开始执行: {Time}", DateTime.Now);
        await Task.Delay(500);
        _logger.LogInformation("基础示例作业执行完成: {Time}", DateTime.Now);
    }
}
```

### 2. 带参数的作业 (ParamJob)

**功能**：演示如何获取和使用作业数据。

**执行频率**：每2分钟执行一次

**核心代码**：
```csharp
[QuartzJob("ParamJob", "DEFAULT", "0 0/2 * * * ?", Description = "带参数的作业，每2分钟执行一次")]
public class ParamJob : IJob
{
    private readonly ILogger<ParamJob> _logger;

    public ParamJob(ILogger<ParamJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("带参数的作业开始执行: {Time}", DateTime.Now);
        
        // 使用新的作业数据获取方式
        var jobDataJson = context.JobDetail.JobDataMap.GetJobDataJson();
        _logger.LogInformation("作业数据JSON: {JobDataJson}", jobDataJson);
        
        var jobDataDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jobDataJson);
        
        if (jobDataDict != null)
        {
            foreach (var kvp in jobDataDict)
            {
                _logger.LogInformation("获取到参数: {Key} = {Value}", kvp.Key, kvp.Value);
            }
        }
        
        await Task.Delay(800);
        _logger.LogInformation("带参数的作业执行完成: {Time}", DateTime.Now);
    }
}
```

### 3. 错误处理作业 (ErrorHandlingJob)

**功能**：演示作业执行过程中的异常处理。

**执行频率**：每3分钟执行一次

**核心代码**：
```csharp
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
```

### 4. 长时间运行作业 (LongRunningJob)

**功能**：演示长时间运行任务的处理方式。

**执行频率**：每10分钟执行一次

**核心代码**：
```csharp
[QuartzJob("LongRunningJob", "DEFAULT", "0 0/10 * * * ?", Description = "长时间运行作业，每10分钟执行一次，演示长时间任务处理")]
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
        
        // 模拟长时间运行的业务逻辑
        for (int i = 1; i <= 5; i++)
        {
            _logger.LogInformation("长时间运行作业进度: {Progress}%", i * 20);
            await Task.Delay(2000); // 每次延迟2秒，总共10秒
        }
        
        _logger.LogInformation("长时间运行作业执行完成: {Time}", DateTime.Now);
    }
}
```

### 5. API调用作业 (ApiCallJob)

**功能**：演示如何调用外部API。

**执行频率**：每5分钟执行一次

**核心代码**：
```csharp
[QuartzJob("ApiCallJob", "API", "0 0/5 * * * ?", Description = "API调用作业，每5分钟执行一次，演示HTTP请求")]
public class ApiCallJob : IJob
{
    private readonly ILogger<ApiCallJob> _logger;
    private static readonly HttpClient _httpClient = new HttpClient();

    public ApiCallJob(ILogger<ApiCallJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("API调用作业开始执行: {Time}", DateTime.Now);
        
        try
        {
            // 调用示例API
            var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos/1");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API调用成功，响应内容: {Content}", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API调用失败: {Time}", DateTime.Now);
            throw;
        }
    }
}
```

### 6. 数据处理作业 (DataProcessingJob)

**功能**：演示数据处理逻辑，包括数据生成、过滤和转换。

**执行频率**：每7分钟执行一次

**核心代码**：
```csharp
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
```

### 7. 定时清理作业 (CleanupJob)

**功能**：演示资源清理和维护任务。

**执行频率**：每天凌晨执行一次

**核心代码**：
```csharp
[QuartzJob("CleanupJob", "MAINTENANCE", "0 0 0 * * ?", Description = "定时清理作业，每天凌晨执行，演示资源清理")]
public class CleanupJob : IJob
{
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(ILogger<CleanupJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("定时清理作业开始执行: {Time}", DateTime.Now);
        
        try
        {
            // 模拟清理逻辑
            _logger.LogInformation("开始清理临时文件");
            await Task.Delay(1000);
            
            _logger.LogInformation("开始清理日志文件");
            await Task.Delay(1500);
            
            _logger.LogInformation("开始清理缓存数据");
            await Task.Delay(1200);
            
            _logger.LogInformation("定时清理作业执行完成: {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时清理作业执行失败: {Time}", DateTime.Now);
            throw;
        }
    }
}
```

### 8. 多参数复杂作业 (ComplexJob)

**功能**：演示复杂参数处理和强类型数据获取。

**执行频率**：每15分钟执行一次

**核心代码**：
```csharp
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

// 复杂作业数据模型
public class ComplexJobData
{
    public string JobId { get; set; }
    public string JobName { get; set; }
    public int ExecutionCount { get; set; }
    public bool EnableNotification { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

### 9. 状态监控作业 (StatusMonitorJob)

**功能**：演示系统状态监控和告警。

**执行频率**：每3分钟执行一次

**核心代码**：
```csharp
[QuartzJob("StatusMonitorJob", "MONITOR", "0 0/3 * * * ?", Description = "状态监控作业，每3分钟执行一次，演示系统状态监控")]
public class StatusMonitorJob : IJob
{
    private readonly ILogger<StatusMonitorJob> _logger;

    public StatusMonitorJob(ILogger<StatusMonitorJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("状态监控作业开始执行: {Time}", DateTime.Now);
        
        try
        {
            // 模拟系统状态检查
            var cpuUsage = new Random().Next(10, 90);
            var memoryUsage = new Random().Next(20, 85);
            var diskUsage = new Random().Next(30, 80);
            
            _logger.LogInformation("CPU使用率: {CpuUsage}%", cpuUsage);
            _logger.LogInformation("内存使用率: {MemoryUsage}%", memoryUsage);
            _logger.LogInformation("磁盘使用率: {DiskUsage}%", diskUsage);
            
            // 模拟异常情况
            if (cpuUsage > 80 || memoryUsage > 80 || diskUsage > 80)
            {
                _logger.LogWarning("系统资源使用率过高，需要关注");
            }
            
            await Task.Delay(1800);
            _logger.LogInformation("状态监控作业执行完成: {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态监控作业执行失败: {Time}", DateTime.Now);
            throw;
        }
    }
}
```

## 使用方法

### 1. 启动应用程序

```bash
dotnet run
```

### 2. 访问 Quartz UI 界面

打开浏览器，访问 `http://localhost:5000/quartz-ui` 进入 Quartz UI 界面。

### 3. 查看作业列表

在 "作业管理" 菜单中，可以查看所有作业的列表和状态。

### 4. 查看作业执行日志

在 "日志管理" 菜单中，可以查看所有作业的执行日志，包括执行时间、状态、耗时和异常信息。

### 5. 手动触发作业

在 "作业管理" 页面，找到要操作的作业，点击 "立即执行" 按钮，可以手动触发作业执行。

### 6. 修改作业配置

在 "作业管理" 页面，找到要操作的作业，点击 "编辑" 按钮，可以修改作业的配置，包括执行频率、作业数据和通知设置等。

## 技术特点

### 1. 基于特性的作业定义

使用 `[QuartzJob]` 特性定义作业，包括作业名称、分组、Cron 表达式和描述等。

### 2. 依赖注入支持

作业类支持依赖注入，可以注入日志记录器、数据库上下文等服务。

### 3. 作业数据支持

支持 JSON 格式的作业数据，可以在作业中使用 `GetJobDataJson` 和 `GetJobData<T>` 方法获取作业数据。

### 4. 异常处理机制

内置了完善的异常处理机制，自动记录作业执行过程中的异常信息。

### 5. 日志记录

自动记录作业的执行历史，包括执行时间、状态、耗时和异常信息。

### 6. 通知功能

支持配置通知服务，在作业执行成功或失败时发送通知。

## 最佳实践

1. **作业粒度**：将复杂的业务逻辑拆分为多个小作业，提高作业的可维护性和可靠性。

2. **作业数据**：尽量使用简单的作业数据结构，避免传递过大的数据。

3. **错误处理**：在作业中添加完善的错误处理，记录详细的日志。

4. **超时设置**：根据作业的实际执行时间，合理设置作业的超时时间。

5. **并发控制**：对于可能存在并发问题的作业，使用分布式锁或其他并发控制机制。

6. **测试**：在正式环境部署前，充分测试作业的执行情况。

## 总结

本项目提供了9个不同类型的作业示例，展示了 Chet.QuartzNet.UI 框架的各种功能和使用场景。这些作业示例可以作为您开发自己的作业时的参考，帮助您更快地理解和使用 Chet.QuartzNet.UI 框架。

如果您有任何问题或建议，欢迎在 GitHub 仓库提交 Issue 或参与讨论。
