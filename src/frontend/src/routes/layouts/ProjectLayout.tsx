import { NavLink, Outlet, useParams } from "react-router";
import { cn } from "@/lib/utils";
import { useProject } from "@/hooks/useProjects";
import { Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import {
  Code,
  CircleDot,
  PlayCircle,
  Settings,
  Users,
} from "lucide-react";

const projectTabs = [
  { to: "", label: "Files", icon: Code, end: true },
  { to: "issues", label: "Issues", icon: CircleDot },
  { to: "pipelines", label: "CI/CD", icon: PlayCircle },
  { to: "members", label: "Members", icon: Users },
  { to: "settings", label: "Settings", icon: Settings },
];

/**
 * Layout for project-scoped pages.
 * Shows project header with tabs (Files, Issues, CI/CD, Members, Settings).
 */
export function ProjectLayout() {
  const { slug } = useParams<{ slug: string }>();
  const { data: project, isLoading } = useProject(slug!);

  if (isLoading) return <Spinner size="lg" />;
  if (!project) return <div className="p-8 text-center">Project not found.</div>;

  return (
    <div>
      {/* Project header */}
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
            {project.name}
          </h1>
          <Badge
            variant={
              project.visibility === "Public"
                ? "success"
                : project.visibility === "Internal"
                  ? "info"
                  : "default"
            }
          >
            {project.visibility}
          </Badge>
        </div>
        {project.description && (
          <p className="mt-1 text-sm text-slate-500">{project.description}</p>
        )}
      </div>

      {/* Tabs */}
      <div className="mb-6 flex gap-1 border-b border-border">
        {projectTabs.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            end={tab.end}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition-colors",
                isActive
                  ? "border-brand-600 text-brand-600"
                  : "border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700",
              )
            }
          >
            <tab.icon className="h-4 w-4" />
            {tab.label}
          </NavLink>
        ))}
      </div>

      {/* Tab content */}
      <Outlet context={{ project }} />
    </div>
  );
}
