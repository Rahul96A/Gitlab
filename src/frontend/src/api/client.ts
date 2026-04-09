import ky from "ky";

/**
 * Centralized API client using `ky` (a tiny fetch wrapper).
 *
 * Why ky over axios?
 * - 4KB vs 30KB+ (matters for initial load)
 * - Built on native fetch — works with React 19's `use()` hook
 * - Automatic retry, timeout, and JSON parsing built in
 * - No polyfills needed for modern browsers
 *
 * Auth: The `beforeRequest` hook injects the JWT token from
 * localStorage. On 401, the `afterResponse` hook clears the
 * token and redirects to login.
 */

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";

export const api = ky.create({
  prefixUrl: `${API_BASE}/api/v1`,
  timeout: 30_000,
  retry: {
    limit: 2,
    methods: ["get"],
    statusCodes: [408, 429, 500, 502, 503, 504],
  },
  hooks: {
    beforeRequest: [
      (request) => {
        const token = localStorage.getItem("auth_token");
        if (token) {
          request.headers.set("Authorization", `Bearer ${token}`);
        }
      },
    ],
    afterResponse: [
      (_request, _options, response) => {
        if (response.status === 401) {
          localStorage.removeItem("auth_token");
          // Only redirect if not already on login page
          if (!window.location.pathname.startsWith("/login")) {
            window.location.href = "/login";
          }
        }
      },
    ],
  },
});

/**
 * Type-safe API helper — wraps ky's JSON response parsing.
 *
 * Usage:
 *   const user = await apiGet<CurrentUser>("auth/me");
 *   const project = await apiPost<Project>("projects", { name: "foo" });
 */
export async function apiGet<T>(
  path: string,
  searchParams?: Record<string, string | number | boolean | undefined>,
): Promise<T> {
  // Filter out undefined values from search params
  const cleanParams: Record<string, string> = {};
  if (searchParams) {
    for (const [key, value] of Object.entries(searchParams)) {
      if (value !== undefined) {
        cleanParams[key] = String(value);
      }
    }
  }

  return api.get(path, { searchParams: cleanParams }).json<T>();
}

export async function apiPost<T>(
  path: string,
  body?: unknown,
): Promise<T> {
  return api.post(path, { json: body }).json<T>();
}

export async function apiPut<T>(
  path: string,
  body?: unknown,
): Promise<T> {
  return api.put(path, { json: body }).json<T>();
}

export async function apiDelete(path: string): Promise<void> {
  await api.delete(path);
}
