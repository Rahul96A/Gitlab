import { create } from "zustand";
import type { CurrentUser } from "@/api/types/auth";

/**
 * Auth store — manages JWT token and current user.
 *
 * Only auth state lives in Zustand. All server data (projects, issues, etc.)
 * lives in React Query's cache. This separation means:
 * - Auth state persists across navigation (Zustand)
 * - Server data auto-refetches, caches, and deduplicates (React Query)
 */
interface AuthState {
  token: string | null;
  user: CurrentUser | null;
  isAuthenticated: boolean;

  setAuth: (token: string, user: CurrentUser) => void;
  setUser: (user: CurrentUser) => void;
  logout: () => void;
  hydrateFromStorage: () => string | null;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  isAuthenticated: false,

  setAuth: (token, user) => {
    localStorage.setItem("auth_token", token);
    set({ token, user, isAuthenticated: true });
  },

  setUser: (user) => {
    set({ user });
  },

  logout: () => {
    localStorage.removeItem("auth_token");
    set({ token: null, user: null, isAuthenticated: false });
  },

  /**
   * On app load, check if a token exists in localStorage.
   * Returns the token so the caller can validate it with the API.
   */
  hydrateFromStorage: () => {
    const token = localStorage.getItem("auth_token");
    if (token) {
      set({ token, isAuthenticated: true });
    }
    return token;
  },
}));
