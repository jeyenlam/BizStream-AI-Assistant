# BZSAI
BZSAI is a personal project developed during my internship at BizStream. It’s an AI assistant designed specifically for BizStream, aimed at enhancing the experience of website visitors. Instead of navigating through multiple pages to learn about the company, users can now interact with BZSAI to get instant, accurate answers about BizStream — making their visit more engaging and efficient.

## Table of Contents
- [Intro](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#bzsai)
- [Demo](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#demo)
- [Features](https://github.com/jeyenlam/Readsify?tab=readme-ov-file#features)
- [Project Structure](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#project-structure)
- [Built With](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#built-with)
- [Prerequisites](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#prerequisites)  
- [Architecture](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#architecture)
- [How to Run](https://github.com/jeyenlam/BizStream-AI-Assistant?tab=readme-ov-file#how-to-run)
- [License](https://github.com/jeyenlam/Readsify?tab=readme-ov-file#license)
  
## Demo

## Features
1. **Web Indexing**: Crawls the main BizStream website and its subdomains to extract content for use in Retrieval-Augmented Generation (RAG).
2. **Web Indexing Scheduler**:Automates periodic website crawling to ensure the indexed data remains accurate and up to date.
3. **Chatbot Widget**: A standalone AI assistant that can be embedded into the BizStream website to help visitors get instant answers about the company.
4. **BizStream Homepage Clone**: A replicated version of the BizStream homepage used for testing and development purposes.

## Project Structure
```
BizStream-AI-Assistant/
├─ BizStreamAIAssistant/
│  ├─ Controllers/
│  │  ├─ ChatController.cs
│  ├─ Services/
│  │  ├─ ChatService.cs
│  │  ├─ SearchService.cs
│  │  ├─ TextEmbeddingService.cs
│  │  ├─ WebIndexingService.cs
│  ├─ Views/
├─ BizStreamWebsiteClone/
├─ WebIndexingScheduler/
│  ├─ WebIndexingSchedulerFunction.cs
```

## Build With
- [ASP.NET MVC](https://dotnet.microsoft.com/en-us/apps/aspnet/mvc)
- [ASP.NET Web Pages](https://learn.microsoft.com/en-us/aspnet/web-pages/overview/getting-started/introducing-aspnet-web-pages-2/getting-started)
- [.NET Azure Function](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)
- [Azure Web Services](https://azure.microsoft.com/en-us/)
- [TailwindCSS](https://tailwindcss.com/)

## Prerequisites
1. Azure account (billing set up)
2. Azure Search Service Instance (with index, semantic config, vector profile setups)
3. Azure AI Foundry (with chat model and text embedding deployments)
4. Azurite elmulator installation (npm install -g azurite)

## How to Run
**1. Run Cloned Website**
1. In ./BizStreamWebsiteClone, open terminal, run:
```
npm i (1st time only)
dotnet watch
```

**2. Run Chatbot (run side by side with the cloned website)**
1. In ./BizStreamAIAssistant, create an .env file 
2. Add the followings in the file
```
AZUREOPENAICHATSETTINGS__ENDPOINT=your-azure-openai-chat-endpoint
AZUREOPENAICHATSETTINGS__APIKEY=your-azure-openai-chat-apikey
AZUREOPENAITEXTEMBEDDINGSETTINGS__ENDPOINT=your-azure-openai-text-embedding-endpoint
AZUREOPENAITEXTEMBEDDINGSETTINGS__APIKEY=your-azure-openai-text-embedding-apikey
AZUREAISEARCHSETTINGS__ENDPOINT=your-azure-search-endpoint
AZUREAISEARCHSETTINGS__APIKEY=your-azure-search-apikey
```
2. Open terminal, run:
```
npm i (1st time only)
dotnet watch
```

**3. Run Web Indexing Scheduler**
1. In ./WebIndexingScheduler, paste the followings to local.settings.json:
```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AzureOpenAITextEmbeddingSettings__Endpoint": "your-azure-openai-text-embedding-endpoint",
        "AzureOpenAITextEmbeddingSettings__ApiKey": "your-azure-openai-text-embedding-apikey",
        "AzureOpenAITextEmbeddingSettings__DeploymentName": "text-embedding-ada-002",
        "AzureOpenAITextEmbeddingSettings__Model": "text-embedding-ada-002",
        "AzureOpenAITextEmbeddingSettings__ApiVersion": "2023-05-15",
        "AzureAISearchSettings__Endpoint": "your-azure-aisearch-endpoint",
        "AzureAISearchSettings__ApiKey": "your-azure-aisearch-apikey",
        "AzureAISearchSettings__IndexName": "bzsai-embeddings-index",
        "WebIndexingSettings__RootUrl": "https://bizstream.com",
        "WebIndexingSettings__Depth": 1
    },
    "ConnectionStrings": {}
}
```
3. Open 2 terminals, run the followings respectively in each terminal:
```
azurite (azurite installation required)
func start
```

## License
```
```
