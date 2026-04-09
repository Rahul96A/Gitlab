import { useParams, Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { pipelinesApi } from "@/api/endpoints/pipelines";
import { Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { formatRelativeTime, shortSha } from "@/lib/utils";
import { PIPELINE_STATUS_COLORS } from "@/lib/constants";
import { ArrowLeft, GitBranch, Clock, CheckCircle, XCircle, Loader2, SkipForward } from "lucide-react";
import type { JobStatus } from "@/api/types/pipeline";

const jobStatusIcon: Record<string, typeof CheckCircle> = {
  Success: CheckCircle,
  Failed: XCircle,
  Running: Loader2,
  Skipped: SkipForward,
};

const jobStatusVariant: Record<string, "success" | "danger" | "info" | "warning" | "default"> = {
  Success: "success",
  Failed: "danger",
  Running: "info",
  Pending: "warning",
  Skipped: "default",
};

export function PipelineDetail() {
  const { slug, pipelineId } = useParams<{ slug: string; pipelineId: string }>();

  const { data: pipeline, isLoading } = useQuery({
    queryKey: ["pipelines", slug, pipelineId],
    queryFn: () => pipelinesApi.getById(slug!, pipelineId!),
    enabled: !!slug && !!pipelineId,
  });

  const { data: jobs } = useQuery({
    queryKey: ["pipelines", slug, pipelineId, "jobs"],
    queryFn: () => pipelinesApi.getJobs(slug!, pipelineId!),
    enabled: !!slug && !!pipelineId,
    refetchInterval: pipeline?.status === "Running" ? 3000 : false,
  });

  if (isLoading) return <Spinner />;
  if (!pipeline) return <div className="p-8 text-center">Pipeline not found.</div>;

  // Group jobs by stage
  const stages = new Map<string, typeof jobs>();
  jobs?.forEach((job) => {
    const list = stages.get(job.stage) ?? [];
    list.push(job);
    stages.set(job.stage, list);
  });

  return (
    <div className="space-y-6">
      <Link
        to={`/projects/${slug}/pipelines`}
        className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600"
      >
        <ArrowLeft className="h-4 w-4" /> Back to pipelines
      </Link>

      {/* Pipeline header */}
      <div className="flex items-center gap-4">
        <span
          className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${PIPELINE_STATUS_COLORS[pipeline.status] ?? ""}`}
        >
          {pipeline.status}
        </span>
        <div>
          <div className="flex items-center gap-2 text-sm">
            <GitBranch className="h-4 w-4 text-slate-400" />
            <span className="font-medium">{pipeline.ref}</span>
            <span className="font-mono text-xs text-slate-400">{shortSha(pipeline.commitSha)}</span>
          </div>
          <div className="mt-1 flex items-center gap-3 text-xs text-slate-500">
            <span>Triggered by {pipeline.triggeredByUsername}</span>
            <span className="flex items-center gap-1">
              <Clock className="h-3 w-3" /> {formatRelativeTime(pipeline.createdAt)}
            </span>
          </div>
        </div>
      </div>

      {/* Stages + Jobs */}
      <div className="space-y-4">
        {[...stages.entries()].map(([stage, stageJobs]) => (
          <div key={stage}>
            <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">
              Stage: {stage}
            </h3>
            <div className="space-y-2">
              {stageJobs!.map((job) => {
                const Icon = jobStatusIcon[job.status] ?? Clock;
                return (
                  <div
                    key={job.id}
                    className="rounded-lg border border-border"
                  >
                    <div className="flex items-center gap-3 px-4 py-2.5">
                      <Icon className={`h-4 w-4 ${job.status === "Running" ? "animate-spin text-blue-500" : job.status === "Success" ? "text-green-500" : job.status === "Failed" ? "text-red-500" : "text-slate-400"}`} />
                      <span className="flex-1 text-sm font-medium text-slate-700 dark:text-slate-300">
                        {job.name}
                      </span>
                      <Badge variant={jobStatusVariant[job.status] ?? "default"}>
                        {job.status}
                      </Badge>
                    </div>
                    {job.log && (
                      <pre className="border-t border-border bg-slate-950 p-4 text-xs text-green-400 overflow-x-auto rounded-b-lg">
                        {job.log}
                      </pre>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
