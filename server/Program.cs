using GitWeb.Api.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 配置 JSON 序列化选项，支持大型响应
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 移除 JSON 深度限制
        options.JsonSerializerOptions.MaxDepth = 128;
        // 允许大型 JSON 响应
        options.JsonSerializerOptions.DefaultBufferSize = 16 * 1024 * 1024; // 16MB
        // 性能优化
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // 忽略 null 值（减少 JSON 大小，避免前端渲染问题）
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 配置 Kestrel 服务器选项
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // 增加请求体大小限制（如果需要上传大文件）
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
    // 增加响应缓冲区大小
    serverOptions.Limits.MaxResponseBufferSize = 16 * 1024 * 1024; // 16MB
    // 增加请求头大小
    serverOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins("http://localhost:9001")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton<GitService>();
builder.Services.AddSingleton<RepoConfigService>();

var app = builder.Build();

app.UseCors("dev");
app.MapControllers();

builder.WebHost.UseUrls("http://localhost:9002");

app.Run();