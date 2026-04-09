import { useState } from "react";
import { useParams, useNavigate } from "react-router";
import { useCreateIssue } from "@/hooks/useIssues";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { toast } from "sonner";

export function NewIssue() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const createIssue = useCreateIssue(slug!);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    createIssue.mutate(
      { title, description: description || undefined },
      {
        onSuccess: (issue) => {
          toast.success(`Issue #${issue.issueNumber} created`);
          navigate(`/projects/${slug}/issues/${issue.issueNumber}`);
        },
        onError: () => toast.error("Failed to create issue"),
      },
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h2 className="text-xl font-bold text-slate-900 dark:text-white">New Issue</h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Issue title"
          required
        />

        <div className="space-y-1.5">
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
            Description
          </label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Describe the issue..."
            rows={6}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500 focus:outline-none dark:bg-slate-800 dark:text-slate-200"
          />
        </div>

        <div className="flex gap-3">
          <Button type="submit" loading={createIssue.isPending}>
            Create Issue
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(`/projects/${slug}/issues`)}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
