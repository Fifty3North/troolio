FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Sample/ShoppingListSample/Sample.Api/Sample.Api.csproj -c Release -o /app --nologo -p:UseSharedCompilation=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://+:8081 Shopping__EmbeddedHost=true Shopping__ReadModels__Path=/data/readmodels.db
EXPOSE 8081
ENTRYPOINT ["dotnet", "Sample.Api.dll"]
