
using System.Text.Json;
using AngleSharp;
using BizStreamAIAssistant.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IChatbotService, ChatbotService>(); 
builder.Services.Configure<AzureOpenAISettingsModel>(
    builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var webIndexingSettings = builder.Configuration
    .GetSection("WebIndexingSettings")
    .Get<WebIndexingSettingsModel>();

if (webIndexingSettings == null)
{
    throw new InvalidOperationException("WebIndexingSettings not configured.");
}
    
var rootUrl = webIndexingSettings.RootUrl;
var depth = webIndexingSettings.Depth;
var indexer = new WebIndexingService();
await indexer.CrawlAndExtractAsync(rootUrl, depth);



var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();