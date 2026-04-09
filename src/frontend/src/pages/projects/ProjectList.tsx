import { useState } from "react";
import { Link } from "react-router";
import { useProjects } from "@/hooks/useProjects";
import { Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { Pagination } from "@/components/ui/Pagination";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { formatRelativeTime } from "@/lib/utils";
import { Plus, Search, FolderGit2 } from "lucide-react";

export function ProjectList() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const { data, isLoading } = useProjects({ page, pageSize: 20, search: search || undefined });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
          Projects
        </h1>
        <Link to="/projects/new">
          <Button>
            <Plus className="h-4 w-4" />
            New Project
          </Button>
        </Link>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search projects..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="h-9 w-full rounded-md border border-border bg-white py-1 pr-3 pl-9 text-sm focus:border-brand-500 focus:ring-1 focus:ring-brand-500 focus:outline-none dark:bg-slate-800"
        />
      </div>

      {/* List */}
      {isLoading ? (
        <Spinner />
      ) : data?.items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border p-12 text-center">
          <FolderGit2 className="mx-auto h-12 w-12 text-slate-300" />
          <h3 className="mt-3 text-lg font-medium text-slate-600">No projects found</h3>
          <p className="mt-1 text-sm text-slate-400">
            {search ? "Try a different search term." : "Create your first project to get started."}
          </p>
        </div>
      ) : (
        <>
          <div className="divide-y divide-border rounded-lg border border-border bg-surface">
            {data?.items.map((project) => (
              <Link
                key={project.id}
                to={`/projects/${project.slug}`}
                className="flex items-center justify-between p-4 transition-colors hover:bg-slate-50 dark:hover:bg-slate-800/50"
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-slate-900 dark:text-white">
                      {project.ownerUsername} / {project.name}
                    </span>
                    <Badge
                      variant={project.visibility === "Public" ? "success" : "default"}
                    >
                      {project.visibility}
                    </Badge>
                  </div>
                  {project.description && (
                    <p className="mt-0.5 truncate text-sm text-slate-500">
                      {project.description}
                    </p>
                  )}
                </div>
                <span className="ml-4 shrink-0 text-xs text-slate-400">
                  Updated {formatRelativeTime(project.updatedAt || project.createdAt)}
                </span>
              </Link>
            ))}
          </div>

          {data && (
            <Pagination
              page={data.page}
              totalPages={data.totalPages}
              onPageChange={setPage}
            />
          )}
        </>
      )}
    </div>
  );
}
