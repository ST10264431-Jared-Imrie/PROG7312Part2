@echo off
title SmartX Dashboard Launcher
set "PATH=C:\Program Files\dotnet;%PATH%"
echo Starting SmartX Dashboard...
cd /d "%~dp0"
dotnet run --project SmartX.Dashboard/SmartX.Dashboard.csproj
