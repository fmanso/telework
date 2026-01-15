@echo off
echo Killing any processes using port 5123...

REM Kill processes using port 5123
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5123 ^| findstr LISTENING') do (
    echo Killing PID %%a
    taskkill /F /PID %%a 2>nul
)

REM Kill any TeleworkApp processes
taskkill /F /IM TeleworkApp.exe 2>nul

REM Kill any dotnet processes running our app (be careful - this kills all dotnet)
REM Uncomment if needed:
REM taskkill /F /IM dotnet.exe 2>nul

echo Done. Port 5123 should be free now.
pause
