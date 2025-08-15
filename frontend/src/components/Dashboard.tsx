import React, { useState, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
import { useNavigate } from 'react-router-dom';
// Test change for GitHub CI/CD pipeline verification
// Updated to use list view format for better file management
import { 
  FolderOpen, 
  File, 
  Download, 
  Trash2, 
  Upload, 
  Plus, 
  LogOut,
  User,
  ExternalLink,
  Settings,
  Share2,
  Edit2
} from 'lucide-react';
import { fileService } from '../services/fileService';
import { authService } from '../services/authService';
import { api } from '../services/api';
import { DirectoryItem, FileItem, User as UserType, UserRole, ShareLinkRequest, ShareLinkResponse } from '../types';
import './Dashboard.css';

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [currentPath, setCurrentPath] = useState('');
  const [directoryStructure, setDirectoryStructure] = useState<DirectoryItem | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [user, setUser] = useState<UserType | null>(null);
  const [showCreateFolder, setShowCreateFolder] = useState(false);
  const [newFolderName, setNewFolderName] = useState('');
  const [editingItem, setEditingItem] = useState<{type: 'file' | 'directory', path: string, name: string} | null>(null);
  const [editingName, setEditingName] = useState('');
  const [draggedItem, setDraggedItem] = useState<{type: 'file' | 'directory', path: string, name: string} | null>(null);
  const [dragOverTarget, setDragOverTarget] = useState<string | null>(null);

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

  const handleShareLink = async (file: FileItem) => {
    try {
      const shareRequest: ShareLinkRequest = {
        filePath: file.path,
        directoryPath: currentPath
      };

      const response = await api.post<ShareLinkResponse>('/share/generate-link', shareRequest);
      
      if (response.data.success) {
        // Copy the share URL to clipboard
        await navigator.clipboard.writeText(response.data.shareUrl);
        
        // Show success message
        setSuccessMessage(`Share link copied to clipboard!`);
        
        // Clear the success message after 4 seconds
        setTimeout(() => setSuccessMessage(''), 4000);
      } else {
        setError(response.data.message);
      }
    } catch (error: any) {
      console.error('Error generating share link:', error);
      setError(`Failed to generate share link for ${file.name}: ${error.response?.data?.message || error.message}`);
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

  const handleStartEdit = (type: 'file' | 'directory', path: string, name: string) => {
    setEditingItem({ type, path, name });
    setEditingName(name);
  };

  const handleCancelEdit = () => {
    setEditingItem(null);
    setEditingName('');
  };

  const handleSaveEdit = async () => {
    if (!editingItem || !editingName.trim() || editingName === editingItem.name) {
      handleCancelEdit();
      return;
    }

    try {
      if (editingItem.type === 'file') {
        await fileService.renameFile(editingItem.path, editingName.trim());
      } else {
        await fileService.renameDirectory(editingItem.path, editingName.trim());
      }
      setSuccessMessage(`${editingItem.type === 'file' ? 'File' : 'Directory'} renamed successfully`);
      handleCancelEdit();
      loadDirectory(); // Refresh the directory
    } catch (error: any) {
      setError(`Failed to rename ${editingItem.type}: ${error.response?.data?.message || error.message}`);
    }
  };

  const handleEditKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSaveEdit();
    } else if (e.key === 'Escape') {
      handleCancelEdit();
    }
  };

  // Drag and drop handlers
  const handleDragStart = (e: React.DragEvent, type: 'file' | 'directory', path: string, name: string) => {
    setDraggedItem({ type, path, name });
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', JSON.stringify({ type, path, name }));
  };

  const handleDragEnd = () => {
    setDraggedItem(null);
    setDragOverTarget(null);
  };

  const handleDragOver = (e: React.DragEvent, targetDirectoryPath: string) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverTarget(targetDirectoryPath);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    // Only clear drag over if we're leaving the target element entirely
    if (!e.currentTarget.contains(e.relatedTarget as Node)) {
      setDragOverTarget(null);
    }
  };

  const handleDrop = async (e: React.DragEvent, targetDirectoryPath: string) => {
    e.preventDefault();
    setDragOverTarget(null);

    if (!draggedItem) return;

    // Don't allow dropping on itself
    if (draggedItem.path === targetDirectoryPath) {
      return;
    }

    // Don't allow dropping a directory into its own subdirectory
    if (draggedItem.type === 'directory' && targetDirectoryPath.startsWith(draggedItem.path + '/')) {
      setError('Cannot move a directory into its own subdirectory');
      return;
    }

    try {
      setLoading(true);
      setError('');

      if (draggedItem.type === 'file') {
        await fileService.moveFile(draggedItem.path, targetDirectoryPath);
        setSuccessMessage(`Moved file "${draggedItem.name}" successfully`);
      } else {
        await fileService.moveDirectory(draggedItem.path, targetDirectoryPath);
        setSuccessMessage(`Moved folder "${draggedItem.name}" successfully`);
      }

      loadDirectory(); // Refresh the directory
    } catch (error: any) {
      setError(`Failed to move ${draggedItem.type}: ${error.response?.data?.message || error.message}`);
    } finally {
      setLoading(false);
      setDraggedItem(null);
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

  const getParentPath = (path: string) => {
    const pathParts = path.split('/').filter(Boolean);
    pathParts.pop();
    return pathParts.join('/');
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
        <h1>My File Cabinet</h1>
        <div className="user-info">
          <div className="user-profile">
            <User size={20} className="user-icon" />
            <span>{user?.username}</span>
          </div>
          <div className="header-actions">
            {user?.role === UserRole.Owner && (
              <button 
                onClick={() => navigate('/admin')} 
                className="admin-button"
                title="User Management"
              >
                <Settings size={16} />
                Admin
              </button>
            )}
            <button onClick={authService.logout} className="logout-button">
              <LogOut size={16} />
              Logout
            </button>
          </div>
        </div>
      </header>

      <div className="dashboard-content">
        {error && (
          <div className="error-banner">
            {error}
            <button onClick={() => setError('')}>×</button>
          </div>
        )}

        {successMessage && (
          <div className="success-banner">
            {successMessage}
            <button onClick={() => setSuccessMessage('')}>×</button>
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
            <div></div> {/* Icon column */}
            <div className="header-name">Name</div>
            <div className="header-type">Type</div>
            <div className="header-size">Size</div>
            <div className="header-date">Date</div>
            <div className="header-actions">Actions</div>
          </div>

          {currentPath && (
            <div 
              className={`file-row directory-row parent-directory-row ${dragOverTarget === getParentPath(currentPath) ? 'drag-over' : ''}`}
              onClick={navigateUp}
              onDragOver={(e) => handleDragOver(e, getParentPath(currentPath))}
              onDragLeave={handleDragLeave}
              onDrop={(e) => handleDrop(e, getParentPath(currentPath))}
              title="Drop files here to move them to the parent folder"
            >
              <div className="file-icon">
                <FolderOpen size={20} />
              </div>
              <div className="file-name">..</div>
              <div className="file-type">Folder</div>
              <div className="file-size">-</div>
              <div className="file-date">
                <span className="file-info">Go up</span>
              </div>
              <div className="file-actions">
                <span className="file-info">Parent</span>
              </div>
            </div>
          )}

          {directoryStructure?.subdirectories.map((directory) => (
            <div
              key={directory.path}
              className={`file-row directory-row ${dragOverTarget === directory.path ? 'drag-over' : ''}`}
              draggable
              onDragStart={(e) => handleDragStart(e, 'directory', directory.path, directory.name)}
              onDragEnd={handleDragEnd}
              onDragOver={(e) => handleDragOver(e, directory.path)}
              onDragLeave={handleDragLeave}
              onDrop={(e) => handleDrop(e, directory.path)}
              onClick={(e) => {
                // Don't navigate if currently editing this directory
                if (editingItem?.type === 'directory' && editingItem?.path === directory.path) {
                  return;
                }
                // Only navigate if the click is not on the action buttons
                if (!(e.target as HTMLElement).closest('.file-actions')) {
                  navigateToDirectory(directory.path);
                }
              }}
              title={editingItem?.type === 'directory' && editingItem?.path === directory.path ? 
                'Currently editing - click outside to save' : 
                'Click to open folder'}
            >
              <div className="file-icon">
                <FolderOpen size={20} />
              </div>
              <div className="file-name">
                {editingItem?.type === 'directory' && editingItem?.path === directory.path ? (
                  <input
                    type="text"
                    value={editingName}
                    onChange={(e) => setEditingName(e.target.value)}
                    onKeyDown={handleEditKeyPress}
                    onBlur={handleSaveEdit}
                    onClick={(e) => e.stopPropagation()}
                    autoFocus
                    className="edit-input"
                  />
                ) : (
                  <>
                    {directory.name} 
                    <span className="file-count">({directory.files.length})</span>
                  </>
                )}
              </div>
              <div className="file-type">Folder</div>
              <div className="file-size">-</div>
              <div className="file-date">
                {directory.lastModified ? new Date(directory.lastModified).toLocaleDateString() : '-'}
              </div>
              <div className="file-actions" onClick={(e) => e.stopPropagation()}>
                <button
                  onClick={() => navigateToDirectory(directory.path)}
                  className="action-button small"
                  title="Open Folder"
                >
                  <FolderOpen size={16} />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    handleStartEdit('directory', directory.path, directory.name);
                  }}
                  className="action-button small"
                  title="Rename Folder"
                >
                  <Edit2 size={16} />
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
              draggable
              onDragStart={(e) => handleDragStart(e, 'file', file.path, file.name)}
              onDragEnd={handleDragEnd}
              onClick={() => {
                // Don't open file if currently editing this item
                if (editingItem?.type === 'file' && editingItem?.path === file.path) {
                  return;
                }
                handleOpenFile(file);
              }}
              title={editingItem?.type === 'file' && editingItem?.path === file.path ? 
                'Currently editing - click outside to save' : 
                `Click to open ${file.name}`}
            >
              <div className="file-icon">
                <File size={20} />
              </div>
              <div className="file-name">
                {editingItem?.type === 'file' && editingItem?.path === file.path ? (
                  <input
                    type="text"
                    value={editingName}
                    onChange={(e) => setEditingName(e.target.value)}
                    onKeyDown={handleEditKeyPress}
                    onBlur={handleSaveEdit}
                    onClick={(e) => e.stopPropagation()}
                    autoFocus
                    className="edit-input"
                  />
                ) : (
                  file.name
                )}
              </div>
              <div className="file-type">File</div>
              <div className="file-size">{formatFileSize(file.size)}</div>
              <div className="file-date">
                {new Date(file.lastModified).toLocaleDateString()}
              </div>
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
                  onClick={() => handleShareLink(file)}
                  className="action-button small"
                  title="Copy share link to clipboard"
                >
                  <Share2 size={16} />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    handleStartEdit('file', file.path, file.name);
                  }}
                  className="action-button small"
                  title="Rename"
                >
                  <Edit2 size={16} />
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
