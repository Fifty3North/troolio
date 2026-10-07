FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Sample/ShoppingListSample/Sample.Host/Sample.Host.csproj -c Release -o /app --nologo -p:UseSharedCompilation=false
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Sample.Host.dll"]
