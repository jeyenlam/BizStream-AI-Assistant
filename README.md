# BizStream-AI-Assistant

## How to Run
**DotEnv Setup (optional if you want to interact with the bot)**
1. Create an .env file in ./BizStreamAIAssistant
2. Add the followings in the file
```
AZUREOPENAI__ENDPOINT=your-azure-openai-endpoint
AZUREOPENAI__APIKEY=your-azure-openai-api-key
```

**Run Cloned Website**
1. In ./BizStreamWebsiteClone, run:
```
npm i (run on the 1st time only)
dotnet watch
```

**Run Chatbot (run side by side with the cloned website)**
1. In ./BizStreamAIAssistant
```
npm i (run on the 1st time only)
dotnet watch
```
