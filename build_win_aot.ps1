dotnet publish MapTileDownloader.UI.Desktop  --runtime win-x64 -c Release -o Publish/win-x64-aot
rm Publish/win-x64-aot/*.pdb
Invoke-Item Publish/win-x64-aot