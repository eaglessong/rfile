import React, { useState, useEffect } from 'react';
import { User, UserRole } from '../types';
import { api } from '../services/api';
import UserList from '../components/UserList';
import UserForm from '../components/UserForm';
import './Admin.css';

interface AdminProps {
  currentUser: User;
}

const Admin: React.FC<AdminProps> = ({ currentUser }) => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  // Check if current user has admin permissions
  const hasAdminPermissions = currentUser.role === UserRole.Owner;

  useEffect(() => {
    if (hasAdminPermissions) {
      loadUsers();
    }
  }, [hasAdminPermissions]);

  const loadUsers = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await api.get('/admin/users');
      setUsers(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = () => {
    setSelectedUser(null);
    setIsEditing(false);
    setShowForm(true);
  };

  const handleEditUser = (user: User) => {
    setSelectedUser(user);
    setIsEditing(true);
    setShowForm(true);
  };

  const handleDeleteUser = async (user: User) => {
    if (user.id === currentUser.id) {
      alert('You cannot delete your own account');
      return;
    }

    if (user.role === UserRole.Owner) {
      const ownerCount = users.filter(u => u.role === UserRole.Owner).length;
      if (ownerCount <= 1) {
        alert('Cannot delete the last owner account');
        return;
      }
    }

    if (window.confirm(`Are you sure you want to delete user "${user.username}"?`)) {
      try {
        await api.delete(`/admin/users/${user.id}`);
        await loadUsers();
      } catch (err: any) {
        setError(err.response?.data?.message || 'Failed to delete user');
      }
    }
  };

  const handleFormSubmit = async (userData: any) => {
    try {
      if (isEditing && selectedUser) {
        await api.put(`/admin/users/${selectedUser.id}`, userData);
      } else {
        await api.post('/admin/users', userData);
      }
      setShowForm(false);
      await loadUsers();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save user');
    }
  };

  const handleFormCancel = () => {
    setShowForm(false);
    setSelectedUser(null);
    setIsEditing(false);
  };

  if (!hasAdminPermissions) {
    return (
      <div className="admin-page">
        <div className="access-denied">
          <h2>Access Denied</h2>
          <p>You don't have permission to access the admin panel.</p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="admin-page">
        <div className="loading">Loading users...</div>
      </div>
    );
  }

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h1>User Management</h1>
        <button 
          className="btn btn-primary" 
          onClick={handleCreateUser}
          disabled={showForm}
        >
          Add New User
        </button>
      </div>

      {error && (
        <div className="error-message">
          {error}
          <button 
            className="error-dismiss" 
            onClick={() => setError(null)}
          >
            ×
          </button>
        </div>
      )}

      {showForm ? (
        <UserForm
          user={selectedUser}
          isEditing={isEditing}
          onSubmit={handleFormSubmit}
          onCancel={handleFormCancel}
          currentUserRole={currentUser.role}
        />
      ) : (
        <UserList
          users={users}
          currentUser={currentUser}
          onEdit={handleEditUser}
          onDelete={handleDeleteUser}
        />
      )}
    </div>
  );
};

export default Admin;
