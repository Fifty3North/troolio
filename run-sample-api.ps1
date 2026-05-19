$env:Shopping__Clustering__Storage = 'Local'
$env:ASPNETCORE_URLS = 'http://localhost:8081'
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --project Sample\ShoppingListSample\Sample.Api\Sample.Api.csproj --no-build