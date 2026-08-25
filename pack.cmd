@echo off
rem ---------------------------------------------------------------------------
rem  Bumps Taciturn's package version, builds, runs tests, and packs the
rem  NuGet package (Release).
rem  Usage: pack.cmd [patch|minor|major] [quiet]   (default: patch)
rem  Output: src\Taciturn\bin\Release\Taciturn.<version>.nupkg
rem ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

set BUMP=%~1
if "%BUMP%"=="" set BUMP=patch

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo Cannot find "dotnet".
    echo.
    echo Taciturn needs the .NET SDK. Install it from:
    echo     https://dotnet.microsoft.com/download
    echo.
    if not "%~2"=="quiet" pause
    exit /b 1
)

echo Bumping version ^(%BUMP%^)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Set-Version.ps1" -Bump %BUMP%
if errorlevel 1 (
    echo.
    echo Version bump failed - see the message above.
    echo.
    if not "%~2"=="quiet" pause
    exit /b 1
)

echo.
echo Building ^(Release^)...
dotnet build Taciturn.slnx -c Release --nologo
if errorlevel 1 (
    echo.
    echo BUILD FAILED - see the messages above.
    echo The version in src\Taciturn\Taciturn.csproj was already bumped;
    echo fix the error and run pack.cmd again, or revert that change if you
    echo want to retry from a clean version.
    echo.
    if not "%~2"=="quiet" pause
    exit /b 1
)

echo.
echo Running tests...
dotnet test Taciturn.slnx -c Release --no-build --nologo
if errorlevel 1 (
    echo.
    echo TESTS FAILED - not packing. See the messages above.
    echo The version in src\Taciturn\Taciturn.csproj was already bumped;
    echo fix the failure and run pack.cmd again, or revert that change.
    echo.
    if not "%~2"=="quiet" pause
    exit /b 1
)

echo.
echo Packing...
dotnet pack src\Taciturn\Taciturn.csproj -c Release --no-build --nologo
if errorlevel 1 (
    echo.
    echo PACK FAILED - see the messages above.
    echo.
    if not "%~2"=="quiet" pause
    exit /b 1
)

echo.
echo Done. Package is in src\Taciturn\bin\Release\
echo.
echo Next step: publish it -
echo   dotnet nuget push src\Taciturn\bin\Release\Taciturn.^<version^>.nupkg --api-key ^<your-key^> --source https://api.nuget.org/v3/index.json
echo.
if not "%~2"=="quiet" pause
exit /b 0
