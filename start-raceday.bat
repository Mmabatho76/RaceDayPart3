@echo off
echo Starting RaceDay API...
start "RaceDay API" cmd /k "cd /d %~dp0RaceDay.Api && dotnet run --launch-profile http"
timeout /t 15 /nobreak >nul
echo Starting RaceDay MVC...
start "RaceDay MVC" cmd /k "cd /d %~dp0RaceDay.Mvc && dotnet run"
timeout /t 10 /nobreak >nul
start http://localhost:62579
