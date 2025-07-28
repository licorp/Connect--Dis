@echo off
echo Installing MEP Connector Add-in for Revit 2025...
echo.

REM Kiểm tra xem file dll đã build chưa
if not exist "bin\Release2025\net48\MEPConnector2025.dll" (
    echo Error: MEPConnector2025.dll not found!
    echo Please build the project first using: dotnet build MEPConnector.csproj --configuration Release2025
    echo Or run BuildAllVersions.bat to build all versions
    pause
    exit /b 1
)

REM Tạo thư mục đích nếu chưa có
set DEST_DIR=%APPDATA%\Autodesk\Revit\Addins\2025
if not exist "%DEST_DIR%" (
    echo Creating directory: %DEST_DIR%
    mkdir "%DEST_DIR%"
)

REM Copy files
echo Copying files...
copy "bin\Release2025\net48\MEPConnector2025.dll" "%DEST_DIR%\"
copy "Addins\MEPConnector2025.addin" "%DEST_DIR%\"

if %errorlevel% equ 0 (
    echo.
    echo Installation completed successfully!
    echo Files copied to: %DEST_DIR%
    echo.
    echo Please restart Revit to see the MEP Connector tab.
) else (
    echo.
    echo Installation failed!
    echo Please check if Revit is closed and try again.
)

echo.
pause
