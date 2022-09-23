dotnet msbuild /t:UnityEditModeTest /p:Configuration=Release
dotnet msbuild /t:UnityPlayModeTest /p:Configuration=Release
dotnet msbuild /t:UnitySmokeTestStandalonePlayerIL2CPP /p:Configuration=Release

// test
