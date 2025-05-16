using System.Text.Json;
using BizStreamAIAssistant.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IChatbotService, ChatbotService>(); //added to register the ChatbotService as a service that can be injected into controllers
builder.Services.Configure<AzureOpenAISettingsModel>( //added to register the AzureOpenAISettingsModel with the dependency injection container
    builder.Configuration.GetSection("AzureOpenAI"));
// builder.Services.AddSession(); //added to register the session service with the dependency injection container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});


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
// app.UseSession(); //added to enable session state in the application
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
