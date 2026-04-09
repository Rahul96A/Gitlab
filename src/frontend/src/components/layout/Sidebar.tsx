import { NavLink } from "react-router";
import { cn } from "@/lib/utils";
import {
  FolderGit2,
  LayoutDashboard,
  Plus,
  Settings,
} from "lucide-react";
import { APP_NAME } from "@/lib/constants";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/projects", label: "Projects", icon: FolderGit2 },
  { to: "/projects/new", label: "New Project", icon: Plus },
];

export function Sidebar() {
  return (
    <aside className="fixed left-0 top-0 z-30 flex h-screen w-sidebar flex-col border-r border-border bg-surface-secondary">
      {/* Logo */}
      <div className="flex h-14 items-center gap-2 border-b border-border px-4">
        <FolderGit2 className="h-6 w-6 text-brand-600" />
        <span className="text-lg font-semibold text-slate-900 dark:text-white">
          {APP_NAME}
        </span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 space-y-1 p-3">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === "/"}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300"
                  : "text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800",
              )
            }
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </NavLink>
        ))}
      </nav>

      {/* Bottom */}
      <div className="border-t border-border p-3">
        <NavLink
          to="/settings"
          className="flex items-center gap-3 rounded-md px-3 py-2 text-sm text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          <Settings className="h-4 w-4" />
          Settings
        </NavLink>
      </div>
    </aside>
  );
}
