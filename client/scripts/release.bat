cd app
dotnet publish "client.csproj" -c Release -r win-x64 --self-contained true /property:PublishTrimmed=true /property:PublishSingleFile=true /property:Version=%1 -o /app/publish
exit