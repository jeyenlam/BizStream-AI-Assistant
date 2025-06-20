using System.Text.Json;
using BizStreamAIAssistant.Services;

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
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TextEmbeddingService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<WebIndexingService>();
builder.Services.Configure<AzureOpenAISettingsModel>("Chat", builder.Configuration.GetSection("AzureOpenAIChatSettings"));
builder.Services.Configure<AzureOpenAISettingsModel>("TextEmbedding", builder.Configuration.GetSection("AzureOpenAITextEmbeddingSettings"));
builder.Services.Configure<AzureAISearchSettingsModel>(builder.Configuration.GetSection("AzureAISearchSettings"));
builder.Services.Configure<WebIndexingSettingsModel>(builder.Configuration.GetSection("WebIndexingSettings"));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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


// In-app Testing (uncomment each chunk at a time)
// 1. Crawling and extracting data from websites, saving that jsonl file to ./Data/Chunks/data.jsonl
// using var scope = app.Services.CreateScope();
// var webIndexingService = scope.ServiceProvider.GetRequiredService<WebIndexingService>();
// string jsonlFilePath = await webIndexingService.CrawlAndExtractAsync();
// Console.WriteLine($"Crawling completed. Data saved to: {jsonlFilePath}");

// 2. Generating embeddings from the jsonl file and uploading them to Azure AI Search
// using (var scope = app.Services.CreateScope())
// {
//     var textEmbeddingService = scope.ServiceProvider.GetRequiredService<TextEmbeddingService>();
//     await textEmbeddingService.UploadEmbeddingsAsync();
// }

app.Run();