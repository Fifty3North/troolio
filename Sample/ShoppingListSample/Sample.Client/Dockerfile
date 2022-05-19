#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/ShoppingListSample/Sample.Client/Sample.Client.csproj", "Sample/ShoppingListSample/Sample.Client/"]
COPY ["Sample/ShoppingListSample/Sample.Shared/Sample.Shared.csproj", "Sample/ShoppingListSample/Sample.Shared/"]
COPY ["Troolio.Core/Troolio.Core.csproj", "Troolio.Core/"]
RUN dotnet restore "Sample/ShoppingListSample/Sample.Client/Sample.Client.csproj"
COPY . .
WORKDIR "/src/Sample/ShoppingListSample/Sample.Client"
RUN dotnet build "Sample.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Client.dll"]