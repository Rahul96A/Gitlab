import { useState } from "react";
import { useParams, Link } from "react-router";
import { useIssues } from "@/hooks/useIssues";
import { Spinner } from "@/components/ui/Spinner";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Pagination } from "@/components/ui/Pagination";
import { formatRelativeTime } from "@/lib/utils";
import { CircleDot, CircleCheck, Plus, MessageSquare } from "lucide-react";

export function IssueList() {
  const { slug } = useParams<{ slug: string }>();
  const [page, setPage] = useState(1);
  const { data, isLoading } = useIssues(slug!, { page, pageSize: 20 });

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-white">Issues</h2>
        <Link to={`/projects/${slug}/issues/new`}>
          <Button size="sm">
            <Plus className="h-4 w-4" />
            New Issue
          </Button>
        </Link>
      </div>

      {data?.items.length === 0 ? (
        <div className="rounded-lg border border-border p-8 text-center text-sm text-slate-500">
          No issues yet. Create your first issue to get started.
        </div>
      ) : (
        <div className="divide-y divide-border rounded-lg border border-border">
          {data?.items.map((issue) => (
            <Link
              key={issue.id}
              to={`/projects/${slug}/issues/${issue.issueNumber}`}
              className="flex items-start gap-3 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50"
            >
              {issue.status === "Open" ? (
                <CircleDot className="mt-0.5 h-4 w-4 shrink-0 text-green-600" />
              ) : (
                <CircleCheck className="mt-0.5 h-4 w-4 shrink-0 text-purple-600" />
              )}
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-slate-900 dark:text-white">
                    {issue.title}
                  </span>
                  {issue.labels.map((label) => (
                    <span
                      key={label.id}
                      className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium text-white"
                      style={{ backgroundColor: label.color }}
                    >
                      {label.name}
                    </span>
                  ))}
                </div>
                <div className="mt-1 flex items-center gap-3 text-xs text-slate-500">
                  <span>#{issue.issueNumber}</span>
                  <span>opened {formatRelativeTime(issue.createdAt)} by {issue.authorUsername}</span>
                  {issue.assigneeUsername && (
                    <Badge variant="default">{issue.assigneeUsername}</Badge>
                  )}
                </div>
              </div>
              {issue.commentCount > 0 && (
                <div className="flex items-center gap-1 text-xs text-slate-400">
                  <MessageSquare className="h-3.5 w-3.5" />
                  {issue.commentCount}
                </div>
              )}
            </Link>
          ))}
        </div>
      )}

      {data && (
        <Pagination
          page={data.page}
          totalPages={data.totalPages}
          onPageChange={setPage}
        />
      )}
    </div>
  );
}
