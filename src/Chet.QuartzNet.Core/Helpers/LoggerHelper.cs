using Microsoft.Extensions.Logging;

namespace Chet.QuartzNet.Core.Helpers;

/// <summary>
/// 日志帮助类，提供统一的日志记录方法和标准化格式
/// </summary>
public static class LoggerHelper
{
    #region Information 日志
    /// <summary>
    /// 记录信息级别的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息</param>
    public static void LogInfo<T>(this ILogger<T> logger, string message)
    {
        logger.LogInformation(message);
    }

    /// <summary>
    /// 记录信息级别的日志（带参数）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息模板</param>
    /// <param name="args">日志消息参数</param>
    public static void LogInfo<T>(this ILogger<T> logger, string message, params object?[] args)
    {
        logger.LogInformation(message, args);
    }

    /// <summary>
    /// 记录操作成功的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="details">操作详情</param>
    public static void LogSuccess<T>(this ILogger<T> logger, string operation, string details = "")
    {
        if (string.IsNullOrEmpty(details))
        {
            logger.LogInformation("✅ [{Operation}] 操作成功", operation);
        }
        else
        {
            logger.LogInformation("✅ [{Operation}] 操作成功: {Details}", operation, details);
        }
    }

    /// <summary>
    /// 记录操作成功的日志（带参数）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="message">日志消息模板</param>
    /// <param name="args">日志消息参数</param>
    public static void LogSuccess<T>(this ILogger<T> logger, string operation, string message, params object?[] args)
    {
        logger.LogInformation($"✅ [{operation}] {message}", args);
    }
    #endregion

    #region Warning 日志
    /// <summary>
    /// 记录警告级别的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息</param>
    public static void LogWarn<T>(this ILogger<T> logger, string message)
    {
        logger.LogWarning(message);
    }

    /// <summary>
    /// 记录警告级别的日志（带参数）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息模板</param>
    /// <param name="args">日志消息参数</param>
    public static void LogWarn<T>(this ILogger<T> logger, string message, params object?[] args)
    {
        logger.LogWarning(message, args);
    }

    /// <summary>
    /// 记录操作警告的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="details">警告详情</param>
    public static void LogWarningOperation<T>(this ILogger<T> logger, string operation, string details)
    {
        logger.LogWarning("⚠️ [{Operation}] 操作警告: {Details}", operation, details);
    }
    #endregion

    #region Error 日志

    /// <summary>
    /// 记录操作失败的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="exception">异常对象</param>
    public static void LogFailure<T>(this ILogger<T> logger, string operation, Exception exception)
    {
        logger.LogError(exception, "❌ [{Operation}] 操作失败", operation);
    }

    /// <summary>
    /// 记录操作失败的日志（带详情）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="details">失败详情</param>
    /// <param name="exception">异常对象</param>
    public static void LogFailure<T>(this ILogger<T> logger, string operation, string details, Exception exception)
    {
        logger.LogError(exception, "❌ [{Operation}] 操作失败: {Details}", operation, details);
    }

    /// <summary>
    /// 记录操作失败的日志（不带异常）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="details">失败详情</param>
    public static void LogFailure<T>(this ILogger<T> logger, string operation, string details)
    {
        logger.LogError("❌ [{Operation}] 操作失败: {Details}", operation, details);
    }
    #endregion

    #region Debug 日志
    /// <summary>
    /// 记录调试级别的操作日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="details">操作详情</param>
    public static void LogDebugOperation<T>(this ILogger<T> logger, string operation, string details = "")
    {
        if (string.IsNullOrEmpty(details))
        {
            logger.LogDebug("🔍 [{Operation}] 调试信息", operation);
        }
        else
        {
            logger.LogDebug("🔍 [{Operation}] 调试信息: {Details}", operation, details);
        }
    }
    #endregion

    #region Trace 日志
    /// <summary>
    /// 记录跟踪级别的日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息</param>
    public static void LogTrace<T>(this ILogger<T> logger, string message)
    {
        logger.LogTrace(message);
    }

    /// <summary>
    /// 记录跟踪级别的日志（带参数）
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">日志消息模板</param>
    /// <param name="args">日志消息参数</param>
    public static void LogTrace<T>(this ILogger<T> logger, string message, params object?[] args)
    {
        logger.LogTrace(message, args);
    }
    #endregion

    #region 结构化日志
    /// <summary>
    /// 记录结构化日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="logLevel">日志级别</param>
    /// <param name="eventName">事件名称</param>
    /// <param name="properties">结构化属性</param>
    public static void LogStructured<T>(this ILogger<T> logger, LogLevel logLevel, string eventName, params (string Key, object? Value)[] properties)
    {
        var state = new Dictionary<string, object?>
        {
            ["EventName"] = eventName
        };

        foreach (var (key, value) in properties)
        {
            state[key] = value;
        }

        logger.Log(logLevel, new EventId(), state, null, (s, e) =>
        {
            var propertyString = string.Join(", ", s.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"📋 [{eventName}] {propertyString}";
        });
    }

    /// <summary>
    /// 记录信息级别的结构化日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventName">事件名称</param>
    /// <param name="properties">结构化属性</param>
    public static void LogInfoStructured<T>(this ILogger<T> logger, string eventName, params (string Key, object? Value)[] properties)
    {
        LogStructured(logger, LogLevel.Information, eventName, properties);
    }

    /// <summary>
    /// 记录错误级别的结构化日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventName">事件名称</param>
    /// <param name="exception">异常对象</param>
    /// <param name="properties">结构化属性</param>
    public static void LogErrorStructured<T>(this ILogger<T> logger, string eventName, Exception exception, params (string Key, object? Value)[] properties)
    {
        var state = new Dictionary<string, object?>
        {
            ["EventName"] = eventName
        };

        foreach (var (key, value) in properties)
        {
            state[key] = value;
        }

        logger.Log(LogLevel.Error, new EventId(), state, exception, (s, e) =>
        {
            var propertyString = string.Join(", ", s.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"📋 [{eventName}] {propertyString}";
        });
    }
    #endregion

    #region 性能日志
    /// <summary>
    /// 记录性能指标日志
    /// </summary>
    /// <typeparam name="T">日志记录器泛型类型</typeparam>
    /// <param name="logger">日志记录器</param>
    /// <param name="operation">操作名称</param>
    /// <param name="durationMilliseconds">持续时间（毫秒）</param>
    /// <param name="thresholdMilliseconds">警告阈值（毫秒）</param>
    public static void LogPerformance<T>(this ILogger<T> logger, string operation, long durationMilliseconds, long thresholdMilliseconds = 1000)
    {
        if (durationMilliseconds > thresholdMilliseconds)
        {
            logger.LogWarning("⏱️ [{Operation}] 性能警告: 执行时间过长 - {Duration}ms", operation, durationMilliseconds);
        }
        else
        {
            logger.LogInformation("⏱️ [{Operation}] 执行时间: {Duration}ms", operation, durationMilliseconds);
        }
    }
    #endregion
}