# MyMonthlyBlogArticle.Bot

# Prerequesities

You need to register first your Bot Application in https://apps.dev.microsoft.com. From there, you should get 2 values for the deployment process below: `Application Id` and `Application Secret`.

TIPS: you could put these values in [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-whatis) for more security and to be able to automate more.

# Deploy

## Deploy via the Azure portal

[![Deploy to Azure](http://azuredeploy.net/deploybutton.svg)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmathieu-benoit%2FMyMonthlyBlogArticle.Bot%2Fmaster%2Fazure-deploy.json)

## Deploy via CLI

```
RG=<your-existing-resource-group-name>
BotName=<bot-name>
AppId=<your-app-guid>
AppSecret=<your-app-secret>

az group deployment create \
  -g $RG \
  --template-file azure-deploy.json 
  --parameters botName=$BotName appSecret=$AppSecret appId=$AppId
```
# Manual setup

You could enable the Bot Analytics feature with Application Insights like described [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-analytics).