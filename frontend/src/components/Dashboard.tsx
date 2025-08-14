import React, { useState, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
// Test change for GitHub CI/CD pipeline verification
import { 
  FolderOpen, 
  File, 
  Download, 
  Trash2, 
  Upload, 
  Plus, 
  LogOut,
  User,
  ExternalLink
} from 'lucide-react';
import { fileService } from '../services/fileService';
import { authService } from '../services/authService';
import { DirectoryItem, FileItem, User as UserType } from '../types';
import './Dashboard.css';

const Dashboard: React.FC = () => {
  const [currentPath, setCurrentPath] = useState('');
  const [directoryStructure, setDirectoryStructure] = useState<DirectoryItem | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [user, setUser] = useState<UserType | null>(null);
  const [showCreateFolder, setShowCreateFolder] = useState(false);
  const [newFolderName, setNewFolderName] = useState('');

  const handleFileDrop = async (acceptedFiles: File[]) => {
    console.log('Files dropped:', acceptedFiles);
    for (const file of acceptedFiles) {
      try {
        console.log('Uploading file:', file.name);
        await fileService.uploadFile(file, currentPath);
        console.log('File uploaded successfully:', file.name);
      } catch (error: any) {
        console.error('Upload error:', error);
        setError(`Failed to upload ${file.name}: ${error.response?.data?.message || error.message}`);
      }
    }
    console.log('Refreshing directory...');
    loadDirectory(); // Refresh the directory
  };

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop: handleFileDrop,
    multiple: true
  });

  useEffect(() => {
    loadUser();
    loadDirectory();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPath]);

  const loadUser = async () => {
    try {
      const userData = await authService.getCurrentUser();
      setUser(userData);
    } catch (error) {
      console.error('Failed to load user:', error);
    }
  };

  const loadDirectory = async () => {
    setLoading(true);
    setError('');
    try {
      const structure = await fileService.getDirectoryStructure(currentPath);
      setDirectoryStructure(structure);
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to load directory');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenFile = async (file: FileItem) => {
    try {
      console.log('Attempting to open file:', file.name, 'with path:', file.path);
      
      // For viewing files, we open them in a new tab without download attribute
      // This allows the browser to handle them appropriately
      const viewUrl = `${process.env.REACT_APP_API_URL || 'http://localhost:5077'}/api/files/view/${encodeURIComponent(file.path)}`;
      console.log('Opening view URL:', viewUrl);
      
      // Open in new tab - browser will handle appropriately based on file type
      window.open(viewUrl, '_blank');
      
    } catch (error: any) {
      console.error('Error opening file:', error);
      setError(`Failed to open ${file.name}: ${error.message}`);
    }
  };

  const handleDownload = async (file: FileItem) => {
    try {
      console.log('Attempting to download file:', file.name, 'with path:', file.path);
      // Use the download-file endpoint that triggers a download
      const downloadUrl = `${process.env.REACT_APP_API_URL || 'http://localhost:5077'}/api/files/download-file/${encodeURIComponent(file.path)}`;
      console.log('Opening download URL:', downloadUrl);
      window.open(downloadUrl, '_blank');
    } catch (error: any) {
      console.error('Error downloading file:', error);
      setError(`Failed to download ${file.name}: ${error.response?.data?.message || error.message}`);
    }
  };

  const handleDelete = async (file: FileItem) => {
    if (window.confirm(`Are you sure you want to delete ${file.name}?`)) {
      try {
        await fileService.deleteFile(file.path);
        loadDirectory(); // Refresh the directory
      } catch (error: any) {
        setError(`Failed to delete ${file.name}: ${error.response?.data?.message || error.message}`);
      }
    }
  };

  const handleCreateFolder = async () => {
    if (!newFolderName.trim()) return;
    
    try {
      const folderPath = currentPath ? `${currentPath}/${newFolderName}` : newFolderName;
      await fileService.createDirectory(folderPath);
      setNewFolderName('');
      setShowCreateFolder(false);
      loadDirectory(); // Refresh the directory
    } catch (error: any) {
      setError(`Failed to create folder: ${error.response?.data?.message || error.message}`);
    }
  };

  const handleDeleteDirectory = async (directoryPath: string, directoryName: string) => {
    if (window.confirm(`Are you sure you want to delete the folder "${directoryName}" and all its contents?`)) {
      try {
        await fileService.deleteDirectory(directoryPath);
        loadDirectory(); // Refresh the directory
      } catch (error: any) {
        setError(`Failed to delete folder ${directoryName}: ${error.response?.data?.message || error.message}`);
      }
    }
  };

  const navigateToDirectory = (path: string) => {
    setCurrentPath(path);
  };

  const navigateUp = () => {
    const pathParts = currentPath.split('/').filter(Boolean);
    pathParts.pop();
    setCurrentPath(pathParts.join('/'));
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getBreadcrumbs = () => {
    if (!currentPath) return [{ name: 'Root', path: '' }];
    
    const parts = currentPath.split('/').filter(Boolean);
    const breadcrumbs = [{ name: 'Root', path: '' }];
    
    let accumulatedPath = '';
    for (const part of parts) {
      accumulatedPath += accumulatedPath ? `/${part}` : part;
      breadcrumbs.push({ name: part, path: accumulatedPath });
    }
    
    return breadcrumbs;
  };

  if (loading && !directoryStructure) {
    return (
      <div className="dashboard-container">
        <div className="loading">Loading...</div>
      </div>
    );
  }

  return (
    <div className="dashboard-container">
      <header className="dashboard-header">
        <h1>Azure File Viewer</h1>
        <div className="user-info">
          <User size={20} />
          <span>{user?.username}</span>
          <button onClick={authService.logout} className="logout-button">
            <LogOut size={16} />
            Logout
          </button>
        </div>
      </header>

      <div className="dashboard-content">
        {error && (
          <div className="error-banner">
            {error}
            <button onClick={() => setError('')}>×</button>
          </div>
        )}

        <div className="toolbar">
          <nav className="breadcrumbs">
            {getBreadcrumbs().map((crumb, index) => (
              <React.Fragment key={crumb.path}>
                <button
                  className="breadcrumb-button"
                  onClick={() => navigateToDirectory(crumb.path)}
                >
                  {crumb.name}
                </button>
                {index < getBreadcrumbs().length - 1 && <span className="breadcrumb-separator">/</span>}
              </React.Fragment>
            ))}
          </nav>

          <div className="toolbar-actions">
            <button 
              onClick={() => setShowCreateFolder(true)}
              className="action-button"
            >
              <Plus size={16} />
              New Folder
            </button>
          </div>
        </div>

        <div 
          {...getRootProps()} 
          className={`upload-zone ${isDragActive ? 'drag-active' : ''}`}
        >
          <input {...getInputProps()} />
          <Upload size={24} />
          <p>
            {isDragActive 
              ? 'Drop files here...' 
              : 'Drag & drop files here, or click to select files'
            }
          </p>
        </div>

        {showCreateFolder && (
          <div className="create-folder-form">
            <input
              type="text"
              value={newFolderName}
              onChange={(e) => setNewFolderName(e.target.value)}
              placeholder="Folder name"
              onKeyPress={(e) => e.key === 'Enter' && handleCreateFolder()}
              autoFocus
            />
            <button onClick={handleCreateFolder}>Create</button>
            <button onClick={() => {
              setShowCreateFolder(false);
              setNewFolderName('');
            }}>Cancel</button>
          </div>
        )}

        <div className="file-list">
          <div className="file-list-header">
            <div className="header-name">Name</div>
            <div className="header-type">Type</div>
            <div className="header-size">Size</div>
            <div className="header-modified">Modified</div>
            <div className="header-actions">Actions</div>
          </div>

          {currentPath && (
            <div className="file-row directory-row" onClick={navigateUp}>
              <div className="file-icon">
                <FolderOpen size={20} />
              </div>
              <div className="file-name">..</div>
              <div className="file-type">Folder</div>
              <div className="file-size">-</div>
              <div className="file-modified">-</div>
              <div className="file-actions">
                <span className="file-info">Go up</span>
              </div>
            </div>
          )}

          {directoryStructure?.subdirectories.map((directory) => (
            <div
              key={directory.path}
              className="file-row directory-row"
            >
              <div className="file-icon">
                <FolderOpen size={20} />
              </div>
              <div className="file-name">{directory.name}</div>
              <div className="file-type">Folder</div>
              <div className="file-size">-</div>
              <div className="file-modified">-</div>
              <div className="file-actions" onClick={(e) => e.stopPropagation()}>
                <button
                  onClick={() => navigateToDirectory(directory.path)}
                  className="action-button small"
                  title="Open Folder"
                >
                  <FolderOpen size={16} />
                </button>
                <button
                  onClick={() => handleDeleteDirectory(directory.path, directory.name)}
                  className="action-button small danger"
                  title="Delete Folder"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}

          {directoryStructure?.files.map((file) => (
            <div 
              key={file.path} 
              className="file-row file-row-clickable"
              onClick={() => handleOpenFile(file)}
              title={`Click to open ${file.name}`}
            >
              <div className="file-icon">
                <File size={20} />
              </div>
              <div className="file-name">{file.name}</div>
              <div className="file-type">File</div>
              <div className="file-size">{formatFileSize(file.size)}</div>
              <div className="file-modified">{new Date(file.lastModified).toLocaleString()}</div>
              <div className="file-actions" onClick={(e) => e.stopPropagation()}>
                <button
                  onClick={() => handleOpenFile(file)}
                  className="action-button small"
                  title="Open"
                >
                  <ExternalLink size={16} />
                </button>
                <button
                  onClick={() => handleDownload(file)}
                  className="action-button small"
                  title="Download"
                >
                  <Download size={16} />
                </button>
                <button
                  onClick={() => handleDelete(file)}
                  className="action-button small danger"
                  title="Delete"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}

          {directoryStructure && 
           directoryStructure.files.length === 0 && 
           directoryStructure.subdirectories.length === 0 && (
            <div className="empty-state">
              <p>This folder is empty</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
