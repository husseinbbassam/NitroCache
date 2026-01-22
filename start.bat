@echo off
echo ==========================================
echo    NitroCache - Quick Start Script
echo ==========================================
echo.

REM Check if Docker is installed
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo X Docker is not installed. Please install Docker first.
    echo    Visit: https://docs.docker.com/get-docker/
    exit /b 1
)

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo X .NET is not installed. Please install .NET 9 SDK.
    echo    Visit: https://dotnet.microsoft.com/download
    exit /b 1
)

echo √ Docker and .NET are installed
echo.

REM Start Redis
echo Starting Redis with Docker Compose...
docker-compose up -d

REM Wait for Redis to be healthy
echo Waiting for Redis to be ready...
timeout /t 5 /nobreak >nul
docker exec nitrocache-redis redis-cli ping >nul 2>&1
if %errorlevel% neq 0 (
    echo X Redis failed to start
    exit /b 1
)
echo √ Redis is ready!
echo.

echo Building the solution...
dotnet build -c Release

echo.
echo ==========================================
echo    NitroCache is ready!
echo ==========================================
echo.
echo To run the API:
echo   cd NitroCache.Api
echo   dotnet run
echo.
echo To run benchmarks:
echo   cd NitroCache.Benchmarks
echo   dotnet run -c Release
echo.
echo To stop Redis:
echo   docker-compose down
echo.
