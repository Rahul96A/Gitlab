export interface Project {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  visibility: "Private" | "Internal" | "Public";
  defaultBranch: string;
  ownerId: string;
  ownerUsername: string;
  memberCount: number;
  issueCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  visibility?: "Private" | "Internal" | "Public";
  defaultBranch?: string;
}

export interface PaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
