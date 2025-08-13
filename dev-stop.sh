#!/bin/bash

echo "🛑 Stopping File Viewer Application Services"
echo "==========================================="

# Function to stop a process by PID file
stop_by_pidfile() {
    local service_name=$1
    local pidfile=$2
    
    if [ -f "$pidfile" ]; then
        local pid=$(cat "$pidfile")
        if ps -p $pid > /dev/null 2>&1; then
            echo "🛑 Stopping $service_name (PID: $pid)..."
            kill $pid
            sleep 2
            
            # Force kill if still running
            if ps -p $pid > /dev/null 2>&1; then
                echo "⚡ Force stopping $service_name..."
                kill -9 $pid
            fi
            
            echo "✅ $service_name stopped"
        else
            echo "⚠️ $service_name was not running"
        fi
        rm -f "$pidfile"
    else
        echo "⚠️ No PID file found for $service_name"
    fi
}

# Function to stop by port
stop_by_port() {
    local service_name=$1
    local port=$2
    
    local pids=$(lsof -ti :$port 2>/dev/null)
    if [ -n "$pids" ]; then
        echo "🛑 Stopping $service_name on port $port..."
        echo $pids | xargs kill
        sleep 2
        
        # Force kill if still running
        local remaining_pids=$(lsof -ti :$port 2>/dev/null)
        if [ -n "$remaining_pids" ]; then
            echo "⚡ Force stopping $service_name..."
            echo $remaining_pids | xargs kill -9
        fi
        
        echo "✅ $service_name stopped"
    else
        echo "⚠️ No $service_name process found on port $port"
    fi
}

cd /Users/ssong/rfile

# Stop backend
echo "🔄 Stopping Backend API..."
stop_by_pidfile "Backend API" ".backend.pid"
stop_by_port "Backend API" 5077

# Stop frontend
echo "🔄 Stopping Frontend..."
stop_by_pidfile "Frontend" ".frontend.pid"
stop_by_port "Frontend" 3000

# Clean up log files
echo "🧹 Cleaning up log files..."
rm -f backend/FileViewer.Api/backend.log
rm -f frontend/frontend.log

echo ""
echo "✅ All services stopped successfully!"
echo ""
