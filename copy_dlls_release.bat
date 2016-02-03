@echo off

set CONFIGURATION=Release

if exist "%REDOX_EXTENSIONS_INSTALL%" (
    xcopy /EYI bin\%CONFIGURATION%\RedoxExtensions.* "%REDOX_EXTENSIONS_INSTALL%"
    xcopy /EYI bin\%CONFIGURATION%\RedoxLib.* "%REDOX_EXTENSIONS_INSTALL%"
    xcopy /EYI bin\%CONFIGURATION%\RedoxFilter.* "%REDOX_EXTENSIONS_INSTALL%"

) else if exist "D:\Games\Decal Plugins\RedoxExtensions" (

    xcopy /EYI bin\%CONFIGURATION%\RedoxExtensions.* "D:\Games\Decal Plugins\RedoxExtensions\"
    xcopy /EYI bin\%CONFIGURATION%\RedoxLib.* "D:\Games\Decal Plugins\RedoxExtensions\"
    xcopy /EYI bin\%CONFIGURATION%\RedoxFilter.* "D:\Games\Decal Plugins\RedoxExtensions\"
) else if exist "C:\Games\Decal Plugins\RedoxExtensions" (

    xcopy /EYI bin\%CONFIGURATION%\RedoxExtensions.* "C:\Games\Decal Plugins\RedoxExtensions\"
    xcopy /EYI bin\%CONFIGURATION%\RedoxLib.* "C:\Games\Decal Plugins\RedoxExtensions\"
    xcopy /EYI bin\%CONFIGURATION%\RedoxFilter.* "C:\Games\Decal Plugins\RedoxExtensions\"
) else (
    echo.
    echo No Redox Extensions Install Directory Found
    echo.
)

pause