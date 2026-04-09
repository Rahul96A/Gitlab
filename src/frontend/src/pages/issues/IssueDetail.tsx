import { useState } from "react";
import { useParams, Link } from "react-router";
import { useIssue, useIssueComments, useAddComment } from "@/hooks/useIssues";
import { issuesApi } from "@/api/endpoints/issues";
import { Spinner } from "@/components/ui/Spinner";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Avatar } from "@/components/ui/Avatar";
import { formatRelativeTime } from "@/lib/utils";
import { CircleDot, CircleCheck, ArrowLeft } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export function IssueDetail() {
  const { slug, issueNumber } = useParams<{ slug: string; issueNumber: string }>();
  const num = Number(issueNumber);
  const { data: issue, isLoading } = useIssue(slug!, num);
  const { data: comments } = useIssueComments(slug!, num);
  const addComment = useAddComment(slug!, num);
  const [body, setBody] = useState("");
  const queryClient = useQueryClient();

  const toggleStatus = useMutation({
    mutationFn: () =>
      issuesApi.update(slug!, num, {
        status: issue?.status === "Open" ? "Closed" : "Open",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["issues", slug, num] });
    },
  });

  function handleSubmitComment(e: React.FormEvent) {
    e.preventDefault();
    if (!body.trim()) return;
    addComment.mutate({ body }, { onSuccess: () => setBody("") });
  }

  if (isLoading) return <Spinner />;
  if (!issue) return <div className="p-8 text-center">Issue not found.</div>;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <Link
          to={`/projects/${slug}/issues`}
          className="mb-3 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600"
        >
          <ArrowLeft className="h-4 w-4" /> Back to issues
        </Link>
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold text-slate-900 dark:text-white">
              {issue.title}
              <span className="ml-2 font-normal text-slate-400">#{issue.issueNumber}</span>
            </h2>
            <div className="mt-2 flex items-center gap-3 text-sm text-slate-500">
              {issue.status === "Open" ? (
                <Badge variant="success"><CircleDot className="mr-1 h-3 w-3" /> Open</Badge>
              ) : (
                <Badge variant="default"><CircleCheck className="mr-1 h-3 w-3" /> Closed</Badge>
              )}
              <span>Opened {formatRelativeTime(issue.createdAt)} by {issue.authorUsername}</span>
              {issue.assigneeUsername && (
                <span>Assigned to <strong>{issue.assigneeUsername}</strong></span>
              )}
            </div>
          </div>
          <Button
            variant={issue.status === "Open" ? "secondary" : "primary"}
            size="sm"
            onClick={() => toggleStatus.mutate()}
            loading={toggleStatus.isPending}
          >
            {issue.status === "Open" ? "Close issue" : "Reopen issue"}
          </Button>
        </div>

        {/* Labels */}
        {issue.labels.length > 0 && (
          <div className="mt-3 flex gap-1.5">
            {issue.labels.map((label) => (
              <span
                key={label.id}
                className="rounded-full px-2.5 py-0.5 text-xs font-medium text-white"
                style={{ backgroundColor: label.color }}
              >
                {label.name}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Description */}
      {issue.description && (
        <div className="rounded-lg border border-border bg-surface-secondary p-4 text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap">
          {issue.description}
        </div>
      )}

      {/* Comments */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">
          Comments ({comments?.length ?? 0})
        </h3>

        {comments?.map((comment) => (
          <div key={comment.id} className="flex gap-3">
            <Avatar name={comment.authorUsername} size="sm" />
            <div className="min-w-0 flex-1 rounded-lg border border-border p-3">
              <div className="mb-1 flex items-center gap-2 text-xs text-slate-500">
                <span className="font-medium text-slate-700 dark:text-slate-300">
                  {comment.authorUsername}
                </span>
                <span>{formatRelativeTime(comment.createdAt)}</span>
              </div>
              <p className="text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap">
                {comment.body}
              </p>
            </div>
          </div>
        ))}

        {/* Comment form */}
        <form onSubmit={handleSubmitComment} className="space-y-3">
          <textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="Leave a comment..."
            rows={3}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500 focus:outline-none dark:bg-slate-800 dark:text-slate-200"
          />
          <Button type="submit" size="sm" loading={addComment.isPending} disabled={!body.trim()}>
            Comment
          </Button>
        </form>
      </div>
    </div>
  );
}
