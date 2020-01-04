cd app
dotnet publish "client.csproj" -c Release -r win-x64 --self-contained true /property:PublishSingleFile=true /property:PublishTrimmed=true -o /app/publish
exit