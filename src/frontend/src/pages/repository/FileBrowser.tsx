import { useParams, useSearchParams, Link } from "react-router";
import { useFileTree, useCommitLog } from "@/hooks/useRepository";
import { Spinner } from "@/components/ui/Spinner";
import { formatRelativeTime, formatFileSize, shortSha } from "@/lib/utils";
import { File, Folder, GitCommit, Copy } from "lucide-react";
import { toast } from "sonner";

export function FileBrowser() {
  const { slug } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const ref = searchParams.get("ref") || "main";
  const path = searchParams.get("path") || undefined;

  const { data: files, isLoading } = useFileTree(slug!, { ref, path });
  const { data: commits } = useCommitLog(slug!, { ref, count: 1 });

  const lastCommit = commits?.[0];

  function copyCloneUrl() {
    const url = `${window.location.origin}/${slug}.git`;
    navigator.clipboard.writeText(url);
    toast.success("Clone URL copied!");
  }

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      {/* Clone URL bar */}
      <div className="flex items-center gap-2 rounded-md border border-border bg-surface-secondary p-3">
        <code className="flex-1 truncate text-sm text-slate-600 dark:text-slate-400">
          git clone {window.location.origin}/{slug}.git
        </code>
        <button
          onClick={copyCloneUrl}
          className="rounded p-1.5 text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700"
          title="Copy clone URL"
        >
          <Copy className="h-4 w-4" />
        </button>
      </div>

      {/* Last commit bar */}
      {lastCommit && (
        <div className="flex items-center gap-3 rounded-md border border-border bg-surface-secondary px-4 py-2.5 text-sm">
          <GitCommit className="h-4 w-4 text-slate-400" />
          <span className="flex-1 truncate text-slate-700 dark:text-slate-300">
            {lastCommit.message}
          </span>
          <span className="shrink-0 font-mono text-xs text-slate-500">
            {shortSha(lastCommit.sha)}
          </span>
          <span className="shrink-0 text-xs text-slate-400">
            {formatRelativeTime(lastCommit.timestamp)}
          </span>
        </div>
      )}

      {/* Breadcrumbs */}
      {path && (
        <nav className="flex items-center gap-1 text-sm">
          <Link
            to={`/projects/${slug}?ref=${ref}`}
            className="text-brand-600 hover:underline"
          >
            root
          </Link>
          {path.split("/").map((segment, i, arr) => {
            const segPath = arr.slice(0, i + 1).join("/");
            const isLast = i === arr.length - 1;
            return (
              <span key={segPath} className="flex items-center gap-1">
                <span className="text-slate-400">/</span>
                {isLast ? (
                  <span className="text-slate-700 dark:text-slate-300">
                    {segment}
                  </span>
                ) : (
                  <Link
                    to={`/projects/${slug}?ref=${ref}&path=${segPath}`}
                    className="text-brand-600 hover:underline"
                  >
                    {segment}
                  </Link>
                )}
              </span>
            );
          })}
        </nav>
      )}

      {/* File table */}
      <div className="overflow-hidden rounded-lg border border-border">
        {files?.length === 0 ? (
          <div className="p-8 text-center text-sm text-slate-500">
            This repository is empty.
          </div>
        ) : (
          <table className="w-full text-sm">
            <tbody className="divide-y divide-border">
              {files?.map((entry) => (
                <tr
                  key={entry.path}
                  className="hover:bg-slate-50 dark:hover:bg-slate-800/50"
                >
                  <td className="px-4 py-2.5">
                    <Link
                      to={
                        entry.type === "tree"
                          ? `/projects/${slug}?ref=${ref}&path=${entry.path}`
                          : `/projects/${slug}/file?ref=${ref}&path=${entry.path}`
                      }
                      className="flex items-center gap-2 text-slate-700 hover:text-brand-600 dark:text-slate-300"
                    >
                      {entry.type === "tree" ? (
                        <Folder className="h-4 w-4 text-brand-400" />
                      ) : (
                        <File className="h-4 w-4 text-slate-400" />
                      )}
                      {entry.name}
                    </Link>
                  </td>
                  <td className="px-4 py-2.5 text-right text-xs text-slate-400">
                    {entry.type === "blob" ? formatFileSize(entry.size) : ""}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
