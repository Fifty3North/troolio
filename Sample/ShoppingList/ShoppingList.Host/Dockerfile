#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/ShoppingList/ShoppingList.Database/ShoppingList.Database.csproj", "Sample/ShoppingList/ShoppingList.Database/"]
COPY ["Sample/ShoppingList/ShoppingList.Host/ShoppingList.Host.csproj", "Sample/ShoppingList/ShoppingList.Host/"]
COPY ["Sample/ShoppingList/ShoppingList.Host.App/ShoppingList.Host.App.csproj", "Sample/ShoppingList/ShoppingList.Host.App/"]
COPY ["Sample/ShoppingList/ShoppingList.Shared/ShoppingList.Shared.csproj", "Sample/ShoppingList/ShoppingList.Shared/"]
RUN dotnet restore "Sample/ShoppingList/ShoppingList.Host/ShoppingList.Host.csproj"
COPY . .
WORKDIR "/src/Sample/ShoppingList/ShoppingList.Host"
RUN dotnet build "ShoppingList.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShoppingList.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShoppingList.Host.dll"]