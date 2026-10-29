@echo off
setlocal
rem Builds the agent bridge CLI quietly (so stdout stays pure JSON), then runs it.
dotnet build "%~dp0RedoxExtensions.AgentCli" -c Debug -v q -nologo >nul 2>&1
if errorlevel 1 (
    echo rectl build failed.  Run: dotnet build RedoxExtensions.AgentCli 1>&2
    exit /b 2
)
dotnet "%~dp0RedoxExtensions.AgentCli\bin\Debug\net10.0\rectl.dll" %*
