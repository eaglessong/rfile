# 📁 Remote File Viewer & Store

A comprehensive Azure-powered file management system with drag-and-drop upload, web-based viewing, and role-based access control.

## 🏗️ Architecture

- **Frontend**: React 18 + TypeScript with modern UI components
- **Backend**: .NET 7 Web API with JWT authentication
- **Storage**: Azure Blob Storage for file storage
- **Infrastructure**: Azure Bicep templates with AZD deployment
- **Authentication**: JWT-based with role management (Owner, Friend, Guest)

## 🚀 Quick Start

### Prerequisites

- **.NET 7 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/7.0))
- **Node.js 18+** ([Download](https://nodejs.org/))
- **Git** for version control

### Development Setup

1. **Clone and navigate to the project**:
   ```bash
   git clone <repository-url>
   cd rfile
   ```

2. **Health Check** (verify everything is properly configured):
   ```bash
   bash health-check.sh
   ```

3. **Start Development Environment**:
   ```bash
   bash dev-start.sh
   ```

4. **Access the Application**:
   - 🌐 **Frontend**: http://localhost:3000
   - � **Backend API**: http://localhost:5077
   - 📚 **API Documentation**: http://localhost:5077/swagger

5. **Stop Services**:
   ```bash
   bash dev-stop.sh
   ```

## � Manual Setup (if needed)

### Backend Setup
```bash
cd backend/FileViewer.Api
dotnet restore
dotnet build
dotnet run --urls http://localhost:5077
```

### Frontend Setup
```bash
cd frontend
npm install
npm start
```

## 🎯 Features

### Core Functionality
- ✅ **Drag & Drop File Upload** - Multi-file upload with progress tracking
- ✅ **File Viewing** - In-browser preview for PDFs, images, text files
- ✅ **File Download** - Secure file retrieval with proper headers
- ✅ **Directory Navigation** - Folder creation and browsing
- ✅ **File Management** - Delete, organize, and manage files

### Authentication & Security
- ✅ **JWT Authentication** - Secure token-based authentication
- ✅ **Role-based Access** - Owner, Friend, Guest roles with different permissions
- ✅ **CORS Configuration** - Secure cross-origin requests

### Technical Features
- ✅ **Persistent Storage** - JSON-based file metadata with Base64 content storage
- ✅ **Real Content Serving** - Actual uploaded file content (no mock data)
- ✅ **Responsive UI** - Mobile-friendly interface
- ✅ **Error Handling** - Comprehensive error logging and user feedback

## 🛠️ Development Scripts

| Script | Purpose |
|--------|---------|
| `bash health-check.sh` | 🏥 Check system status and configuration |
| `bash dev-start.sh` | 🚀 Start both frontend and backend services |
| `bash dev-stop.sh` | 🛑 Stop all running services |

## 📁 Project Structure

```
rfile/
├── backend/                    # .NET 7 Web API
│   └── FileViewer.Api/
│       ├── Controllers/        # API endpoints
│       ├── Services/          # Business logic
│       └── Models/            # Data models
├── frontend/                   # React 18 + TypeScript
│   ├── src/
│   │   ├── components/        # React components
│   │   └── services/          # API clients
│   └── public/                # Static assets
├── infra/                     # Azure Bicep templates
└── scripts/                   # Development utilities
```

## 🔍 Troubleshooting

### Common Issues

**1. Port Already in Use**
```bash
# Kill processes on specific ports
lsof -ti :5077 | xargs kill  # Backend
lsof -ti :3000 | xargs kill  # Frontend
```

**2. Frontend Dependencies Issues**
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

**3. Backend Build Errors**
```bash
cd backend/FileViewer.Api
dotnet clean
dotnet restore
dotnet build
```

**4. CORS Issues**
- Ensure frontend `.env` file has: `REACT_APP_API_URL=http://localhost:5077`
- Check backend CORS policy in `Program.cs`

### Debug Logs

- **Backend logs**: `tail -f backend/FileViewer.Api/backend.log`
- **Frontend logs**: `tail -f frontend/frontend.log`

## 🧪 Testing

### Backend Tests
```bash
cd backend/FileViewer.Api
dotnet test
```

### Frontend Tests
```bash
cd frontend
npm test
```

### Manual Testing
1. Upload a file via drag-and-drop
2. View the file in-browser
3. Download the file
4. Test authentication flows

## 🌟 Recent Fixes

- ✅ Fixed React Router v6 compatibility issues
- ✅ Resolved frontend dependency conflicts
- ✅ Implemented real file content serving (no mock data)
- ✅ Fixed view vs download functionality
- ✅ Added comprehensive health checking
- ✅ Improved development workflow scripts

## 🔮 Azure Deployment

The project includes full Azure infrastructure setup:

```bash
# Initialize Azure deployment
azd init

# Deploy to Azure
azd up
```

**Azure Services Used**:
- Static Web Apps (Frontend)
- App Service (Backend API)
- Blob Storage (File storage)
- Key Vault (Secrets management)
- Application Insights (Monitoring)

## 📞 Support

For issues or questions:
1. Run `bash health-check.sh` to diagnose problems
2. Check the troubleshooting section above
3. Review logs for specific error messages
4. Ensure all prerequisites are installed

---

🎉 **Happy coding!** Your remote file viewer is ready for development and deployment.
   cd backend
   dotnet run
   
   # Start frontend (in a new terminal)
   cd frontend
   npm start
   ```

### Azure Deployment

1. Initialize Azure Developer CLI:
   ```bash
   azd init
   ```

2. Deploy to Azure:
   ```bash
   azd up
   ```

## Project Structure

```
azure-file-viewer/
├── frontend/           # React application
├── backend/           # .NET Core Web API
├── infra/            # Azure infrastructure (Bicep)
├── .github/          # GitHub Actions workflows
└── azure.yaml        # Azure Developer CLI configuration
```

## Cost Estimation

For personal use with moderate traffic:
- Azure App Service (Basic B1): ~$13/month
- Azure Blob Storage: ~$0.02/GB/month
- Azure Static Web Apps: Free tier
- Azure Key Vault: ~$0.03/10k operations
- **Total**: ~$15-25/month

## License

MIT License
