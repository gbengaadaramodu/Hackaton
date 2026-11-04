import React, { createContext, useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login as apiLogin, register as apiRegister, getMe } from '../api/auth';
import type { UserProfile, LoginRequest, RegisterRequest, AuthResponse } from '../types';

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isStudent: boolean;
  isTeacher: boolean;
  isAdmin: boolean;
  login: (req: LoginRequest) => Promise<void>;
  logout: () => void;
  register: (req: RegisterRequest) => Promise<AuthResponse>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('sl_token'));
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const storedToken = localStorage.getItem('sl_token');
    if (storedToken) {
      getMe()
        .then((profile) => setUser(profile))
        .catch(() => {
          localStorage.removeItem('sl_token');
          setToken(null);
        })
        .finally(() => setIsLoading(false));
    } else {
      setIsLoading(false);
    }
  }, []);

  const login = async (req: LoginRequest) => {
    const res = await apiLogin(req);
    localStorage.setItem('sl_token', res.accessToken);
    setToken(res.accessToken);
    setUser(res.user);
  };

  const logout = () => {
    localStorage.removeItem('sl_token');
    setToken(null);
    setUser(null);
    navigate('/login');
  };

  const register = async (req: RegisterRequest): Promise<AuthResponse> => {
    return await apiRegister(req);
  };

  const roles = user?.roles ?? [];

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token && !!user,
        isLoading,
        isStudent: roles.includes('Student'),
        isTeacher: roles.includes('Teacher'),
        isAdmin: roles.includes('Administrator'),
        login,
        logout,
        register,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
