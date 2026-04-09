import { Link } from "react-router";
import { useProjects } from "@/hooks/useProjects";
import { useAuthStore } from "@/stores/authStore";
import { Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { formatRelativeTime } from "@/lib/utils";
import { FolderGit2, Plus, CircleDot } from "lucide-react";
import { Button } from "@/components/ui/Button";

export function Dashboard() {
  const { user } = useAuthStore();
  const { data, isLoading } = useProjects({ pageSize: 10 });

  return (
    <div className="space-y-6">
      {/* Welcome */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
            Welcome back, {user?.displayName}
          </h1>
          <p className="text-sm text-slate-500">
            Here&apos;s what&apos;s happening across your projects.
          </p>
        </div>
        <Link to="/projects/new">
          <Button>
            <Plus className="h-4 w-4" />
            New Project
          </Button>
        </Link>
      </div>

      {/* Recent Projects */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-slate-800 dark:text-slate-200">
          Recent Projects
        </h2>
        {isLoading ? (
          <Spinner />
        ) : data?.items.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <FolderGit2 className="mx-auto h-10 w-10 text-slate-300" />
            <p className="mt-2 text-sm text-slate-500">
              No projects yet.{" "}
              <Link
                to="/projects/new"
                className="font-medium text-brand-600 hover:text-brand-700"
              >
                Create your first project
              </Link>
            </p>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {data?.items.map((project) => (
              <Link
                key={project.id}
                to={`/projects/${project.slug}`}
                className="group rounded-lg border border-border bg-surface p-4 transition-shadow hover:shadow-md"
              >
                <div className="flex items-start justify-between">
                  <div className="min-w-0 flex-1">
                    <h3 className="truncate font-semibold text-slate-900 group-hover:text-brand-600 dark:text-white">
                      {project.name}
                    </h3>
                    <p className="mt-1 text-xs text-slate-500">
                      {project.ownerUsername} / {project.slug}
                    </p>
                  </div>
                  <Badge
                    variant={
                      project.visibility === "Public" ? "success" : "default"
                    }
                  >
                    {project.visibility}
                  </Badge>
                </div>

                {project.description && (
                  <p className="mt-2 line-clamp-2 text-sm text-slate-500">
                    {project.description}
                  </p>
                )}

                <div className="mt-3 flex items-center gap-4 text-xs text-slate-400">
                  <span className="flex items-center gap-1">
                    <CircleDot className="h-3 w-3" />
                    {project.issueCount} issues
                  </span>
                  <span>{formatRelativeTime(project.createdAt)}</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
