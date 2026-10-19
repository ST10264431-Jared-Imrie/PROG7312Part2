@echo off
title SmartX API Gateway Launcher
set "PATH=C:\Program Files\dotnet;%PATH%"
echo Starting SmartX API Gateway on http://localhost:5000...
cd /d "%~dp0"
dotnet run --project SmartX.API/SmartX.API.csproj --urls http://localhost:5000
