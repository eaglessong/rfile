import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { authService } from './services/authService';
import { User } from './types';
import Login from './components/Login';
import Register from './components/Register';
import Dashboard from './components/Dashboard';
import Admin from './pages/Admin';
import './App.css';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(authService.isAuthenticated());
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  useEffect(() => {
    // Check authentication status when the app loads
    setIsAuthenticated(authService.isAuthenticated());
    
    // Get current user if authenticated
    if (authService.isAuthenticated()) {
      authService.getCurrentUser()
        .then(user => setCurrentUser(user))
        .catch(() => {
          // If getting user fails, logout
          authService.logout();
          setIsAuthenticated(false);
        });
    }
    
    // Listen for storage changes (when token is set/removed in other tabs)
    const handleStorageChange = () => {
      setIsAuthenticated(authService.isAuthenticated());
      if (!authService.isAuthenticated()) {
        setCurrentUser(null);
      }
    };
    
    window.addEventListener('storage', handleStorageChange);
    
    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  return (
    <Router>
      <div className="App">
        <Routes>
          <Route 
            path="/login" 
            element={!isAuthenticated ? <Login /> : <Navigate to="/dashboard" replace />} 
          />
          <Route 
            path="/register" 
            element={!isAuthenticated ? <Register /> : <Navigate to="/dashboard" replace />} 
          />
          <Route 
            path="/dashboard" 
            element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" replace />} 
          />
          <Route 
            path="/admin" 
            element={
              isAuthenticated && currentUser ? 
                <Admin currentUser={currentUser} /> : 
                <Navigate to="/login" replace />
            } 
          />
          <Route 
            path="/" 
            element={<Navigate to={isAuthenticated ? "/dashboard" : "/login"} replace />} 
          />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
