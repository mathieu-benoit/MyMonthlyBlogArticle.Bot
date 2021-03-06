# Temporary Build image
# https://mcr.microsoft.com/v2/dotnet/core/sdk/tags/list
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.301-alpine AS build
WORKDIR /app
COPY *.sln .
COPY src/*.csproj ./src/
COPY test/*.csproj ./test/
RUN dotnet restore
COPY . .

# Run Unit Tests image
FROM build AS unittests
WORKDIR /app/test
RUN dotnet test --logger:trx

# Temporary Publish image
FROM build AS publish
WORKDIR /app/src
RUN dotnet publish -c Release -o out --no-restore

# Runtime image
# https://mcr.microsoft.com/v2/dotnet/core/aspnet/tags/list
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.5-alpine
ENV ASPNETCORE_URLS=http://+:5000
# OPT OUT OF Diagnostic pipeline so we can run readonly.
ENV COMPlus_EnableDiagnostics=0
WORKDIR /app
COPY --from=publish /app/src/out .
ENTRYPOINT ["dotnet", "MyMonthlyBlogArticle.Bot.dll"]
