export const APP_NAME = "GitLabClone";

export const ROLES = {
  Guest: 10,
  Reporter: 20,
  Developer: 30,
  Maintainer: 40,
  Admin: 50,
} as const;

export type RoleName = keyof typeof ROLES;

/**
 * Check if a user's role meets the minimum required access level.
 */
export function hasMinRole(userRole: string, minRole: RoleName): boolean {
  return (ROLES[userRole as RoleName] ?? 0) >= ROLES[minRole];
}

export const VISIBILITY_OPTIONS = [
  { value: "Private", label: "Private", description: "Only project members can access" },
  { value: "Internal", label: "Internal", description: "Any authenticated user can access" },
  { value: "Public", label: "Public", description: "Anyone can access" },
] as const;

/** Pipeline status → color mapping for badges */
export const PIPELINE_STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200",
  Running: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200",
  Success: "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200",
  Failed: "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200",
  Canceled: "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200",
};
