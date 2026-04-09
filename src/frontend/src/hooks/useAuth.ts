import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { authApi } from "@/api/endpoints/auth";
import { useAuthStore } from "@/stores/authStore";
import type { LoginRequest, RegisterRequest } from "@/api/types/auth";

/**
 * Hook for the current user query.
 * Only fires when we have a token — avoids 401s on public pages.
 */
export function useCurrentUser() {
  const { token, setUser } = useAuthStore();

  return useQuery({
    queryKey: ["auth", "me"],
    queryFn: async () => {
      const user = await authApi.me();
      setUser(user);
      return user;
    },
    enabled: !!token,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false,
  });
}

export function useLogin() {
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      setAuth(response.token, {
        id: response.userId,
        username: response.username,
        email: response.email,
        displayName: response.displayName,
        avatarUrl: null,
        role: response.role,
      });
      queryClient.invalidateQueries({ queryKey: ["auth"] });
      navigate("/");
    },
  });
}

export function useRegister() {
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: (response) => {
      setAuth(response.token, {
        id: response.userId,
        username: response.username,
        email: response.email,
        displayName: response.displayName,
        avatarUrl: null,
        role: response.role,
      });
      navigate("/");
    },
  });
}

export function useLogout() {
  const { logout } = useAuthStore();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return () => {
    logout();
    queryClient.clear();
    navigate("/login");
  };
}
