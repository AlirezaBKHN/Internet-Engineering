@echo off
REM Check if all arguments are provided
if "%1"=="" (
    echo Please provide the server IP as the first argument.
    exit /b
)

if "%2"=="" (
    echo Please provide the server port as the second argument.
    exit /b
)

if "%3"=="" (
    echo Please provide thread count as the third argument.
    exit /b
)

if "%4"=="" (
    echo Please provide the file path of server as the foruth argument
    exit /b
)
@REM if "%5"=="" (
@REM     echo Please provide the file path to save as the fifth argument.
@REM     exit /b
@REM )


REM Navigate to the client folder
cd /d "%~dp0Client"

REM Build the client application
dotnet build

REM Run the client application with server IP, server port, thread count, and file path as arguments

dotnet run -- %1 %2 %3 %4

pause
