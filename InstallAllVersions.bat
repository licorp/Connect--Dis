@echo off
echo ============================================
echo MEP Connector - Install All Versions
echo ============================================
echo.

echo This script will install MEP Connector to all available Revit versions on your system.
echo.
echo Press any key to continue, or Ctrl+C to cancel...
pause >nul
echo.

REM Check and install for each Revit version
for %%v in (2020 2021 2022 2023 2024 2025 2026) do (
    set "addinPath=%APPDATA%\Autodesk\Revit\Addins\%%v"
    set "dllFile=bin\Release%%v\MEPConnector%%v.dll"
    set "addinFile=Addins\MEPConnector%%v.addin"
    
    echo Checking Revit %%v...
    
    REM Check if Revit addins folder exists (indicating Revit is installed)
    if exist "%APPDATA%\Autodesk\Revit\Addins\%%v" (
        echo   Revit %%v detected
        
        REM Check if our build exists
        if exist "%%dllFile%%" (
            echo   Installing MEP Connector for Revit %%v...
            
            REM Copy DLL
            copy "%%dllFile%%" "%%addinPath%%\" /Y >nul
            if !errorlevel! equ 0 (
                echo   ✓ DLL installed
            ) else (
                echo   ✗ Failed to install DLL
                goto :continue
            )
            
            REM Copy ADDIN
            copy "%%addinFile%%" "%%addinPath%%\" /Y >nul
            if !errorlevel! equ 0 (
                echo   ✓ ADDIN file installed
                echo   ✓ Revit %%v installation completed
            ) else (
                echo   ✗ Failed to install ADDIN file
            )
        ) else (
            echo   ✗ Build not found for Revit %%v - please run BuildAllVersions.bat first
        )
    ) else (
        echo   Revit %%v not installed
    )
    echo.
    :continue
)

echo ============================================
echo Installation process completed!
echo ============================================
echo.
echo Please restart any running Revit instances to load the add-in.
echo You should see "MEP Connector" tab in the ribbon.
echo.
echo If you don't see the add-in, check:
echo 1. The correct files are in the Revit Addins folder
echo 2. Windows Event Viewer for any loading errors
echo 3. Revit version compatibility
echo.
pause
