# Chet.QuartzNet.UI

## 项目简介

Chet.QuartzNet.UI 是一个基于 .NET 8.0 开发的轻量级 Quartz.Net 可视化管理 UI 组件库，提供了完整的任务调度管理功能，支持文件存储和数据库存储两种模式。该组件库旨在简化 Quartz.Net 的使用门槛，提供直观、易用的可视化管理界面，使开发人员能够轻松地创建、管理、监控和调试定时作业，无需深入了解 Quartz.Net 的复杂 API。

通过 Chet.QuartzNet.UI，您可以快速集成任务调度功能到现有项目中，实现作业的可视化配置、实时监控和历史记录查询，大大提高开发效率和运维便利性。

## 功能特性

- 🔧 **可视化管理 Quartz 作业**：通过 Web 界面管理 Quartz 作业、触发器和调度器
- 📊 **实时监控**：实时查看作业执行状态和日志
- 🎯 **ClassJob 模式支持**：支持基于类的作业定义，简化作业创建
- ✅ **ClassJob 自动注册**：自动扫描和注册带有特定特性的作业类
- � **多种存储方式**：支持文件存储和数据库存储（MySQL、PostgreSQL、SQL Server、SQLite）
- 🔐 **认证保护**：提供 JWT 认证保护管理界面
- 📦 **RCL 打包**：使用 Razor Class Library 打包，无侵入集成
- 🚀 **快速集成**：简单配置即可集成到现有项目
- 🎨 **现代化 UI**：基于 Ant Design Vue，界面美观易用
- 📱 **响应式设计**：支持移动端访问
- 🌙 **暗色主题**：支持系统暗色主题
- 📝 **作业执行历史**：记录作业执行历史和结果
- 🎯 **数据同步功能**：支持数据同步作业示例
- 📊 **报表生成功能**：支持报表生成作业示例
- ⏱️ **灵活的时间调度**：支持 Cron 表达式和多种触发器类型

## 快速开始

### 1. 安装 NuGet 包

根据需要选择安装的包：

```bash
# 主包（包含核心功能和文件存储）
dotnet add package Chet.QuartzNet.UI

# 如果需要数据库存储支持
dotnet add package Chet.QuartzNet.EFCore

# 数据库提供程序（根据需要选择）
dotnet add package Pomelo.EntityFrameworkCore.MySql        # MySQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL    # PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.SqlServer   # SQL Server
dotnet add package Microsoft.EntityFrameworkCore.Sqlite      # SQLite
```

### 2. 基本配置

#### 文件存储模式（推荐用于轻量级应用）

```csharp
// Program.cs
// 添加 Quartz UI 服务（文件存储模式，读取 QuartzUI 节）
builder.Services.AddQuartzUI(builder.Configuration);

// 添加 ClassJob 自动注册（可选，用于自动扫描作业类）
builder.Services.AddQuartzClassJobs();

// 启用中间件
app.UseQuartz();
```

#### 数据库存储模式（推荐用于中大型应用）

```csharp
// Program.cs
// 添加 Quartz UI 服务（StorageType=Database，DatabaseProvider=SqlServer，读取 ConnectionStrings:QuartzUI）
builder.Services.AddQuartzUI(builder.Configuration);

// 添加 ClassJob 自动注册（可选）
builder.Services.AddQuartzClassJobs();

// 启用中间件
app.UseQuartz();
```

### 3. 配置 ClassJob

创建一个作业类并添加 `[QuartzJob]` 特性：

```csharp
using Chet.QuartzNet.Core.Attributes;
using Quartz;

[QuartzJob("SampleJob", "DEFAULT", "0 0/5 * * * ?", Description = "这是一个示例作业，每5分钟执行一次")]
public class SampleJob : IJob
{
    private readonly ILogger<SampleJob> _logger;

    public SampleJob(ILogger<SampleJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SampleJob开始执行，执行时间: {ExecuteTime}", DateTime.Now);

        try
        {
            // 业务逻辑处理
            await Task.Delay(1000);

            // 获取作业数据
            var jobData = context.JobDetail.JobDataMap;
            if (jobData.ContainsKey("customData"))
            {
                _logger.LogInformation("获取到自定义数据: {CustomData}", jobData.GetString("customData"));
            }

            _logger.LogInformation("SampleJob执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SampleJob执行失败");
            throw;
        }
    }
}
```

