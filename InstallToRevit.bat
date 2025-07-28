@echo off
echo ============================================
echo MEP Connector Auto Installer
echo ============================================
echo.

set "year=%1"
if "%year%"=="" (
    echo Usage: InstallToRevit.bat [year]
    echo Example: InstallToRevit.bat 2024
    echo.
    echo Available years: 2020, 2021, 2022, 2023, 2024, 2025, 2026
    echo.
    pause
    goto :end
)

set "addinPath=%APPDATA%\Autodesk\Revit\Addins\%year%"
set "dllFile=bin\Release%year%\MEPConnector%year%.dll"
set "addinFile=Addins\MEPConnector%year%.addin"

echo Installing MEP Connector for Revit %year%...
echo.

REM Check if DLL file exists
if not exist "%dllFile%" (
    echo ERROR: %dllFile% not found!
    echo Please build the project first using BuildAllVersions.bat
    echo.
    pause
    goto :end
)

REM Check if ADDIN file exists
if not exist "%addinFile%" (
    echo ERROR: %addinFile% not found!
    echo.
    pause
    goto :end
)

REM Create Revit addins directory if it doesn't exist
if not exist "%addinPath%" (
    echo Creating directory: %addinPath%
    mkdir "%addinPath%"
)

REM Copy files
echo Copying %dllFile% to %addinPath%\
copy "%dllFile%" "%addinPath%\" /Y
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy DLL file
    pause
    goto :end
)

echo Copying %addinFile% to %addinPath%\
copy "%addinFile%" "%addinPath%\" /Y
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy ADDIN file
    pause
    goto :end
)

echo.
echo ============================================
echo Installation completed successfully!
echo ============================================
echo.
echo Files installed to: %addinPath%
echo - MEPConnector%year%.dll
echo - MEPConnector%year%.addin
echo.
echo Please restart Revit %year% to load the add-in.
echo You should see "MEP Connector" tab in the ribbon.
echo.
pause

:end
