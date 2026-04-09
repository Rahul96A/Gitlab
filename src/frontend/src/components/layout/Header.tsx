import { Link } from "react-router";
import { Bell, LogOut, User } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { useAuthStore } from "@/stores/authStore";
import { useNotificationStore } from "@/stores/notificationStore";
import { useLogout } from "@/hooks/useAuth";

export function Header() {
  const { user, isAuthenticated } = useAuthStore();
  const { unreadCount } = useNotificationStore();
  const logout = useLogout();

  return (
    <header className="sticky top-0 z-20 flex h-14 items-center justify-between border-b border-border bg-surface/80 px-6 backdrop-blur-sm">
      {/* Breadcrumbs placeholder */}
      <div />

      {/* Right side */}
      <div className="flex items-center gap-3">
        {isAuthenticated ? (
          <>
            {/* Notifications */}
            <button
              className="relative rounded-md p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
              title="Notifications"
            >
              <Bell className="h-5 w-5" />
              {unreadCount > 0 && (
                <span className="absolute -top-0.5 -right-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </button>

            {/* User menu */}
            <div className="flex items-center gap-2">
              <Avatar
                name={user?.displayName || "User"}
                src={user?.avatarUrl}
                size="sm"
              />
              <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                {user?.displayName}
              </span>
            </div>

            <button
              onClick={logout}
              className="rounded-md p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
              title="Sign out"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </>
        ) : (
          <div className="flex gap-2">
            <Link
              to="/login"
              className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
            >
              Sign in
            </Link>
            <Link
              to="/register"
              className="rounded-md bg-brand-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-brand-700"
            >
              Register
            </Link>
          </div>
        )}
      </div>
    </header>
  );
}
