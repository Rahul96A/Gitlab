import { useParams, useSearchParams, Link } from "react-router";
import { useFileContent, useCommitLog } from "@/hooks/useRepository";
import { Spinner } from "@/components/ui/Spinner";
import { formatFileSize } from "@/lib/utils";
import { ArrowLeft, File, Copy } from "lucide-react";
import { toast } from "sonner";

export function FileViewer() {
  const { slug } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const ref = searchParams.get("ref") || "main";
  const path = searchParams.get("path") || "";

  const { data: file, isLoading } = useFileContent(slug!, path, ref);

  const fileName = path.split("/").pop() ?? path;
  const dirPath = path.split("/").slice(0, -1).join("/");

  function copyContent() {
    if (file?.content) {
      navigator.clipboard.writeText(file.content);
      toast.success("File content copied!");
    }
  }

  if (isLoading) return <Spinner />;
  if (!file) return <div className="p-8 text-center text-sm text-slate-500">File not found.</div>;

  const lines = file.encoding === "utf-8" ? file.content.split("\n") : [];
  const isBinary = file.encoding === "base64";

  return (
    <div className="space-y-4">
      {/* Breadcrumbs */}
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
                <span className="text-slate-700 dark:text-slate-300">{segment}</span>
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

      {/* File header */}
      <div className="flex items-center justify-between rounded-t-lg border border-border bg-surface-secondary px-4 py-2.5">
        <div className="flex items-center gap-2 text-sm">
          <File className="h-4 w-4 text-slate-400" />
          <span className="font-medium text-slate-700 dark:text-slate-300">{fileName}</span>
          <span className="text-xs text-slate-400">{formatFileSize(file.size)}</span>
          {!isBinary && (
            <span className="text-xs text-slate-400">{lines.length} lines</span>
          )}
        </div>
        {!isBinary && (
          <button
            onClick={copyContent}
            className="rounded p-1.5 text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700"
            title="Copy file content"
          >
            <Copy className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* File content */}
      <div className="overflow-x-auto rounded-b-lg border border-t-0 border-border">
        {isBinary ? (
          <div className="p-8 text-center text-sm text-slate-500">
            Binary file — cannot display content.
          </div>
        ) : (
          <table className="w-full text-sm">
            <tbody>
              {lines.map((line, i) => (
                <tr key={i} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td className="select-none border-r border-border px-3 py-0 text-right font-mono text-xs text-slate-400">
                    {i + 1}
                  </td>
                  <td className="whitespace-pre px-4 py-0 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {line}
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
