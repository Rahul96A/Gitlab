import { apiGet, apiPost, apiPut, apiDelete } from "../client";
import type { Project, CreateProjectRequest, PaginatedList } from "../types/project";

export const projectsApi = {
  list: (params?: { page?: number; pageSize?: number; search?: string }) =>
    apiGet<PaginatedList<Project>>("projects", params),

  getBySlug: (slug: string) => apiGet<Project>(`projects/${slug}`),

  create: (data: CreateProjectRequest) => apiPost<Project>("projects", data),

  update: (slug: string, data: Partial<CreateProjectRequest>) =>
    apiPut<Project>(`projects/${slug}`, data),

  delete: (slug: string) => apiDelete(`projects/${slug}`),
};
