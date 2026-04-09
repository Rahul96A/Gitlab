import { useEffect, useRef } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useAuthStore } from "@/stores/authStore";
import { useNotificationStore } from "@/stores/notificationStore";
import type { AppNotification } from "@/stores/notificationStore";

const SIGNALR_URL =
  import.meta.env.VITE_SIGNALR_URL || "/hubs/notifications";

/**
 * Manages the SignalR connection for real-time notifications.
 *
 * Connects when authenticated, disconnects on logout.
 * Auto-reconnects with exponential backoff on failure.
 * Must be called once in a top-level component (e.g., RootLayout).
 */
export function useSignalR() {
  const { token, isAuthenticated } = useAuthStore();
  const { addNotification } = useNotificationStore();
  const connectionRef = useRef<ReturnType<
    typeof HubConnectionBuilder.prototype.build
  > | null>(null);

  useEffect(() => {
    if (!isAuthenticated || !token) {
      // Disconnect if logged out
      connectionRef.current?.stop();
      connectionRef.current = null;
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(SIGNALR_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    // Handle incoming notifications
    connection.on("ReceiveNotification", (notification: AppNotification) => {
      addNotification(notification);
    });

    connection
      .start()
      .catch((err) => console.error("SignalR connection failed:", err));

    connectionRef.current = connection;

    return () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop();
      }
    };
  }, [isAuthenticated, token, addNotification]);
}
