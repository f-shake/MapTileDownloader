$publishPath = [System.IO.Path]::Combine($env:TEMP, "TypoChecker_Publish", "win-x64")
dotnet publish TypoChecker.UI.Desktop -r win-x64 -c Release -o $publishPath
explorer $publishPath