[![Build Status](https://dev.azure.com/mabenoit-ms/MyOwnBacklog/_apis/build/status/MyMonthlyBlogArticle.Bot?branchName=master)](https://dev.azure.com/mabenoit-ms/MyOwnBacklog/_build/latest?definitionId=111&branchName=master)

# MyMonthlyBlogArticle.Bot

This repository contains all the code and deployment scripts related to this project: [My monthly "Azure News & Updates" blog article got a Bot!](https://alwaysupalwayson.blogspot.com/2018/04/my-monthly-azure-news-updates-blog.html) created on April 2018. This repository got actually updated with a more Cloud Native App approach by leveraging associated latest and greatest technologies and features such as Docker, ASP.NET Core 3.1, Helm 3, Kubernetes and Terraform, check this entire story here: [My Bot just got powered by .NET Core 3.1, Docker, Kubernetes and Terraform](https://alwaysupalwayson.blogspot.com/2019/12/my-bot-just-got-powered-by-net-core-31.html).

![Flow & Architecture diagram](./FlowAndArchitecture.PNG "Flow & Architecture diagram")

# Prerequisities

You need an Azure Kubernetes Service (AKS) and the recommended K8S version is 1.15.7+ since the mecanism to set the DNS name on the Public Azure IP Address got working back since then.

You need to register your Bot Application in https://apps.dev.microsoft.com. From there, you should get 2 values for the deployment process below: `Application Id` and `Application Secret`.

# Deploy

You could deploy this bot locally with VS Code or Visual Studio or even `dotnet` command line. Other ways explained below are to deploy it with `Docker` or `Helm`.

```
botName=<bot-name>
appId=<app-guid>
appSecret=<app-secret>
azureSearchServiceName=<azure-search-service-name>
azureSearchIndexName=<azure-search-index-name>
azureSearchServiceQueryApiKey=<azure-search-service-query-api-key>
appInsightsInstrumentationKey=<app-insights-instrumentation-key>
```

## Deploy with Docker

```
docker build \
    -t $botName \
    '
#TODO: docker run -p 80:80 $botName
```

You could now expose your Docker image in a Container registry, with the command `docker push`.

## Deploy with Helm

As a prerequisities, you need to install `cert-manager`:
```
# Install cert-manager
kubectl apply -f https://raw.githubusercontent.com/jetstack/cert-manager/release-0.13/deploy/manifests/00-crds.yaml
kubectl create namespace cert-manager
kubectl label namespace cert-manager certmanager.k8s.io/disable-validation=true
helm repo add jetstack https://charts.jetstack.io
helm repo update
helm install \
    cert-manager \
    jetstack/cert-manager \
    -n cert-manager \
    --version v0.13.0
```

You could then run the `helm upgrade` command below against your Kuberentes cluster:

```
registryName=<registry-name>
k8sNamespace=<k8s-namespace-name>
dnsName=<custom-dns-name>
hostName=$dnsName.eastus.cloudapp.azure.com # could depend on your DNS, in my case that's the DNS on the Azure IP Address hosted in EastUS.
issuerEmail=<your-email-for-certificate>

kubectl create namespace $k8sNamespace

# Install MyMonthlyBlogArticle.Bot
cd chart
helm dependencies update
helm upgrade \
    --namespace $k8sNamespace \
    --install \
    --wait \
    --set image.repository=$registryName/$botName \
    --set image.env.microsoftAppId=$appId \
    --set image.env.microsoftAppPassword=$appPassword \
    --set image.env.appInsights.instrumentationKey=$appInsightsInstrumentationKey \
    --set image.env.search.serviceName=$azureSearchServiceName \
    --set image.env.search.indexName=$azureSearchIndexName \
    --set image.env.search.serviceQueryApiKey=$azureSearchServiceQueryApiKey \
    --set ingress.hostName=$hostName \
    --set issuer.acme.email=$issuerEmail \
    --set nginx-ingress.defaultBackend.enabled=false \
    --set nginx-ingress.controller.image.tag=0.27.1 \
    --set nginx-ingress.controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=$dnsName \
    $botName \
    .
```

# Application Insights

Once the Azure Bot Service deployed you could leverage the Bot Analytics feature with Application Insights like described [here](https://docs.microsoft.com/azure/bot-service/bot-service-manage-analytics).

Additionally to that, you could perform different queries to retrieve information logged from the Bot Framework into Application Insights where additional telemetry has been setup like described [here](https://docs.microsoft.com/azure/bot-service/bot-builder-telemetry).

Get all the requests:
```
requests
| order by timestamp desc
```

Get all the requests about initializing conversations:
```
requests
| where tostring(customDimensions.activityType) == "conversationUpdate"
| order by timestamp desc
```

Get all the requests about messages sent:
```
requests
| where tostring(customDimensions.activityType) == "message"
| order by timestamp desc
```

Display on a time chart the durations of the message requests:
```
requests
| where tostring(customDimensions.activityType) == "message"
| project duration, timestamp, appId
| render timechart
```

Get all the exceptions:
```
exceptions
| order by timestamp desc
```

Get all the customEvents:
```
customEvents
| order by timestamp desc
```

Count of search by query term:
```
customEvents
| where name == "BotMessageReceived" 
| summarize count() by tostring(customDimensions.text)
```
