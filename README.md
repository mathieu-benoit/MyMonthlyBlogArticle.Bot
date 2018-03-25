# MyMonthlyBlogArticle.Bot

# Deploy

## Deploy via the Azure portal

[![Deploy to Azure](http://azuredeploy.net/deploybutton.svg)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmathieu-benoit%2FMyMonthlyBlogArticle.Bot%2Fmaster%2Fazure-deploy.json)

## Deploy via CLI

```
RG=<your-existing-resource-group-name>
SiteName=<site-name>
AppId=<your-app-guid>
AppSecret=<your-app-secret>

az group deployment create \
  -g $RG \
  --template-file azure-deploy.json 
  --parameters siteName=$SiteName appSecret=$AppSecret appId=$AppId
```
