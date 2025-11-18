using Chet.QuartzNet.Core.Configuration;
using Chet.QuartzNet.Core.Interfaces;
using Chet.QuartzNet.EFCore.Data;
using Chet.QuartzNet.EFCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chet.QuartzNet.EFCore.Extensions;

/// <summary>
/// EFCore服务扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加EFCore数据库存储支持（MySQL）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="serverVersion">MySQL服务器版本</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUIMySql(this IServiceCollection services, string connectionString, string? serverVersion = null)
    {
        services.AddDbContext<QuartzDbContext>(options =>
        {
            var mySqlServerVersion = serverVersion != null
                ? MySqlServerVersion.Parse(serverVersion)
                : MySqlServerVersion.AutoDetect(connectionString);

            options.UseMySql(connectionString, mySqlServerVersion, mySqlOptions =>
            {
                mySqlOptions.MaxBatchSize(1);
                mySqlOptions.MigrationsAssembly("Chet.QuartzNet.EFCore");
            });
        });

        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 添加EFCore数据库存储支持（MySQL）- 使用DbContextOptions
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">DbContext配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUIMySql(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<QuartzDbContext>(optionsAction);
        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 添加EFCore数据库存储支持（PostgreSQL）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUIPostgreSQL(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<QuartzDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Chet.QuartzNet.EFCore");
            });
        });

        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 添加EFCore数据库存储支持（PostgreSQL）- 使用DbContextOptions
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">DbContext配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUIPostgreSQL(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<QuartzDbContext>(optionsAction);
        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 添加EFCore数据库存储支持（SQL Server）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUISqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<QuartzDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly("Chet.QuartzNet.EFCore");
            });
        });

        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 添加EFCore数据库存储支持（SQLite）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUISQLite(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<QuartzDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly("Chet.QuartzNet.EFCore");
            });
        });

        services.Replace(ServiceDescriptor.Scoped<IJobStorage, EFCoreJobStorage>());
        return services;
    }

    /// <summary>
    /// 从配置文件中添加EFCore数据库存储支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddQuartzUIDatabaseFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var quartzUIOptions = configuration.GetSection("QuartzUI").Get<QuartzUIOptions>();

        if (quartzUIOptions?.StorageType == StorageType.Database)
        {
            var connectionString = configuration.GetConnectionString("QuartzUI");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("未找到QuartzUI数据库连接字符串配置");
            }

            var databaseProvider = quartzUIOptions.DatabaseProvider;

            switch (databaseProvider)
            {
                case DatabaseProvider.MySql:
                    services.AddQuartzUIMySql(connectionString);
                    break;
                case DatabaseProvider.PostgreSql:
                    services.AddQuartzUIPostgreSQL(connectionString);
                    break;
                case DatabaseProvider.SqlServer:
                    services.AddQuartzUISqlServer(connectionString);
                    break;
                case DatabaseProvider.SQLite:
                    services.AddQuartzUISQLite(connectionString);
                    break;
                default:
                    throw new ArgumentException($"不支持的数据库提供程序: {databaseProvider}");
            }
        }

        return services;
    }
}