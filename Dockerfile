# Temporary Build image
# https://mcr.microsoft.com/v2/dotnet/core/sdk/tags/list
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-alpine AS build
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
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.3-alpine
WORKDIR /app
COPY --from=publish /app/src/out .
ENTRYPOINT ["dotnet", "MyMonthlyBlogArticle.Bot.dll"]
