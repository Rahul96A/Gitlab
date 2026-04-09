import { apiGet, apiPost } from "../client";
import type { Pipeline, PipelineJob } from "../types/pipeline";
import type { PaginatedList } from "../types/project";

export const pipelinesApi = {
  list: (slug: string, params?: { page?: number; pageSize?: number }) =>
    apiGet<PaginatedList<Pipeline>>(`projects/${slug}/pipelines`, params),

  getById: (slug: string, pipelineId: string) =>
    apiGet<Pipeline>(`projects/${slug}/pipelines/${pipelineId}`),

  getJobs: (slug: string, pipelineId: string) =>
    apiGet<PipelineJob[]>(`projects/${slug}/pipelines/${pipelineId}/jobs`),

  trigger: (slug: string, data: { ref: string }) =>
    apiPost<Pipeline>(`projects/${slug}/pipelines`, data),
};
