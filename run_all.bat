@echo off
title SmartX-IoT-Mesh System Launcher
echo ========================================================
echo Starting Smart-X IoT Mesh System (Part 2)
echo ========================================================
echo.

:: Ensure dotnet is in PATH
set "PATH=C:\Program Files\dotnet;%PATH%"

echo [1/2] Starting SmartX API Gateway on http://localhost:5000...
start "SmartX API Gateway" cmd /k "dotnet run --project SmartX.API/SmartX.API.csproj --urls http://localhost:5000"

echo Waiting 3 seconds for API initialization...
timeout /t 3 /nobreak >nul

echo [2/2] Launching SmartX Dashboard WPF Console...
start "SmartX Dashboard" cmd /c "dotnet run --project SmartX.Dashboard/SmartX.Dashboard.csproj"

echo.
echo Both services launched successfully!
echo API Documentation: http://localhost:5000/swagger
echo.
pause
