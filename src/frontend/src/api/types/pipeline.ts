export type PipelineStatus =
  | "Pending"
  | "Running"
  | "Success"
  | "Failed"
  | "Canceled";

export type JobStatus =
  | "Pending"
  | "Running"
  | "Success"
  | "Failed"
  | "Skipped"
  | "Canceled";

export interface Pipeline {
  id: string;
  ref: string;
  commitSha: string;
  status: PipelineStatus;
  startedAt: string | null;
  finishedAt: string | null;
  triggeredByUsername: string;
  jobCount: number;
  createdAt: string;
}

export interface PipelineJob {
  id: string;
  name: string;
  stage: string;
  status: JobStatus;
  log: string;
  startedAt: string | null;
  finishedAt: string | null;
  artifactUrl: string | null;
}
