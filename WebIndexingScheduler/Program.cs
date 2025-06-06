using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using BizStreamAIAssistant.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure settings from local.settings.json
        services.Configure<AzureAISearchSettingsModel>(configuration.GetSection("AzureAISearchSettings"));
        services.Configure<AzureOpenAISettingsModel>("TextEmbedding", configuration.GetSection("AzureOpenAITextEmbeddingSettings"));
        services.Configure<WebIndexingSettingsModel>(configuration.GetSection("WebIndexingSettings"));

        services.AddSingleton<WebIndexingService>();
        services.AddSingleton<TextEmbeddingService>();
    })
    .Build();

host.Run();
