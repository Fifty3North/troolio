#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/ShoppingListSample/Sample.Silo/Sample.Silo.csproj", "Sample/ShoppingListSample/Sample.Silo/"]
COPY ["Sample/ShoppingListSample/Sample.Server/Sample.Server.csproj", "Sample/ShoppingListSample/Sample.Server/"]
COPY ["Sample/ShoppingListSample/Sample.Shared/Sample.Shared.csproj", "Sample/ShoppingListSample/Sample.Shared/"]
COPY ["Troolio.Core/Troolio.Core.csproj", "Troolio.Core/"]
COPY ["Troolio.Stores.EventStore/Troolio.Stores.EventStore.csproj", "Troolio.Stores.EventStore/"]
RUN dotnet restore "Sample/ShoppingListSample/Sample.Silo/Sample.Silo.csproj"
COPY . .
WORKDIR "/src/Sample/ShoppingListSample/Sample.Silo"
RUN dotnet build "Sample.Silo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.Silo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Silo.dll"]