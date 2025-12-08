using Chet.QuartzNet.UI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// 添加QuartzUI服务（配置驱动，自动根据 StorageType/DatabaseProvider 选择存储）
builder.Services.AddQuartzUI(builder.Configuration, options =>
{
    // 从配置文件读取邮件设置
    var emailConfig = builder.Configuration.GetSection("QuartzUI:EmailOptions");
    if (emailConfig.Exists())
    {
        emailConfig.Bind(options.EmailOptions);
    }
});
builder.Services.AddQuartzClassJobs();

var app = builder.Build();

// 启用Quartz UI中间件
app.UseQuartz();

app.MapControllers();

app.Run();
