#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/ShoppingListSample/ShoppingListSample.Api/ShoppingListSample.Api.csproj", "Sample/ShoppingListSample/ShoppingListSample.Api/"]
RUN dotnet restore "Sample/ShoppingListSample/ShoppingListSample.Api/ShoppingListSample.Api.csproj"
COPY . .
WORKDIR "/src/Sample/ShoppingListSample/ShoppingListSample.Api"
RUN dotnet build "ShoppingListSample.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShoppingListSample.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShoppingListSample.Api.dll"]