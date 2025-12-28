import api from '../api/axios';

// Auth Service - handles authentication-related API calls
export const authService = {
  // Login user
  login: async (credentials) => {
    try {
      const response = await api.post('/auth/login', credentials);
      const { token, user } = response.data;
      
      // Store token in localStorage
      if (token) {
        localStorage.setItem('authToken', token);
        localStorage.setItem('user', JSON.stringify(user));
      }
      
      return response.data;
    } catch (error) {
      console.error('Error logging in:', error);
      throw error;
    }
  },

  // Register user
  register: async (userData) => {
    try {
      const response = await api.post('/auth/register', userData);
      return response.data;
    } catch (error) {
      console.error('Error registering:', error);
      throw error;
    }
  },

  // Logout user
  logout: () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
    window.location.href = '/login';
  },

  // Get current user
  getCurrentUser: () => {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  },

  // Check if user is authenticated
  isAuthenticated: () => {
    return !!localStorage.getItem('authToken');
  },

  // Refresh token
  refreshToken: async () => {
    try {
      const response = await api.post('/auth/refresh');
      const { token } = response.data;
      
      if (token) {
        localStorage.setItem('authToken', token);
      }
      
      return response.data;
    } catch (error) {
      console.error('Error refreshing token:', error);
      throw error;
    }
  },

  // Forgot password
  forgotPassword: async (email) => {
    try {
      const response = await api.post('/auth/forgot-password', { email });
      return response.data;
    } catch (error) {
      console.error('Error sending forgot password:', error);
      throw error;
    }
  },

  // Reset password
  resetPassword: async (token, newPassword) => {
    try {
      const response = await api.post('/auth/reset-password', {
        token,
        password: newPassword
      });
      return response.data;
    } catch (error) {
      console.error('Error resetting password:', error);
      throw error;
    }
  }
};

export default authService;