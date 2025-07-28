@echo off
echo ============================================
echo Building MEP Connector for All Revit Versions
echo ============================================
echo.

REM Build for Revit 2020
echo Building for Revit 2020...
dotnet build MEPConnector.csproj --configuration Release2020 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2020
    goto :error
)
echo Revit 2020 build completed successfully.
echo.

REM Build for Revit 2021
echo Building for Revit 2021...
dotnet build MEPConnector.csproj --configuration Release2021 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2021
    goto :error
)
echo Revit 2021 build completed successfully.
echo.

REM Build for Revit 2022
echo Building for Revit 2022...
dotnet build MEPConnector.csproj --configuration Release2022 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2022
    goto :error
)
echo Revit 2022 build completed successfully.
echo.

REM Build for Revit 2023
echo Building for Revit 2023...
dotnet build MEPConnector.csproj --configuration Release2023 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2023
    goto :error
)
echo Revit 2023 build completed successfully.
echo.

REM Build for Revit 2024
echo Building for Revit 2024...
dotnet build MEPConnector.csproj --configuration Release2024 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2024
    goto :error
)
echo Revit 2024 build completed successfully.
echo.

REM Build for Revit 2025
echo Building for Revit 2025...
dotnet build MEPConnector.csproj --configuration Release2025 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2025
    goto :error
)
echo Revit 2025 build completed successfully.
echo.

REM Build for Revit 2026
echo Building for Revit 2026...
dotnet build MEPConnector.csproj --configuration Release2026 --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build for Revit 2026
    goto :error
)
echo Revit 2026 build completed successfully.
echo.

echo ============================================
echo All builds completed successfully!
echo ============================================
echo.
echo Build outputs:
echo - Revit 2020: bin\Release2020\MEPConnector2020.dll
echo - Revit 2021: bin\Release2021\MEPConnector2021.dll
echo - Revit 2022: bin\Release2022\MEPConnector2022.dll
echo - Revit 2023: bin\Release2023\MEPConnector2023.dll
echo - Revit 2024: bin\Release2024\MEPConnector2024.dll
echo - Revit 2025: bin\Release2025\MEPConnector2025.dll
echo - Revit 2026: bin\Release2026\MEPConnector2026.dll
echo.
echo Add-in files are in the Addins\ folder
echo.
pause
goto :end

:error
echo.
echo ============================================
echo BUILD FAILED!
echo ============================================
echo Please check the error messages above.
echo Make sure all Revit versions are installed, or modify the .csproj file
echo to remove references to versions you don't have installed.
echo.
pause

:end
