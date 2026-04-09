import { Navigate, useLocation } from "react-router";
import { useAuthStore } from "@/stores/authStore";
import { Spinner } from "@/components/ui/Spinner";
import { useCurrentUser } from "@/hooks/useAuth";

/**
 * Wraps routes that require authentication.
 * Redirects to /login if not authenticated, preserving the intended destination.
 */
export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  const location = useLocation();
  const { isLoading } = useCurrentUser();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (isLoading) {
    return <Spinner size="lg" />;
  }

  return <>{children}</>;
}
