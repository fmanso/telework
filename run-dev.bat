@echo off
echo ========================================
echo Running Telework Tracker in Dev Mode
echo ========================================

REM Kill any existing processes on port 5123 first
echo Checking for existing processes on port 5123...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5123 ^| findstr LISTENING 2^>nul') do (
    echo Killing existing process on port 5123 (PID %%a)
    taskkill /F /PID %%a >nul 2>&1
)

echo.
echo Building .NET application...
cd /d "%~dp0TeleworkApp"
dotnet build
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Installing Electron dependencies if needed...
cd /d "%~dp0electron"
if not exist "node_modules" (
    call npm install
)

echo.
echo Starting Electron (will launch .NET server automatically)...
echo Press Ctrl+C or close the window to stop.
echo.
call npx electron . --dev

echo.
echo Application closed. Cleaning up...

REM Clean up any orphaned processes
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5123 ^| findstr LISTENING 2^>nul') do (
    echo Killing orphaned process (PID %%a)
    taskkill /F /PID %%a >nul 2>&1
)

echo Done.
