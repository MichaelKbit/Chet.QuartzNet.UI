using Chet.QuartzNet.Core.Interfaces;
using Chet.QuartzNet.EFCore.Data;
using Chet.QuartzNet.Models.DTOs;
using Chet.QuartzNet.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.QuartzNet.EFCore.Services;

/// <summary>
/// EFCore数据库存储实现
/// </summary>
public class EFCoreJobStorage : IJobStorage
{
    private readonly QuartzDbContext _dbContext;
    private readonly ILogger<EFCoreJobStorage> _logger;

    public EFCoreJobStorage(QuartzDbContext dbContext, ILogger<EFCoreJobStorage> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> AddJobAsync(QuartzJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查是否已存在
            var existingJob = await _dbContext.QuartzJobs
                .FirstOrDefaultAsync(j => j.JobName == jobInfo.JobName && j.JobGroup == jobInfo.JobGroup, cancellationToken);

            if (existingJob != null)
            {
                _logger.LogWarning("作业已存在: {JobKey}", jobInfo.GetJobKey());
                return false;
            }

            await _dbContext.QuartzJobs.AddAsync(jobInfo, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("作业添加成功: {JobKey}", jobInfo.GetJobKey());
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加作业失败: {JobKey}", jobInfo.GetJobKey());
            return false;
        }
    }

    public async Task<bool> UpdateJobAsync(QuartzJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingJob = await _dbContext.QuartzJobs
                .FirstOrDefaultAsync(j => j.JobName == jobInfo.JobName && j.JobGroup == jobInfo.JobGroup, cancellationToken);

            if (existingJob == null)
            {
                _logger.LogWarning("作业不存在: {JobKey}", jobInfo.GetJobKey());
                return false;
            }

            // 更新属性
            existingJob.TriggerName = jobInfo.TriggerName;
            existingJob.TriggerGroup = jobInfo.TriggerGroup;
            existingJob.CronExpression = jobInfo.CronExpression;
            existingJob.Description = jobInfo.Description;
            existingJob.JobType = jobInfo.JobType;
            existingJob.JobData = jobInfo.JobData;
            existingJob.StartTime = jobInfo.StartTime;
            existingJob.EndTime = jobInfo.EndTime;
            existingJob.IsEnabled = jobInfo.IsEnabled;
            existingJob.Status = jobInfo.Status;
            existingJob.UpdateTime = jobInfo.UpdateTime;
            existingJob.UpdateBy = jobInfo.UpdateBy;
            existingJob.Remark = jobInfo.Remark;

            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("作业更新成功: {JobKey}", jobInfo.GetJobKey());
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新作业失败: {JobKey}", jobInfo.GetJobKey());
            return false;
        }
    }

    public async Task<bool> DeleteJobAsync(string jobName, string jobGroup, CancellationToken cancellationToken = default)
    {
        try
        {
            var job = await _dbContext.QuartzJobs
                .FirstOrDefaultAsync(j => j.JobName == jobName && j.JobGroup == jobGroup, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("作业不存在: {JobKey}", $"{jobGroup}.{jobName}");
                return false;
            }

            _dbContext.QuartzJobs.Remove(job);
            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("作业删除成功: {JobKey}", $"{jobGroup}.{jobName}");
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除作业失败: {JobKey}", $"{jobGroup}.{jobName}");
            return false;
        }
    }

    public async Task<QuartzJobInfo?> GetJobAsync(string jobName, string jobGroup, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.QuartzJobs
                .FirstOrDefaultAsync(j => j.JobName == jobName && j.JobGroup == jobGroup, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取作业失败: {JobKey}", $"{jobGroup}.{jobName}");
            return null;
        }
    }

    public async Task<PagedResponseDto<QuartzJobInfo>> GetJobsAsync(QuartzJobQueryDto queryDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.QuartzJobs.AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(queryDto.JobName))
            {
                query = query.Where(j => EF.Functions.Like(j.JobName, $"%{queryDto.JobName}%"));
            }

            if (!string.IsNullOrEmpty(queryDto.JobGroup))
            {
                query = query.Where(j => EF.Functions.Like(j.JobGroup, $"%{queryDto.JobGroup}%"));
            }

            if (queryDto.Status.HasValue)
            {
                query = query.Where(j => j.Status == queryDto.Status.Value);
            }

            if (queryDto.IsEnabled.HasValue)
            {
                query = query.Where(j => j.IsEnabled == queryDto.IsEnabled.Value);
            }

            // 应用排序
            if (!string.IsNullOrEmpty(queryDto.SortBy))
            {
                var isAscending = string.Equals(queryDto.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);

                switch (queryDto.SortBy.ToLower())
                {
                    case "jobname":
                        query = isAscending ? query.OrderBy(j => j.JobName) : query.OrderByDescending(j => j.JobName);
                        break;
                    case "jobgroup":
                        query = isAscending ? query.OrderBy(j => j.JobGroup) : query.OrderByDescending(j => j.JobGroup);
                        break;
                    case "status":
                        query = isAscending ? query.OrderBy(j => j.Status) : query.OrderByDescending(j => j.Status);
                        break;
                    case "isenabled":
                        query = isAscending ? query.OrderBy(j => j.IsEnabled) : query.OrderByDescending(j => j.IsEnabled);
                        break;
                    case "createtime":
                        query = isAscending ? query.OrderBy(j => j.CreateTime) : query.OrderByDescending(j => j.CreateTime);
                        break;
                    case "updatetime":
                        query = isAscending ? query.OrderBy(j => j.UpdateTime) : query.OrderByDescending(j => j.UpdateTime);
                        break;
                    default:
                        // 默认按创建时间降序排序
                        query = query.OrderByDescending(j => j.CreateTime);
                        break;
                }
            }
            else
            {
                // 默认按创建时间降序排序
                query = query.OrderByDescending(j => j.CreateTime);
            }

