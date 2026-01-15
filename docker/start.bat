@echo off
REM RYG Equipment Manager - Docker Startup Script (Windows)

echo.
echo RYG Equipment Manager - Docker Setup
echo ========================================
echo.

REM Detect docker compose command
docker compose version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    set DOCKER_COMPOSE=docker compose
) else (
    docker-compose version >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        set DOCKER_COMPOSE=docker-compose
    ) else (
        echo Docker Compose is not available. Please install Docker Desktop.
        exit /b 1
    )
)

REM Get mode from argument (default to prod)
set MODE=%1
if "%MODE%"=="" set MODE=prod

if /I "%MODE%"=="prod" goto PROD
if /I "%MODE%"=="dev" goto DEV
if /I "%MODE%"=="debug" goto DEBUG
if /I "%MODE%"=="stop" goto STOP
if /I "%MODE%"=="clean" goto CLEAN
if /I "%MODE%"=="logs" goto LOGS
if /I "%MODE%"=="help" goto HELP
if /I "%MODE%"=="-h" goto HELP
if /I "%MODE%"=="--help" goto HELP

echo Unknown mode: %MODE%
echo.
goto HELP

:PROD
echo Starting full stack in PRODUCTION mode...
echo.
%DOCKER_COMPOSE% up -d
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Failed to start services. Please check Docker is running.
    exit /b 1
)
echo.
echo Services started successfully!
echo.
echo Service endpoints:
echo   - Azure Functions: http://localhost:7071
echo   - SignalR Hub: http://localhost:5000
echo   - Azurite Blob: http://localhost:10000
echo   - Azurite Queue: http://localhost:10001
echo   - Azurite Table: http://localhost:10002
echo.
echo View logs: docker-compose logs -f
echo Stop services: start.bat stop
goto END

:DEV
echo Starting infrastructure in DEVELOPMENT mode...
echo.
%DOCKER_COMPOSE% -f docker-compose.dev.yml up -d
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Failed to start services. Please check Docker is running.
    exit /b 1
)
echo.
echo Infrastructure started successfully!
echo.
echo Services running:
echo   - Azurite Blob: http://localhost:10000
echo   - Azurite Queue: http://localhost:10001
echo   - Azurite Table: http://localhost:10002
echo.
echo Now run your application locally:
echo   cd ..\src\RYG.Functions ^&^& dotnet run
echo   cd ..\src\RYG.SignalRHubHost ^&^& dotnet run
echo.
echo Stop services: %DOCKER_COMPOSE% -f docker-compose.dev.yml down
goto END

:DEBUG
echo Starting full stack in DEBUG mode...
echo.
%DOCKER_COMPOSE% -f docker-compose.yml -f docker-compose.debug.yml up --build
echo.
echo Services started in debug mode with source mounting.
echo.
echo Debugger ports:
echo   - Functions: localhost:5678
echo   - SignalR Hub: localhost:5679
echo.
echo To attach debugger:
echo   1. Open VS Code
echo   2. Press F5
echo   3. Select 'Docker: Attach to Functions' or 'Docker: Attach to SignalR Hub'
echo.
goto END

:STOP
echo Stopping all services...
echo.
%DOCKER_COMPOSE% down 2>nul
%DOCKER_COMPOSE% -f docker-compose.dev.yml down 2>nul
echo.
echo All services stopped!
goto END

:CLEAN
echo Cleaning up (stopping services and removing volumes)...
echo.
set /p CONFIRM="This will delete all data. Are you sure? (y/N): "
if /I "%CONFIRM%"=="y" (
    %DOCKER_COMPOSE% down -v 2>nul
    %DOCKER_COMPOSE% -f docker-compose.dev.yml down -v 2>nul
    echo.
    echo Cleanup complete!
) else (
    echo Cancelled.
)
goto END

:LOGS
echo Showing logs (Ctrl+C to exit)...
echo.
%DOCKER_COMPOSE% logs -f
goto END

:HELP
echo Usage: start.bat [mode]
echo.
echo Modes:
echo   prod       Start full stack in production mode (default)
echo   dev        Start infrastructure only (Azurite)
echo   debug      Start full stack with debugging enabled
echo   stop       Stop all services
echo   clean      Stop all services and remove volumes
echo   logs       Show logs for all services
echo.
echo Examples:
echo   start.bat              # Start production mode
echo   start.bat dev          # Start development mode
echo   start.bat debug        # Start debug mode
echo   start.bat stop         # Stop all services
echo   start.bat clean        # Clean everything
echo.
goto END

:END
