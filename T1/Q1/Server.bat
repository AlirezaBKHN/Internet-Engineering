@echo off
cd /d "%~dp0Server"
dotnet build
dotnet run
pause
