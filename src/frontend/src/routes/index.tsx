import { createBrowserRouter } from "react-router";
import { RootLayout } from "./layouts/RootLayout";
import { DashboardLayout } from "./layouts/DashboardLayout";
import { ProjectLayout } from "./layouts/ProjectLayout";
import { ProtectedRoute } from "./ProtectedRoute";

// Eager-loaded pages
import { Login } from "@/pages/Login";
import { Register } from "@/pages/Register";
import { Dashboard } from "@/pages/Dashboard";
import { ProjectList } from "@/pages/projects/ProjectList";
import { NewProject } from "@/pages/projects/NewProject";
import { FileBrowser } from "@/pages/repository/FileBrowser";
import { FileViewer } from "@/pages/repository/FileViewer";
import { IssueList } from "@/pages/issues/IssueList";
import { IssueDetail } from "@/pages/issues/IssueDetail";
import { NewIssue } from "@/pages/issues/NewIssue";
import { PipelineList } from "@/pages/pipelines/PipelineList";
import { PipelineDetail } from "@/pages/pipelines/PipelineDetail";

/**
 * Application route tree.
 *
 * Structure:
 *   RootLayout (SignalR + Toaster)
 *   ├── /login          — public
 *   ├── /register       — public
 *   └── DashboardLayout — authenticated (Sidebar + Header)
 *       ├── /           — Dashboard
 *       ├── /projects   — Project list
 *       ├── /projects/new — New project form
 *       └── /projects/:slug — ProjectLayout (tabs)
 *           ├── (index)        — File browser
 *           ├── /file          — File viewer
 *           ├── /issues        — Issue list
 *           ├── /issues/new    — New issue form
 *           ├── /issues/:num   — Issue detail
 *           ├── /pipelines     — Pipeline list
 *           ├── /pipelines/:id — Pipeline detail
 *           ├── /members       — Members (Phase 6)
 *           └── /settings      — Settings (Phase 6)
 */
export const router = createBrowserRouter([
  {
    element: <RootLayout />,
    children: [
      // Public auth routes (no sidebar/header)
      { path: "/login", element: <Login /> },
      { path: "/register", element: <Register /> },

      // Authenticated routes with dashboard layout
      {
        element: (
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Dashboard /> },
          { path: "projects", element: <ProjectList /> },
          { path: "projects/new", element: <NewProject /> },

          // Project-scoped routes
          {
            path: "projects/:slug",
            element: <ProjectLayout />,
            children: [
              { index: true, element: <FileBrowser /> },
              { path: "file", element: <FileViewer /> },
              { path: "issues", element: <IssueList /> },
              { path: "issues/new", element: <NewIssue /> },
              { path: "issues/:issueNumber", element: <IssueDetail /> },
              { path: "pipelines", element: <PipelineList /> },
              { path: "pipelines/:pipelineId", element: <PipelineDetail /> },
              {
                path: "members",
                lazy: async () => {
                  return { element: <div>Members — Phase 6</div> };
                },
              },
              {
                path: "settings",
                lazy: async () => {
                  return { element: <div>Settings — Phase 6</div> };
                },
              },
            ],
          },
        ],
      },
    ],
  },
]);
