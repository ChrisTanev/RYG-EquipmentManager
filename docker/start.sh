#!/bin/bash

# RYG Equipment Manager - Docker Startup Script

set -e

echo "🚀 RYG Equipment Manager - Docker Setup"
echo "========================================"
echo ""

# Function to show usage
show_usage() {
    echo "Usage: ./start.sh [mode]"
    echo ""
    echo "Modes:"
    echo "  prod       Start full stack in production mode (default)"
    echo "  dev        Start infrastructure only (Azurite)"
    echo "  debug      Start full stack with debugging enabled"
    echo "  stop       Stop all services"
    echo "  clean      Stop all services and remove volumes"
    echo "  logs       Show logs for all services"
    echo ""
    echo "Examples:"
    echo "  ./start.sh              # Start production mode"
    echo "  ./start.sh dev          # Start development mode"
    echo "  ./start.sh debug        # Start debug mode"
    echo "  ./start.sh stop         # Stop all services"
    echo "  ./start.sh clean        # Clean everything"
    echo ""
}

# Check if docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Detect docker compose command (new: 'docker compose' or old: 'docker-compose')
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ docker compose is not available. Please install Docker Compose."
    exit 1
fi

# Get mode from argument (default to prod)
MODE="${1:-prod}"

case $MODE in
    prod)
        echo "📦 Starting full stack in PRODUCTION mode..."
        echo ""
        $DOCKER_COMPOSE up -d
        echo ""
        echo "✅ Services started successfully!"
        echo ""
        echo "Service endpoints:"
        echo "  - Azure Functions: http://localhost:7071"
        echo "  - SignalR Hub: http://localhost:5000"
        echo "  - Azurite Blob: http://localhost:10000"
        echo "  - Azurite Queue: http://localhost:10001"
        echo "  - Azurite Table: http://localhost:10002"
        echo ""
        echo "View logs: docker-compose logs -f"
        echo "Stop services: ./start.sh stop"
        ;;

    dev)
        echo "🔧 Starting infrastructure in DEVELOPMENT mode..."
        echo ""
        $DOCKER_COMPOSE -f docker-compose.dev.yml up -d
        echo ""
        echo "✅ Infrastructure started successfully!"
        echo ""
        echo "Services running:"
        echo "  - Azurite Blob: http://localhost:10000"
        echo "  - Azurite Queue: http://localhost:10001"
        echo "  - Azurite Table: http://localhost:10002"
        echo ""
        echo "Now run your application locally:"
        echo "  cd ../src/RYG.Functions && dotnet run"
        echo "  cd ../src/RYG.SignalRHubHost && dotnet run"
        echo ""
        echo "Stop services: $DOCKER_COMPOSE -f docker-compose.dev.yml down"
        ;;

    debug)
        echo "🐛 Starting full stack in DEBUG mode..."
        echo ""
        $DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.debug.yml up --build
        echo ""
        echo "Services started in debug mode with source mounting."
        echo ""
        echo "Debugger ports:"
        echo "  - Functions: localhost:5678"
        echo "  - SignalR Hub: localhost:5679"
        echo ""
        echo "To attach debugger:"
        echo "  1. Open VS Code"
        echo "  2. Press F5"
        echo "  3. Select 'Docker: Attach to Functions' or 'Docker: Attach to SignalR Hub'"
        echo ""
        ;;

    stop)
        echo "🛑 Stopping all services..."
        echo ""
        # Try to stop both dev and prod configurations
        $DOCKER_COMPOSE down 2>/dev/null || true
        $DOCKER_COMPOSE -f docker-compose.dev.yml down 2>/dev/null || true
        echo ""
        echo "✅ All services stopped!"
        ;;

    clean)
        echo "🧹 Cleaning up (stopping services and removing volumes)..."
        echo ""
        read -p "This will delete all data. Are you sure? (y/N) " -n 1 -r
        echo ""
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            $DOCKER_COMPOSE down -v 2>/dev/null || true
            $DOCKER_COMPOSE -f docker-compose.dev.yml down -v 2>/dev/null || true
            echo ""
            echo "✅ Cleanup complete!"
        else
            echo "Cancelled."
        fi
        ;;

    logs)
        echo "📋 Showing logs (Ctrl+C to exit)..."
        echo ""
        $DOCKER_COMPOSE logs -f
        ;;

    help|--help|-h)
        show_usage
        ;;

    *)
        echo "❌ Unknown mode: $MODE"
        echo ""
        show_usage
        exit 1
        ;;
esac
