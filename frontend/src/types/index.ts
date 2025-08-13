export interface FileItem {
  name: string;
  path: string;
  size: number;
  contentType: string;
  lastModified: string;
  isDirectory: boolean;
  url: string;
}

export interface DirectoryItem {
  name: string;
  path: string;
  files: FileItem[];
  subdirectories: DirectoryItem[];
}

export interface UploadResponse {
  success: boolean;
  message: string;
  fileInfo?: FileItem;
}

export interface User {
  id: number;
  username: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export enum UserRole {
  Guest = 0,
  Friend = 1,
  Owner = 2
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: User;
}
