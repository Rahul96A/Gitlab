import { apiGet, apiPost, apiPut } from "../client";
import type { Issue, IssueComment, CreateIssueRequest, CreateCommentRequest } from "../types/issue";
import type { PaginatedList } from "../types/project";

export const issuesApi = {
  list: (slug: string, params?: { page?: number; pageSize?: number }) =>
    apiGet<PaginatedList<Issue>>(`projects/${slug}/issues`, params),

  getByNumber: (slug: string, issueNumber: number) =>
    apiGet<Issue>(`projects/${slug}/issues/${issueNumber}`),

  create: (slug: string, data: CreateIssueRequest) =>
    apiPost<Issue>(`projects/${slug}/issues`, data),

  update: (slug: string, issueNumber: number, data: Partial<CreateIssueRequest & { status: string }>) =>
    apiPut<Issue>(`projects/${slug}/issues/${issueNumber}`, data),

  getComments: (slug: string, issueNumber: number) =>
    apiGet<IssueComment[]>(`projects/${slug}/issues/${issueNumber}/comments`),

  addComment: (slug: string, issueNumber: number, data: CreateCommentRequest) =>
    apiPost<IssueComment>(`projects/${slug}/issues/${issueNumber}/comments`, data),
};
