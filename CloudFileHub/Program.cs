using CloudFileHub.Data;
using CloudFileHub.Models;
using CloudFileHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure AliyunOSS options
builder.Services.Configure<AliyunOSSOptions>(
    builder.Configuration.GetSection(AliyunOSSOptions.SectionName));

// Configure AI Assistant options (保留原有配置支持)
builder.Services.Configure<AiAssistantOptions>(
    builder.Configuration.GetSection(AiAssistantOptions.SectionName));

// Configure AlibabaCloud AI options
builder.Services.Configure<AlibabaCloudAiOptions>(
    builder.Configuration.GetSection("AlibabaCloudAi"));

// Add DbContext - 使用MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString, 
        ServerVersion.AutoDetect(connectionString)));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

// Add application services
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<ShareService>();
builder.Services.AddScoped<AliyunOSSService>();
builder.Services.AddScoped<DashboardService>();

// Add AI-related services
builder.Services.AddScoped<DocumentTextExtractorService>();

// Add HttpClient for AlibabaCloud AI Services
builder.Services.AddHttpClient<AlibabaCloudAiService>();
builder.Services.AddHttpClient<AlibabaCloudCompatibleAiService>();

// Register both AI services
builder.Services.AddScoped<AiAssistantService>();
builder.Services.AddScoped<AlibabaCloudAiService>();
builder.Services.AddScoped<AlibabaCloudCompatibleAiService>();

// Register AI service factory (可选，支持动态切换)
builder.Services.AddScoped<AiServiceFactory>();

// Register AI Assistant Service - 使用阿里云百炼兼容模式实现
builder.Services.AddScoped<IAiAssistantService, AlibabaCloudCompatibleAiService>();

var app = builder.Build();

// 自动应用数据库迁移
ApplyMigrations(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

// 自动迁移辅助方法
void ApplyMigrations(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            // 检查数据库连接是否可用
            context.Database.GetPendingMigrations();
            
            // 如果可用，应用迁移
            Console.WriteLine("正在应用数据库迁移...");
            context.Database.Migrate();
            Console.WriteLine("数据库迁移完成");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "应用数据库迁移时出错");
            
            // 重试几次，允许MySQL容器完全启动
            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    Console.WriteLine($"重试迁移 ({retry+1}/5)...");
                    Thread.Sleep(10000); // 等待10秒
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    context.Database.Migrate();
                    Console.WriteLine("数据库迁移成功");
                    break;
                }
                catch (Exception retryEx)
                {
                    logger.LogError(retryEx, $"重试迁移失败 ({retry+1}/5)");
                    if (retry == 4) // 最后一次重试也失败
                    {
                        Console.WriteLine("无法连接到数据库，请确保MySQL容器正在运行且配置正确");
                    }
                }
            }
        }
    }
}
