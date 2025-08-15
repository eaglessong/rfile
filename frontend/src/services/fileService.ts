import api from './authService';
import { FileItem, DirectoryItem, UploadResponse } from '../types';

export const fileService = {
  async getFiles(directoryPath?: string): Promise<FileItem[]> {
    const response = await api.get<FileItem[]>('/files', {
      params: { directoryPath }
    });
    return response.data;
  },

  async getDirectoryStructure(directoryPath?: string): Promise<DirectoryItem> {
    const response = await api.get<DirectoryItem>('/files/directory', {
      params: { directoryPath }
    });
    return response.data;
  },

  async uploadFile(file: File, directoryPath?: string): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (directoryPath) {
      formData.append('directoryPath', directoryPath);
    }

    const response = await api.post<UploadResponse>('/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  async deleteFile(filePath: string): Promise<void> {
    await api.delete(`/files/${encodeURIComponent(filePath)}`);
  },

  async getDownloadUrl(filePath: string): Promise<string> {
    console.log('Getting download URL for:', filePath);
    const encodedPath = encodeURIComponent(filePath);
    console.log('Encoded path:', encodedPath);
    const response = await api.get<{ downloadUrl: string }>(`/files/download/${encodedPath}`);
    console.log('Download URL response:', response.data);
    return response.data.downloadUrl;
  },

  async createDirectory(directoryPath: string): Promise<void> {
    await api.post('/files/create-directory', { directoryPath });
  },

  async deleteDirectory(directoryPath: string): Promise<void> {
    await api.delete(`/files/directory/${encodeURIComponent(directoryPath)}`);
  },

  async renameFile(oldFilePath: string, newFileName: string): Promise<void> {
    await api.put('/files/rename-file', {
      oldFilePath,
      newFileName
    });
  },

  async renameDirectory(oldDirectoryPath: string, newDirectoryName: string): Promise<void> {
    await api.put('/files/rename-directory', {
      oldDirectoryPath,
      newDirectoryName
    });
  },

  async moveFile(sourceFilePath: string, destinationDirectoryPath: string): Promise<void> {
    await api.put('/files/move-file', {
      sourceFilePath,
      destinationDirectoryPath
    });
  },

  async moveDirectory(sourceDirectoryPath: string, destinationDirectoryPath: string): Promise<void> {
    await api.put('/files/move-directory', {
      sourceDirectoryPath,
      destinationDirectoryPath
    });
  }
};
