cd app
dotnet restore "./service.csproj"
dotnet build "service.csproj" -c Release -o /app/build
exit