using GitWeb.Api.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
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