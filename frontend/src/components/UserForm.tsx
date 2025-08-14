import React, { useState, useEffect } from 'react';
import { User, UserRole } from '../types';

interface UserFormProps {
  user: User | null;
  isEditing: boolean;
  onSubmit: (userData: any) => void;
  onCancel: () => void;
  currentUserRole: UserRole;
}

const UserForm: React.FC<UserFormProps> = ({ 
  user, 
  isEditing, 
  onSubmit, 
  onCancel, 
  currentUserRole 
}) => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    role: UserRole.Guest
  });
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    if (isEditing && user) {
      setFormData({
        username: user.username,
        email: user.email,
        password: '',
        confirmPassword: '',
        role: user.role
      });
    } else {
      setFormData({
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
        role: UserRole.Guest
      });
    }
    setErrors({});
  }, [user, isEditing]);

  const validateForm = () => {
    const newErrors: { [key: string]: string } = {};

    if (!formData.username.trim()) {
      newErrors.username = 'Username is required';
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    if (!isEditing && !formData.password) {
      newErrors.password = 'Password is required for new users';
    }

    if (formData.password && formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }

    if (formData.password && formData.password.length < 6) {
      newErrors.password = 'Password must be at least 6 characters long';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    const userData: any = {
      username: formData.username,
      email: formData.email,
      role: formData.role
    };

    // Only include password if it's provided
    if (formData.password) {
      userData.password = formData.password;
    }

    onSubmit(userData);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    
    // Convert role value to number for enum
    const processedValue = name === 'role' ? parseInt(value, 10) : value;
    
    setFormData(prev => ({
      ...prev,
      [name]: processedValue
    }));

    // Clear error when user starts typing
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }
  };

  // Only owners can create other owners
  const canSetOwnerRole = currentUserRole === UserRole.Owner;

  return (
    <div className="user-form">
      <h2>{isEditing ? 'Edit User' : 'Add New User'}</h2>
      
      <form onSubmit={handleSubmit}>
        <div className="form-grid">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              name="username"
              type="text"
              value={formData.username}
              onChange={handleInputChange}
              placeholder="Enter username"
              autoComplete="username"
            />
            {errors.username && (
              <span style={{ color: '#dc3545', fontSize: '12px', marginTop: '4px' }}>
                {errors.username}
              </span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleInputChange}
              placeholder="Enter email address"
              autoComplete="email"
            />
            {errors.email && (
              <span style={{ color: '#dc3545', fontSize: '12px', marginTop: '4px' }}>
                {errors.email}
              </span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="password">
              {isEditing ? 'New Password (leave blank to keep current)' : 'Password'}
            </label>
            <input
              id="password"
              name="password"
              type="password"
              value={formData.password}
              onChange={handleInputChange}
              placeholder={isEditing ? "Leave blank to keep current password" : "Enter password"}
              autoComplete="new-password"
            />
            {errors.password && (
              <span style={{ color: '#dc3545', fontSize: '12px', marginTop: '4px' }}>
                {errors.password}
              </span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">
              {isEditing ? 'Confirm New Password' : 'Confirm Password'}
            </label>
            <input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              value={formData.confirmPassword}
              onChange={handleInputChange}
              placeholder="Confirm password"
              autoComplete="new-password"
            />
            {errors.confirmPassword && (
              <span style={{ color: '#dc3545', fontSize: '12px', marginTop: '4px' }}>
                {errors.confirmPassword}
              </span>
            )}
          </div>

          <div className="form-group full-width">
            <label htmlFor="role">Role</label>
            <select
              id="role"
              name="role"
              value={formData.role}
              onChange={handleInputChange}
            >
              <option value={UserRole.Guest}>Guest</option>
              <option value={UserRole.Friend}>Friend</option>
              {canSetOwnerRole && (
                <option value={UserRole.Owner}>Owner</option>
              )}
            </select>
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onCancel}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="btn btn-primary"
          >
            {isEditing ? 'Update User' : 'Create User'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default UserForm;
