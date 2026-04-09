import React, { createContext, useContext, useEffect, useState } from 'react';
import type { ApiUser } from '../utils/api';
import {
  getProgressRequest,
  loginRequest,
  registerRequest,
  updateProgressRequest,
} from '../utils/api';

type User = ApiUser;
type Theme = 'light' | 'dark';

interface RegisterPayload {
  name: string;
  lastName: string;
  email: string;
  password: string;
}

interface LoginPayload {
  email: string;
  password: string;
}

interface AppContextType {
  user: User | null;
  login: (payload: LoginPayload) => Promise<User>;
  register: (payload: RegisterPayload) => Promise<User>;
  logout: () => void;
  theme: Theme;
  toggleTheme: () => void;
  isLogoutModalOpen: boolean;
  openLogoutModal: () => void;
  closeLogoutModal: () => void;
  completedLessons: number[];
  isProgressLoading: boolean;
  refreshProgress: () => Promise<void>;
  markLessonCompleted: (lessonId: number) => Promise<void>;
}

const initialContext: AppContextType = {
  user: null,
  login: async () => {
    throw new Error('Not implemented');
  },
  register: async () => {
    throw new Error('Not implemented');
  },
  logout: () => {},
  theme: 'light',
  toggleTheme: () => {},
  isLogoutModalOpen: false,
  openLogoutModal: () => {},
  closeLogoutModal: () => {},
  completedLessons: [],
  isProgressLoading: false,
  refreshProgress: async () => {},
  markLessonCompleted: async () => {},
};

const STORAGE_KEY = 'realden_user';
const THEME_STORAGE_KEY = 'realden_theme';
const AppContext = createContext<AppContextType>(initialContext);

export const AppProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    const rawUser = window.localStorage.getItem(STORAGE_KEY);
    if (!rawUser) {
      return null;
    }

    try {
      return JSON.parse(rawUser) as User;
    } catch {
      window.localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  });
  const [theme, setTheme] = useState<Theme>(() => {
    const savedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
    return savedTheme === 'dark' ? 'dark' : 'light';
  });
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false);
  const [completedLessons, setCompletedLessons] = useState<number[]>([]);
  const [isProgressLoading, setIsProgressLoading] = useState(false);

  const persistUser = (nextUser: User | null) => {
    setUser(nextUser);
    if (nextUser) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextUser));
    } else {
      window.localStorage.removeItem(STORAGE_KEY);
    }
  };

  const refreshProgress = async () => {
    if (!user) {
      setCompletedLessons([]);
      return;
    }

    setIsProgressLoading(true);
    try {
      const response = await getProgressRequest(user.id);
      setCompletedLessons(response.completedLessons);
    } finally {
      setIsProgressLoading(false);
    }
  };

  const login = async (payload: LoginPayload) => {
    const response = await loginRequest(payload);
    persistUser(response.user);
    return response.user;
  };

  const register = async (payload: RegisterPayload) => {
    const response = await registerRequest(payload);
    persistUser(response.user);
    return response.user;
  };

  const logout = () => {
    persistUser(null);
    setIsLogoutModalOpen(false);
    setCompletedLessons([]);
  };

  const toggleTheme = () => setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  const openLogoutModal = () => setIsLogoutModalOpen(true);
  const closeLogoutModal = () => setIsLogoutModalOpen(false);

  const markLessonCompleted = async (lessonId: number) => {
    if (!user) {
      return;
    }

    await updateProgressRequest({
      userId: user.id,
      lessonId,
      completed: true,
    });

    setCompletedLessons((prev) => (prev.includes(lessonId) ? prev : [...prev, lessonId]));
  };

  useEffect(() => {
    document.body.classList.remove('light', 'dark');
    document.body.classList.add(theme);
    window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  }, [theme]);

  useEffect(() => {
    if (!user) {
      setCompletedLessons([]);
      return;
    }

    void refreshProgress();
  }, [user]);

  return (
    <AppContext.Provider
      value={{
        user,
        login,
        register,
        logout,
        theme,
        toggleTheme,
        isLogoutModalOpen,
        openLogoutModal,
        closeLogoutModal,
        completedLessons,
        isProgressLoading,
        refreshProgress,
        markLessonCompleted,
      }}
    >
      {children}
    </AppContext.Provider>
  );
};

export const useAppContext = () => useContext(AppContext);
