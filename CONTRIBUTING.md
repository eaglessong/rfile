# Contributing to Remote File Viewer

Thank you for your interest in contributing to the Remote File Viewer project! This document provides guidelines and information for contributors.

## 🚀 Quick Start for Contributors

1. **Fork the repository**
2. **Clone your fork**:
   ```bash
   git clone https://github.com/yourusername/rfile.git
   cd rfile
   ```
3. **Set up development environment**:
   ```bash
   bash health-check.sh
   bash dev-start.sh
   ```

## 📋 Development Guidelines

### Code Style

**Backend (.NET)**:
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep controllers thin, business logic in services

**Frontend (React)**:
- Use TypeScript strictly
- Follow React functional component patterns
- Use meaningful component and variable names
- Keep components small and focused

### Git Workflow

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
3. **Test your changes**:
   ```bash
   # Backend tests
   cd backend/FileViewer.Api
   dotnet test
   
   # Frontend tests
   cd frontend
   npm test
   ```

4. **Commit with descriptive messages**:
   ```bash
   git add .
   git commit -m "feat: add file preview functionality"
   ```

5. **Push and create PR**:
   ```bash
   git push origin feature/your-feature-name
   ```

### Commit Message Format

Use conventional commits:
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation changes
- `style:` - Code style changes
- `refactor:` - Code refactoring
- `test:` - Test additions or changes
- `chore:` - Build/tooling changes

## 🧪 Testing

### Running Tests

```bash
# Full test suite
bash health-check.sh

# Backend only
cd backend/FileViewer.Api && dotnet test

# Frontend only
cd frontend && npm test

# Build verification
dotnet build && cd frontend && npm run build
```

### Writing Tests

- Write unit tests for new functionality
- Ensure high test coverage for critical paths
- Test both success and error scenarios
- Mock external dependencies appropriately

## 🐛 Bug Reports

When reporting bugs, include:
1. **Environment**: OS, browser, .NET/Node versions
2. **Steps to reproduce**
3. **Expected vs actual behavior**
4. **Screenshots** (if applicable)
5. **Console logs** and error messages

## 💡 Feature Requests

For new features:
1. **Check existing issues** first
2. **Describe the use case** clearly
3. **Explain the expected behavior**
4. **Consider backwards compatibility**

## 🔒 Security

- **Never commit sensitive data** (API keys, passwords, personal files)
- **Follow secure coding practices**
- **Report security issues privately** via email
- **Use environment variables** for configuration

## 📁 Project Structure

```
rfile/
├── backend/FileViewer.Api/     # .NET Web API
│   ├── Controllers/           # API endpoints
│   ├── Services/             # Business logic
│   ├── Models/               # Data models
│   └── Properties/           # Configuration
├── frontend/                 # React application
│   ├── src/components/       # React components
│   ├── src/services/         # API clients
│   └── public/               # Static assets
├── infra/                    # Azure infrastructure
└── .github/workflows/        # CI/CD pipelines
```

## 🛠️ Development Tools

- **Health Check**: `bash health-check.sh`
- **Start Dev**: `bash dev-start.sh`
- **Stop Dev**: `bash dev-stop.sh`
- **Logs**: Check `backend.log` and `frontend.log`

## 📦 Dependencies

### Adding Dependencies

**Backend**:
```bash
cd backend/FileViewer.Api
dotnet add package PackageName
```

**Frontend**:
```bash
cd frontend
npm install package-name
```

### Dependency Guidelines
- Keep dependencies minimal and necessary
- Prefer stable, well-maintained packages
- Document any breaking changes
- Update package.json/csproj appropriately

## 🎯 Code Review Process

1. **Self-review** your code first
2. **Ensure tests pass** locally
3. **Write clear PR description**
4. **Respond to feedback** constructively
5. **Update documentation** if needed

## 📚 Resources

- [.NET 7 Documentation](https://docs.microsoft.com/dotnet/)
- [React Documentation](https://reactjs.org/docs/)
- [Azure Documentation](https://docs.microsoft.com/azure/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)

## 📞 Getting Help

- **GitHub Issues**: For bugs and feature requests
- **Discussions**: For questions and community chat
- **Documentation**: Check README.md and PROJECT_STATUS.md

---

Thank you for contributing! 🎉