### 4. 配置认证（可选）

```csharp
// Program.cs
// 添加 Quartz UI 服务时自动启用 JWT 认证（读取 QuartzUI 节）
builder.Services.AddQuartzUI(builder.Configuration);

// 启用中间件
app.UseQuartz();
```

配置文件：

```json
// appsettings.json
"QuartzUI": {
  "EnableJwtAuth": true,
  "UserName": "Admin",
  "Password": "123456",
  "JwtSecret": "your-secret-key-change-this-in-production",
  "JwtExpiresInMinutes": 30,
  "JwtIssuer": "Chet",
  "JwtAudience": "Chet.QuartzNet.UI"
}
```

### 5. 访问管理界面

启动应用后，访问 `/quartz-ui` 即可进入管理界面。

如果启用了认证，需要输入配置的用户名和密码。

## 数据库存储配置

### MySQL

```csharp
// 安装 Pomelo.EntityFrameworkCore.MySql
dotnet add package Pomelo.EntityFrameworkCore.MySql

// 配置服务（appsettings 设置 StorageType=Database，DatabaseProvider=MySql，ConnectionStrings:QuartzUI）
builder.Services.AddQuartzUI(builder.Configuration);
```

### PostgreSQL

```csharp
// 安装 Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

// 配置服务（DatabaseProvider=PostgreSql）
builder.Services.AddQuartzUI(builder.Configuration);
```

### SQL Server

```csharp
// 安装 Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

// 配置服务（DatabaseProvider=SqlServer）
builder.Services.AddQuartzUI(builder.Configuration);
```

### SQLite

```csharp
// 安装 Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

// 配置服务（DatabaseProvider=SQLite）
builder.Services.AddQuartzUI(builder.Configuration);
```

## 项目结构

```
src/
├── Chet.QuartzNet.Core/            # 核心服务和功能
│   ├── Attributes/                 # 作业特性定义
│   ├── Configuration/              # 配置类
│   ├── Interfaces/                 # 接口定义
│   ├── Jobs/                       # 作业相关功能
│   └── Services/                   # 核心服务实现
├── Chet.QuartzNet.EFCore/          # EF Core 数据访问层
│   ├── Data/                       # 数据库上下文
│   ├── Extensions/                 # 扩展方法
│   ├── Migrations/                 # 数据库迁移
│   └── Services/                   # 数据库存储服务
├── Chet.QuartzNet.Models/          # 数据模型和 DTO
│   ├── DTOs/                       # 数据传输对象
│   └── Entities/                   # 实体类
└── Chet.QuartzNet.UI/              # UI 组件和控制器
    ├── Controllers/                # API 控制器
    ├── Extensions/                 # 扩展方法
    ├── Middleware/                 # 中间件
    └── wwwroot/                    # 静态资源

examples/
├── Chet.QuartzNet.Example/         # 文件存储示例项目
└── Chet.QuartzNet.Test/            # 数据库存储示例项目
```

## 依赖项

- .NET 8.0
- Quartz.NET 3.8.1+ - 作业调度核心库
- ASP.NET Core 8.0+ - Web 应用框架
- Entity Framework Core 8.0+ - 数据库访问框架
- Ant Design Vue - UI 组件库
- Vue.js - 前端框架

## 技术栈

- **后端**：C# .NET 8.0, ASP.NET Core
- **前端**：Vue.js 3, Ant Design Vue
- **数据存储**：文件存储, SQL Server, MySQL, PostgreSQL, SQLite
- **ORM**：Entity Framework Core
- **作业调度**：Quartz.NET

## 许可证

MIT License

## 更多信息

请查看项目根目录的 [README.md](README.md) 文件获取完整文档。