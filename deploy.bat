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
    goto :end
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
xcopy /Y "%SOURCE%\RedoxLib.*" "%DEST%\" || exit /b 1
xcopy /Y "%SOURCE%\RedoxFilter.*" "%DEST%\" || exit /b 1
xcopy /Y "%SOURCE%\RedoxExtensions.Wrapper.*" "%DEST%\" || (
    echo.
    echo WARNING: RedoxExtensions.Wrapper not copied ^(in use by Decal?^)
    echo.
)
exit /b 0

:buildfailed
echo.
echo Build failed, nothing was copied
echo.
pause
exit /b 1

:copyfailed
echo.
echo Copy failed
echo.
pause
exit /b 1

:end
pause
