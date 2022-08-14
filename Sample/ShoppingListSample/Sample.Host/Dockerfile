#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/ShoppingListSample/Sample.Database/Sample.Database.csproj", "Sample/ShoppingListSample/Sample.Database/"]
COPY ["Sample/ShoppingListSample/Sample.Host/Sample.Host.csproj", "Sample/ShoppingListSample/Sample.Host/"]
COPY ["Sample/ShoppingListSample/Sample.Host.App/Sample.Host.App.csproj", "Sample/ShoppingListSample/Sample.Host.App/"]
COPY ["Sample/ShoppingListSample/Sample.Shared/Sample.Shared.csproj", "Sample/ShoppingListSample/Sample.Shared/"]
RUN dotnet restore "Sample/ShoppingListSample/Sample.Host/Sample.Host.csproj"
COPY . .
WORKDIR "/src/Sample/ShoppingListSample/Sample.Host"
RUN dotnet build "Sample.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Host.dll"]