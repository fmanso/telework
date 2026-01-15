@echo off
echo ========================================
echo Building Telework Tracker Desktop App
echo ========================================

echo.
echo Step 1: Publishing .NET application (self-contained)...
set "PROJECT=%~dp0TeleworkApp\TeleworkApp.csproj"
set "PUBLISHDIR=%~dp0electron\dotnet"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishDir="%PUBLISHDIR%\\"
if errorlevel 1 (
    echo ERROR: Failed to publish .NET application
    pause
    exit /b 1
)

echo.
echo Step 2: Installing Electron dependencies...
cd /d "%~dp0electron"
call npm install
if errorlevel 1 (
    echo ERROR: Failed to install npm dependencies
    pause
    exit /b 1
)

echo.
echo Step 3: Building Electron installer...

REM Ensure no running Electron/Telework processes are locking dist output
for %%p in ("Telework Tracker.exe" TeleworkApp.exe electron.exe) do (
    taskkill /F /IM %%p >nul 2>&1
)

REM Ensure no leftover listening process is holding the port
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5123 ^| findstr LISTENING 2^>nul') do (
    taskkill /F /PID %%a >nul 2>&1
)

REM Clean previous build output to avoid locked files
if exist "%~dp0electron\dist" rmdir /s /q "%~dp0electron\dist"

call npm run build
if errorlevel 1 (
    echo ERROR: Failed to build Electron app
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo Installer is located in: electron\dist
echo ========================================
pause
