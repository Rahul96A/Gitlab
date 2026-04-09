import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { issuesApi } from "@/api/endpoints/issues";
import type { CreateIssueRequest, CreateCommentRequest } from "@/api/types/issue";

export function useIssues(
  slug: string,
  params?: { page?: number; pageSize?: number },
) {
  return useQuery({
    queryKey: ["issues", slug, params],
    queryFn: () => issuesApi.list(slug, params),
    enabled: !!slug,
  });
}

export function useIssue(slug: string, issueNumber: number) {
  return useQuery({
    queryKey: ["issues", slug, issueNumber],
    queryFn: () => issuesApi.getByNumber(slug, issueNumber),
    enabled: !!slug && issueNumber > 0,
  });
}

export function useCreateIssue(slug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateIssueRequest) => issuesApi.create(slug, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["issues", slug] });
    },
  });
}

export function useIssueComments(slug: string, issueNumber: number) {
  return useQuery({
    queryKey: ["issues", slug, issueNumber, "comments"],
    queryFn: () => issuesApi.getComments(slug, issueNumber),
    enabled: !!slug && issueNumber > 0,
  });
}

export function useAddComment(slug: string, issueNumber: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCommentRequest) =>
      issuesApi.addComment(slug, issueNumber, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["issues", slug, issueNumber, "comments"],
      });
    },
  });
}
