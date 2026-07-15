namespace Chet.QuartzNet.Models.DTOs;

/// <summary>
/// 系统配置DTO
/// 用于在多实例环境中区分不同服务，配置显示在分析页顶部
/// </summary>
public class SystemConfigDto
{
    /// <summary>
    /// 服务名称
    /// 显示在分析页顶部横幅及浏览器标题
    /// </summary>
    public string ServiceName { get; set; } = "QuartzNet 调度服务";

    /// <summary>
    /// 环境标识
    /// 可选值：DEV / TEST / UAT / PROD
    /// </summary>
    public string Environment { get; set; } = "DEV";

    /// <summary>
    /// 服务描述
    /// 显示在服务名称下方的副标题
    /// </summary>
    public string ServiceDescription { get; set; } = string.Empty;
}
