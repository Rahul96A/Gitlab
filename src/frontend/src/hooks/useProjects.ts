import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { projectsApi } from "@/api/endpoints/projects";
import type { CreateProjectRequest } from "@/api/types/project";

export function useProjects(params?: {
  page?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: ["projects", params],
    queryFn: () => projectsApi.list(params),
  });
}

export function useProject(slug: string) {
  return useQuery({
    queryKey: ["projects", slug],
    queryFn: () => projectsApi.getBySlug(slug),
    enabled: !!slug,
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProjectRequest) => projectsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (slug: string) => projectsApi.delete(slug),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
  });
}