            // 分页
            var totalCount = await query.CountAsync(cancellationToken);
            var pagedJobs = await query
                .Skip((queryDto.PageIndex - 1) * queryDto.PageSize)
                .Take(queryDto.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponseDto<QuartzJobInfo>
            {
                Items = pagedJobs,
                TotalCount = totalCount,
                PageIndex = queryDto.PageIndex,
                PageSize = queryDto.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取作业列表失败");
            return new PagedResponseDto<QuartzJobInfo>();
        }
    }

    public async Task<List<QuartzJobInfo>> GetAllJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.QuartzJobs.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有作业失败");
            return new List<QuartzJobInfo>();
        }
    }

    public async Task<bool> UpdateJobStatusAsync(string jobName, string jobGroup, JobStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var job = await _dbContext.QuartzJobs
                .FirstOrDefaultAsync(j => j.JobName == jobName && j.JobGroup == jobGroup, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("作业不存在: {JobKey}", $"{jobGroup}.{jobName}");
                return false;
            }

            job.Status = status;
            job.UpdateTime = DateTime.Now;

            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("作业状态更新成功: {JobKey}, 状态: {Status}", $"{jobGroup}.{jobName}", status);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新作业状态失败: {JobKey}", $"{jobGroup}.{jobName}");
            return false;
        }
    }

    public async Task<bool> AddJobLogAsync(QuartzJobLog jobLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.QuartzJobLogs.AddAsync(jobLog, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("作业日志添加成功: {LogId}", jobLog.LogId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加作业日志失败");
            return false;
        }
    }

    public async Task<PagedResponseDto<QuartzJobLog>> GetJobLogsAsync(string? jobName, string? jobGroup, LogStatus? status, DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, string? sortBy = null, string? sortOrder = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.QuartzJobLogs.AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(jobName))
            {
                query = query.Where(l => EF.Functions.Like(l.JobName, $"%{jobName}%"));
            }

            if (!string.IsNullOrEmpty(jobGroup))
            {
                query = query.Where(l => EF.Functions.Like(l.JobGroup, $"%{jobGroup}%"));
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            if (startTime.HasValue)
            {
                query = query.Where(l => l.StartTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(l => l.StartTime <= endTime.Value);
            }

            // 应用排序
            if (!string.IsNullOrEmpty(sortBy))
            {
                var isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

                switch (sortBy.ToLower())
                {
                    case "jobname":
                        query = isAscending ? query.OrderBy(l => l.JobName) : query.OrderByDescending(l => l.JobName);
                        break;
                    case "jobgroup":
                        query = isAscending ? query.OrderBy(l => l.JobGroup) : query.OrderByDescending(l => l.JobGroup);
                        break;
                    case "status":
                        query = isAscending ? query.OrderBy(l => l.Status) : query.OrderByDescending(l => l.Status);
                        break;
                    case "createtime":
                        query = isAscending ? query.OrderBy(l => l.CreateTime) : query.OrderByDescending(l => l.CreateTime);
                        break;
                    case "starttime":
                        query = isAscending ? query.OrderBy(l => l.StartTime) : query.OrderByDescending(l => l.StartTime);
                        break;
                    case "endtime":
                        query = isAscending ? query.OrderBy(l => l.EndTime) : query.OrderByDescending(l => l.EndTime);
                        break;
                    case "duration":
                        query = isAscending ? query.OrderBy(l => l.Duration) : query.OrderByDescending(l => l.Duration);
                        break;
                    default:
                        // 默认按创建时间降序排序
                        query = query.OrderByDescending(l => l.CreateTime);
                        break;
                }
            }
            else
            {
                // 默认按创建时间降序排序
                query = query.OrderByDescending(l => l.CreateTime);
            }

            // 分页
            var totalCount = await query.CountAsync(cancellationToken);
            var pagedLogs = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponseDto<QuartzJobLog>
            {
                Items = pagedLogs,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取作业日志失败");
            return new PagedResponseDto<QuartzJobLog>();
        }
    }

    public async Task<int> ClearExpiredLogsAsync(int daysToKeep, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            var expiredLogs = await _dbContext.QuartzJobLogs
                .Where(l => l.CreateTime < cutoffDate)
                .ToListAsync(cancellationToken);

            var expiredCount = expiredLogs.Count;

            if (expiredCount > 0)
            {
                _dbContext.QuartzJobLogs.RemoveRange(expiredLogs);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("清除过期日志成功: {Count} 条", expiredCount);
            }

            return expiredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除过期日志失败");
            return 0;
        }
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 应用所有未应用的迁移
            await _dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("EFCore数据库存储初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EFCore数据库存储初始化失败");
            return false;
        }
    }

    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}