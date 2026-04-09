import { useState } from "react";
import { useParams, Link } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { pipelinesApi } from "@/api/endpoints/pipelines";
import { Spinner } from "@/components/ui/Spinner";
import { Button } from "@/components/ui/Button";
import { Pagination } from "@/components/ui/Pagination";
import { formatRelativeTime, shortSha } from "@/lib/utils";
import { PIPELINE_STATUS_COLORS } from "@/lib/constants";
import { PlayCircle, GitBranch, Clock } from "lucide-react";
import { toast } from "sonner";

export function PipelineList() {
  const { slug } = useParams<{ slug: string }>();
  const [page, setPage] = useState(1);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ["pipelines", slug, page],
    queryFn: () => pipelinesApi.list(slug!, { page, pageSize: 20 }),
    enabled: !!slug,
  });

  const trigger = useMutation({
    mutationFn: () => pipelinesApi.trigger(slug!, { ref: "main" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pipelines", slug] });
      toast.success("Pipeline triggered");
    },
    onError: () => toast.error("Failed to trigger pipeline"),
  });

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-white">CI/CD Pipelines</h2>
        <Button size="sm" onClick={() => trigger.mutate()} loading={trigger.isPending}>
          <PlayCircle className="h-4 w-4" />
          Run Pipeline
        </Button>
      </div>

      {data?.items.length === 0 ? (
        <div className="rounded-lg border border-border p-8 text-center text-sm text-slate-500">
          No pipelines yet. Trigger your first pipeline to get started.
        </div>
      ) : (
        <div className="divide-y divide-border rounded-lg border border-border">
          {data?.items.map((pipeline) => (
            <Link
              key={pipeline.id}
              to={`/projects/${slug}/pipelines/${pipeline.id}`}
              className="flex items-center gap-4 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50"
            >
              <span
                className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${PIPELINE_STATUS_COLORS[pipeline.status] ?? ""}`}
              >
                {pipeline.status}
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2 text-sm">
                  <GitBranch className="h-3.5 w-3.5 text-slate-400" />
                  <span className="font-medium text-slate-700 dark:text-slate-300">{pipeline.ref}</span>
                  <span className="font-mono text-xs text-slate-400">{shortSha(pipeline.commitSha)}</span>
                </div>
                <div className="mt-0.5 text-xs text-slate-500">
                  by {pipeline.triggeredByUsername}
                </div>
              </div>
              <div className="flex items-center gap-1 text-xs text-slate-400">
                <Clock className="h-3.5 w-3.5" />
                {formatRelativeTime(pipeline.createdAt)}
              </div>
              <span className="text-xs text-slate-400">{pipeline.jobCount} jobs</span>
            </Link>
          ))}
        </div>
      )}

      {data && (
        <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />
      )}
    </div>
  );
}
