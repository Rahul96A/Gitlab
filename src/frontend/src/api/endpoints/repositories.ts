import { apiGet } from "../client";
import type { GitFileEntry, GitFileContent, GitCommitInfo } from "../types/repository";

export const repositoriesApi = {
  getTree: (slug: string, params?: { ref?: string; path?: string }) =>
    apiGet<GitFileEntry[]>(`projects/${slug}/repository/tree`, params),

  getFile: (slug: string, path: string, ref?: string) =>
    apiGet<GitFileContent>(`projects/${slug}/repository/files`, { path, ref }),

  getCommits: (slug: string, params?: { ref?: string; count?: number }) =>
    apiGet<GitCommitInfo[]>(`projects/${slug}/repository/commits`, params),
};
