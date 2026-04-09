import { Outlet } from "react-router";
import { Sidebar } from "@/components/layout/Sidebar";
import { Header } from "@/components/layout/Header";

/**
 * Layout for authenticated dashboard pages.
 * Sidebar + header + content area.
 */
export function DashboardLayout() {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <div className="flex flex-1 flex-col pl-sidebar">
        <Header />
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
