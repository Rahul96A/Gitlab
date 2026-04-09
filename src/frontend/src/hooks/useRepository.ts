import { useQuery } from "@tanstack/react-query";
import { repositoriesApi } from "@/api/endpoints/repositories";

export function useFileTree(
  slug: string,
  params?: { ref?: string; path?: string },
) {
  return useQuery({
    queryKey: ["repository", slug, "tree", params],
    queryFn: () => repositoriesApi.getTree(slug, params),
    enabled: !!slug,
  });
}

export function useFileContent(slug: string, path: string, ref?: string) {
  return useQuery({
    queryKey: ["repository", slug, "file", path, ref],
    queryFn: () => repositoriesApi.getFile(slug, path, ref),
    enabled: !!slug && !!path,
  });
}

export function useCommitLog(
  slug: string,
  params?: { ref?: string; count?: number },
) {
  return useQuery({
    queryKey: ["repository", slug, "commits", params],
    queryFn: () => repositoriesApi.getCommits(slug, params),
    enabled: !!slug,
  });
}
