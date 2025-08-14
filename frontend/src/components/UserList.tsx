import React from 'react';
import { User, UserRole } from '../types';

interface UserListProps {
  users: User[];
  currentUser: User;
  onEdit: (user: User) => void;
  onDelete: (user: User) => void;
}

const UserList: React.FC<UserListProps> = ({ users, currentUser, onEdit, onDelete }) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getRoleClass = (role: UserRole) => {
    switch (role) {
      case UserRole.Owner:
        return 'owner';
      case UserRole.Friend:
        return 'friend';
      case UserRole.Guest:
        return 'guest';
      default:
        return 'guest';
    }
  };

  const getRoleDisplayName = (role: UserRole) => {
    switch (role) {
      case UserRole.Owner:
        return 'Owner';
      case UserRole.Friend:
        return 'Friend';
      case UserRole.Guest:
        return 'Guest';
      default:
        return 'Guest';
    }
  };

  const canDeleteUser = (user: User) => {
    // Can't delete yourself
    if (user.id === currentUser.id) return false;
    
    // Can't delete the last owner
    if (user.role === UserRole.Owner) {
      const ownerCount = users.filter(u => u.role === UserRole.Owner).length;
      return ownerCount > 1;
    }
    
    return true;
  };

  if (users.length === 0) {
    return (
      <div className="user-list">
        <div style={{ padding: '40px', textAlign: 'center', color: '#666' }}>
          No users found
        </div>
      </div>
    );
  }

  return (
    <div className="user-list">
      <div className="user-list-header">
        <div>User</div>
        <div>Email</div>
        <div>Role</div>
        <div>Created</div>
        <div>Actions</div>
      </div>
      {users.map((user) => (
        <div key={user.id} className="user-list-item">
          <div className="user-info">
            <div className="user-username">
              {user.username}
              {user.id === currentUser.id && (
                <span style={{ color: '#666', fontSize: '12px', marginLeft: '8px' }}>
                  (You)
                </span>
              )}
            </div>
          </div>
          <div className="user-email">{user.email}</div>
          <div>
            <span className={`user-role ${getRoleClass(user.role)}`}>
              {getRoleDisplayName(user.role)}
            </span>
          </div>
          <div className="user-created">
            {formatDate(user.createdAt)}
          </div>
          <div className="user-actions">
            <button
              className="btn btn-secondary btn-small"
              onClick={() => onEdit(user)}
            >
              Edit
            </button>
            <button
              className="btn btn-danger btn-small"
              onClick={() => onDelete(user)}
              disabled={!canDeleteUser(user)}
              title={
                user.id === currentUser.id
                  ? "You cannot delete your own account"
                  : user.role === UserRole.Owner && users.filter(u => u.role === UserRole.Owner).length <= 1
                  ? "Cannot delete the last owner account"
                  : "Delete user"
              }
            >
              Delete
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};

export default UserList;
