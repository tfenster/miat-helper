New-Item "c:\miat-helper" -ItemType Directory 
New-Item "c:\miat-helper\bin" -ItemType Directory 

$latestTag = (Invoke-WebRequest -UseBasicParsing -Uri "https://api.github.com/repos/tfenster/miat-helper/releases/latest" | ConvertFrom-Json).tag_name
Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/tfenster/miat-helper/releases/download/$latestTag/client.exe" -OutFile "c:\miat-helper\bin\client.exe"
Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/tfenster/miat-helper/releases/download/$latestTag/service.exe" -OutFile "c:\miat-helper\bin\service.exe"

New-Service -Name "MIAT-helper" -BinaryPathName "c:\miat-helper\bin\service.exe --folder c:\miat-helper" -StartupType Automatic -DisplayName "Managed Instance Access Token helper"
Start-Service "MIAT-helper"