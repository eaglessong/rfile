#!/bin/bash

echo "🚀 Starting File Viewer Application in Development Mode"
echo "======================================================"

# Function to check if a port is in use
check_port() {
    lsof -ti :$1 > /dev/null 2>&1
}

# Function to start backend
start_backend() {
    echo "🔄 Starting Backend API..."
    cd /Users/ssong/rfile/backend/FileViewer.Api
    
    # Build the backend first
    echo "🔨 Building backend..."
    dotnet build --verbosity quiet
    
    if [ $? -eq 0 ]; then
        echo "✅ Backend build successful"
        
        # Start the backend
        echo "🚀 Starting backend on http://localhost:5077..."
        dotnet run --urls http://localhost:5077 > backend.log 2>&1 &
        BACKEND_PID=$!
        echo $BACKEND_PID > /Users/ssong/rfile/.backend.pid
        
        # Wait for backend to start
        sleep 5
        
        if check_port 5077; then
            echo "✅ Backend API is running on http://localhost:5077 (PID: $BACKEND_PID)"
        else
            echo "❌ Backend failed to start. Check backend.log for details."
            cat backend.log
            exit 1
        fi
    else
        echo "❌ Backend build failed"
        exit 1
    fi
}

# Function to start frontend
start_frontend() {
    echo "🔄 Starting Frontend React App..."
    cd /Users/ssong/rfile/frontend
    
    # Check if node_modules exists
    if [ ! -d "node_modules" ]; then
        echo "📦 Installing frontend dependencies..."
        npm install
    fi
    
    # Start the frontend
    echo "🚀 Starting frontend on http://localhost:3000..."
    BROWSER=none npm start > frontend.log 2>&1 &
    FRONTEND_PID=$!
    echo $FRONTEND_PID > /Users/ssong/rfile/.frontend.pid
    
    # Wait for frontend to start
    sleep 8
    
    if check_port 3000; then
        echo "✅ Frontend is running on http://localhost:3000 (PID: $FRONTEND_PID)"
    else
        echo "❌ Frontend failed to start. Check frontend.log for details."
        cat frontend.log
        exit 1
    fi
}

# Check if backend is already running
if check_port 5077; then
    EXISTING_PID=$(lsof -ti :5077)
    echo "⚠️ Backend port 5077 is already in use by PID: $EXISTING_PID"
    echo "✅ Backend API is already running on http://localhost:5077"
else
    start_backend
fi

# Check if frontend is already running
if check_port 3000; then
    EXISTING_PID=$(lsof -ti :3000)
    echo "⚠️ Frontend port 3000 is already in use by PID: $EXISTING_PID"
    echo "✅ Frontend is already running on http://localhost:3000"
else
    start_frontend
fi

echo ""
echo "🎉 Application is ready!"
echo "📱 Frontend: http://localhost:3000"
echo "🔧 Backend API: http://localhost:5077"
echo "📚 API Documentation: http://localhost:5077/swagger"
echo ""
echo "💡 Tips:"
echo "   - Use 'bash stop-app.sh' to stop all services"
echo "   - Check logs: tail -f backend/FileViewer.Api/backend.log"
echo "   - Check logs: tail -f frontend/frontend.log"
echo ""
