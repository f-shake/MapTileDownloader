dotnet publish MapTileDownloader.UI --runtime linux-x64 -c Release -o Publish/linux-x64 /p:PublishAOT=false /p:PublishSingleFile=true
Invoke-Item Publish/linux-x64