import { useEffect } from "react";
import { RouterProvider } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { router } from "@/routes";
import { useAuthStore } from "@/stores/authStore";

/**
 * React Query client — configured for a good UX:
 * - staleTime: 2 min — don't refetch on every mount
 * - retry: 1 — one retry for network blips, then show error
 * - refetchOnWindowFocus: true — fresh data when user returns to tab
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 2 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
});

export default function App() {
  const { hydrateFromStorage } = useAuthStore();

  // On mount, check for an existing JWT token in localStorage
  useEffect(() => {
    hydrateFromStorage();
  }, [hydrateFromStorage]);

  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
}
