import { apiGet, apiPost } from "../client";
import type {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from "../types/auth";

export const authApi = {
  login: (data: LoginRequest) => apiPost<AuthResponse>("auth/login", data),

  register: (data: RegisterRequest) =>
    apiPost<AuthResponse>("auth/register", data),

  me: () => apiGet<CurrentUser>("auth/me"),
};
