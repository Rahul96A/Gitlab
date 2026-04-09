import { Outlet } from "react-router";
import { Toaster } from "sonner";
import { useSignalR } from "@/hooks/useSignalR";

/**
 * Root layout wrapping ALL routes.
 * Initializes SignalR and provides the toast notification container.
 */
export function RootLayout() {
  useSignalR();

  return (
    <>
      <Outlet />
      <Toaster position="top-right" richColors closeButton />
    </>
  );
}
