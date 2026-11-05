@echo off
setlocal
cd /d "%~dp0"

set CONFIGURATION=%~1
if "%CONFIGURATION%"=="" set CONFIGURATION=Debug
set SOURCE=bin\x86\%CONFIGURATION%

dotnet build RedoxExtensions.slnx -c %CONFIGURATION% -p:PlatformTarget=x86
if errorlevel 1 goto :buildfailed

if exist "%REDOX_EXTENSIONS_INSTALL%" (
    call :copyto "%REDOX_EXTENSIONS_INSTALL%" yes yes
) else if exist "D:\Games\Decal Plugins\RedoxExtensions" (
    call :copyto "D:\Games\Decal Plugins\RedoxExtensions" yes no
) else if exist "C:\Games\Decal Plugins\RedoxExtensions" (
    call :copyto "C:\Games\Decal Plugins\RedoxExtensions" yes no
) else if exist "E:\ac\Games\RedoxExtensions" (
    call :copyto "E:\ac\Games\RedoxExtensions" no no
) else (
    echo.
    echo No Redox Extensions Install Directory Found
    echo.
    exit /b 1
)
if errorlevel 1 goto :copyfailed
goto :end

rem Usage: call :copyto <install dir> <copy Databases yes/no> <copy mysettings.json yes/no>
:copyto
set DEST=%~1
if "%~2"=="yes" (
    xcopy /EYI "%SOURCE%\Databases" "%DEST%\Databases" || exit /b 1
)
if "%~3"=="yes" (
    xcopy /Y "%SOURCE%\mysettings.json" "%DEST%\" || exit /b 1
)
xcopy /Y "%SOURCE%\RedoxExtensions.dll" "%DEST%\" || exit /b 1
xcopy /Y "%SOURCE%\RedoxExtensions.pdb" "%DEST%\" || exit /b 1
rem These stay loaded (locked) while a client runs.  /D skips them when they weren't rebuilt, but Debug builds aren't
rem deterministic (full PDBs), so any rebuild looks like a change.  Warn instead of failing; the plugin reload reports
rem a load error if RedoxLib really changed.
xcopy /Y /D "%SOURCE%\RedoxLib.*" "%DEST%\" || (
    echo.
    echo WARNING: RedoxLib not copied ^(in use by a running client^).  If RedoxLib changed, restart the clients.
    echo.
)
xcopy /Y /D "%SOURCE%\RedoxFilter.*" "%DEST%\" || (
    echo.
    echo WARNING: RedoxFilter not copied ^(in use by a running client^)
    echo.
)
xcopy /Y /D "%SOURCE%\RedoxExtensions.Wrapper.*" "%DEST%\" || (
    echo.
    echo WARNING: RedoxExtensions.Wrapper not copied ^(in use by Decal?^)
    echo.
)
exit /b 0

:buildfailed
echo.
echo Build failed, nothing was copied
echo.
exit /b 1

:copyfailed
echo.
echo Copy failed
echo.
exit /b 1

:end
