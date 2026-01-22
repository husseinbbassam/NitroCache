#!/bin/bash

echo "=========================================="
echo "   NitroCache - Quick Start Script"
echo "=========================================="
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    echo "   Visit: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET is not installed. Please install .NET 9 SDK."
    echo "   Visit: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ Docker and .NET are installed"
echo ""

# Start Redis
echo "🐳 Starting Redis with Docker Compose..."
docker-compose up -d

# Wait for Redis to be healthy
echo "⏳ Waiting for Redis to be ready..."
for i in {1..30}; do
    if docker exec nitrocache-redis redis-cli ping &> /dev/null; then
        echo "✅ Redis is ready!"
        break
    fi
    sleep 1
    if [ $i -eq 30 ]; then
        echo "❌ Redis failed to start within 30 seconds"
        exit 1
    fi
done

echo ""
echo "🏗️  Building the solution..."
dotnet build -c Release

echo ""
echo "=========================================="
echo "   NitroCache is ready!"
echo "=========================================="
echo ""
echo "To run the API:"
echo "  cd NitroCache.Api"
echo "  dotnet run"
echo ""
echo "To run benchmarks:"
echo "  cd NitroCache.Benchmarks"
echo "  dotnet run -c Release"
echo ""
echo "To stop Redis:"
echo "  docker-compose down"
echo ""
