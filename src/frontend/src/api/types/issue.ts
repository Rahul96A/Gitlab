export interface Issue {
  id: string;
  issueNumber: number;
  title: string;
  description: string | null;
  status: "Open" | "Closed";
  projectId: string;
  assigneeId: string | null;
  assigneeUsername: string | null;
  authorId: string;
  authorUsername: string;
  labels: Label[];
  commentCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface Label {
  id: string;
  name: string;
  color: string;
  description: string | null;
}

export interface IssueComment {
  id: string;
  body: string;
  authorId: string;
  authorUsername: string;
  authorAvatarUrl: string | null;
  createdAt: string;
}

export interface CreateIssueRequest {
  title: string;
  description?: string;
  assigneeId?: string;
  labelIds?: string[];
}

export interface CreateCommentRequest {
  body: string;
}
