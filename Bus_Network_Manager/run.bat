@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo [1/2] Dang khoi dong BusBooking.Server (http://localhost:5000)...
start "BusBooking Server" cmd /k "dotnet run --project BusBooking.Server --launch-profile http"

echo [2/2] Dang cho Server san sang va mo BusBooking.Client...
timeout /t 4 /nobreak >nul
start "BusBooking Client" cmd /c "dotnet run --project BusBooking.Client"

exit /b 0
