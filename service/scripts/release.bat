cd app
dotnet publish "service.csproj" -c Release -r win-x64 --self-contained true /property:PublishSingleFile=true /property:PublishTrimmed=true /property:Version=%1 -o /app/publish
exit