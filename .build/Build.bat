call "%VS140COMNTOOLS%VsDevCmd.bat"
nuget.exe restore "..\Mono.Helpers.sln"
msbuild "..\Mono.Helpers.sln" /t:Clean /p:Configuration=Release 
msbuild "..\Mono.Helpers.sln" /p:Configuration=Release 
nuget.exe pack "..\Mono.Helpers\Mono.Helpers.nuspec" -OutputDirectory "..\Mono.Helpers\bin\Release" -symbols
