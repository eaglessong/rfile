#!/bin/bash

echo "🏥 File Viewer Application Health Check"
echo "======================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to check if a port is in use
check_port() {
    lsof -ti :$1 > /dev/null 2>&1
}

# Function to check HTTP endpoint
check_endpoint() {
    local url=$1
    local name=$2
    
    if curl -s --fail --connect-timeout 5 "$url" > /dev/null 2>&1; then
        echo -e "  ✅ ${GREEN}$name is responding${NC}"
        return 0
    else
        echo -e "  ❌ ${RED}$name is not responding${NC}"
        return 1
    fi
}

echo ""
echo "🔍 Checking System Requirements..."

# Check .NET
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version 2>/dev/null)
    echo -e "  ✅ ${GREEN}.NET SDK installed: $DOTNET_VERSION${NC}"
else
    echo -e "  ❌ ${RED}.NET SDK not found${NC}"
fi

# Check Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version 2>/dev/null)
    echo -e "  ✅ ${GREEN}Node.js installed: $NODE_VERSION${NC}"
else
    echo -e "  ❌ ${RED}Node.js not found${NC}"
fi

# Check npm
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version 2>/dev/null)
    echo -e "  ✅ ${GREEN}npm installed: $NPM_VERSION${NC}"
else
    echo -e "  ❌ ${RED}npm not found${NC}"
fi

echo ""
echo "📁 Checking Project Structure..."

# Check backend files
if [ -f "/Users/ssong/rfile/backend/FileViewer.Api/FileViewer.Api.csproj" ]; then
    echo -e "  ✅ ${GREEN}Backend project file exists${NC}"
else
    echo -e "  ❌ ${RED}Backend project file missing${NC}"
fi

# Check frontend files
if [ -f "/Users/ssong/rfile/frontend/package.json" ]; then
    echo -e "  ✅ ${GREEN}Frontend package.json exists${NC}"
else
    echo -e "  ❌ ${RED}Frontend package.json missing${NC}"
fi

# Check frontend dependencies
if [ -d "/Users/ssong/rfile/frontend/node_modules" ]; then
    echo -e "  ✅ ${GREEN}Frontend dependencies installed${NC}"
else
    echo -e "  ⚠️ ${YELLOW}Frontend dependencies not installed${NC}"
fi

echo ""
echo "🌐 Checking Service Status..."

# Check Backend
if check_port 5077; then
    BACKEND_PID=$(lsof -ti :5077)
    echo -e "  ✅ ${GREEN}Backend running on port 5077 (PID: $BACKEND_PID)${NC}"
    check_endpoint "http://localhost:5077/swagger" "Backend Swagger"
    check_endpoint "http://localhost:5077/api/files" "Backend API"
else
    echo -e "  ❌ ${RED}Backend not running on port 5077${NC}"
fi

# Check Frontend
if check_port 3000; then
    FRONTEND_PID=$(lsof -ti :3000)
    echo -e "  ✅ ${GREEN}Frontend running on port 3000 (PID: $FRONTEND_PID)${NC}"
    check_endpoint "http://localhost:3000" "Frontend Application"
else
    echo -e "  ❌ ${RED}Frontend not running on port 3000${NC}"
fi

echo ""
echo "🔧 Configuration Check..."

# Check backend configuration
if [ -f "/Users/ssong/rfile/backend/FileViewer.Api/appsettings.json" ]; then
    echo -e "  ✅ ${GREEN}Backend configuration exists${NC}"
else
    echo -e "  ❌ ${RED}Backend configuration missing${NC}"
fi

# Check frontend environment
if [ -f "/Users/ssong/rfile/frontend/.env" ]; then
    API_URL=$(grep REACT_APP_API_URL /Users/ssong/rfile/frontend/.env | cut -d'=' -f2)
    echo -e "  ✅ ${GREEN}Frontend .env exists (API_URL: $API_URL)${NC}"
    
    if [ "$API_URL" = "http://localhost:5077" ]; then
        echo -e "  ✅ ${GREEN}Frontend API URL correctly configured${NC}"
    else
        echo -e "  ⚠️ ${YELLOW}Frontend API URL may be incorrect${NC}"
    fi
else
    echo -e "  ❌ ${RED}Frontend .env missing${NC}"
fi

echo ""
echo "📋 Build Status..."

# Check backend build
cd /Users/ssong/rfile/backend/FileViewer.Api
if dotnet build --verbosity quiet > /dev/null 2>&1; then
    echo -e "  ✅ ${GREEN}Backend builds successfully${NC}"
else
    echo -e "  ❌ ${RED}Backend build fails${NC}"
fi

# Check frontend build
cd /Users/ssong/rfile/frontend
if npm run build > /dev/null 2>&1; then
    echo -e "  ✅ ${GREEN}Frontend builds successfully${NC}"
else
    echo -e "  ❌ ${RED}Frontend build fails${NC}"
fi

echo ""
echo "🎯 Summary:"
if check_port 5077 && check_port 3000; then
    echo -e "  🎉 ${GREEN}Application is fully operational!${NC}"
    echo -e "  📱 Frontend: ${BLUE}http://localhost:3000${NC}"
    echo -e "  🔧 Backend: ${BLUE}http://localhost:5077${NC}"
    echo -e "  📚 API Docs: ${BLUE}http://localhost:5077/swagger${NC}"
elif check_port 5077; then
    echo -e "  ⚠️ ${YELLOW}Backend is running, but frontend needs to be started${NC}"
    echo -e "     Run: ${BLUE}bash dev-start.sh${NC}"
elif check_port 3000; then
    echo -e "  ⚠️ ${YELLOW}Frontend is running, but backend needs to be started${NC}"
    echo -e "     Run: ${BLUE}bash dev-start.sh${NC}"
else
    echo -e "  🚀 ${YELLOW}Services are not running${NC}"
    echo -e "     Run: ${BLUE}bash dev-start.sh${NC}"
fi

echo ""
