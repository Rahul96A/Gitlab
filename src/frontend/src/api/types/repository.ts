export interface GitFileEntry {
  name: string;
  path: string;
  type: "blob" | "tree";
  size: number;
}

export interface GitFileContent {
  path: string;
  content: string;
  size: number;
  encoding: "utf-8" | "base64";
}

export interface GitCommitInfo {
  sha: string;
  message: string;
  authorName: string;
  authorEmail: string;
  timestamp: string;
}
