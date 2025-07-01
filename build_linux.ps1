$publishPath = [System.IO.Path]::Combine($env:TEMP, "TypoChecker_Publish", "linux-x64")
dotnet publish TypoChecker.UI.Desktop -r linux-x64 -c Release -o $publishPath  /p:PublishAot=false /p:PublishSingleFile=true --self-contained true
explorer $publishPath