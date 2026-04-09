import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useCreateProject } from "@/hooks/useProjects";
import { VISIBILITY_OPTIONS } from "@/lib/constants";

export function NewProject() {
  const navigate = useNavigate();
  const createProject = useCreateProject();

  const [form, setForm] = useState({
    name: "",
    description: "",
    visibility: "Private" as const,
  });

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    createProject.mutate(
      {
        name: form.name,
        description: form.description || undefined,
        visibility: form.visibility,
      },
      {
        onSuccess: (project) => navigate(`/projects/${project.slug}`),
      },
    );
  }

  return (
    <div className="mx-auto max-w-lg space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
          Create New Project
        </h1>
        <p className="mt-1 text-sm text-slate-500">
          A project contains your repository, issues, and CI/CD pipelines.
        </p>
      </div>

      <form
        onSubmit={handleSubmit}
        className="space-y-5 rounded-lg border border-border bg-surface p-6"
      >
        <Input
          label="Project Name"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          required
          minLength={2}
          maxLength={100}
          autoFocus
          placeholder="My Awesome Project"
        />

        <div className="space-y-1.5">
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
            Description (optional)
          </label>
          <textarea
            value={form.description}
            onChange={(e) =>
              setForm((f) => ({ ...f, description: e.target.value }))
            }
            maxLength={1000}
            rows={3}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus:border-brand-500 focus:ring-1 focus:ring-brand-500 focus:outline-none dark:bg-slate-800"
            placeholder="A short description of your project"
          />
        </div>

        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Visibility Level
          </legend>
          {VISIBILITY_OPTIONS.map((opt) => (
            <label
              key={opt.value}
              className="flex cursor-pointer items-start gap-3 rounded-md border border-border p-3 hover:bg-slate-50 dark:hover:bg-slate-800"
            >
              <input
                type="radio"
                name="visibility"
                value={opt.value}
                checked={form.visibility === opt.value}
                onChange={() =>
                  setForm((f) => ({ ...f, visibility: opt.value as typeof f.visibility }))
                }
                className="mt-0.5 text-brand-600 focus:ring-brand-500"
              />
              <div>
                <div className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  {opt.label}
                </div>
                <div className="text-xs text-slate-500">{opt.description}</div>
              </div>
            </label>
          ))}
        </fieldset>

        {createProject.error && (
          <p className="text-sm text-red-600">
            Failed to create project. The name may already be taken.
          </p>
        )}

        <div className="flex gap-3">
          <Button type="submit" loading={createProject.isPending}>
            Create Project
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(-1)}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
