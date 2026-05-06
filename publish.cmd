@echo off
echo Building TopekaIT Portal (self-contained, win-x64)...
echo.
dotnet publish src\TopekaIT.Web\TopekaIT.Web.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:ReadyToRun=true ^
  -o publish\TopekaITPortal
echo.
if %ERRORLEVEL% NEQ 0 (
  echo ERROR: Publish failed. See output above.
  exit /b %ERRORLEVEL%
)
echo Done. Output is in publish\TopekaITPortal\
echo.
echo Target machine requirement: SQL Server LocalDB 2019 or 2022
echo Download: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
echo (Choose Express edition, which includes LocalDB)
