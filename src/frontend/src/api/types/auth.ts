export interface AuthResponse {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  token: string;
  role: string;
}

export interface CurrentUser {
  id: string;
  username: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  role: string;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}
