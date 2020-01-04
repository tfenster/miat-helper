cd app
dotnet restore "./client.csproj"
dotnet build "client.csproj" -c Release -o /app/build
exit