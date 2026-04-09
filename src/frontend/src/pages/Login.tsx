import { type FormEvent, useState } from "react";
import { Link } from "react-router";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useLogin } from "@/hooks/useAuth";
import { FolderGit2 } from "lucide-react";
import { APP_NAME } from "@/lib/constants";

export function Login() {
  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const login = useLogin();

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    login.mutate({ usernameOrEmail, password });
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-secondary p-4">
      <div className="w-full max-w-sm space-y-6">
        {/* Logo */}
        <div className="text-center">
          <FolderGit2 className="mx-auto h-10 w-10 text-brand-600" />
          <h1 className="mt-3 text-2xl font-bold text-slate-900 dark:text-white">
            Sign in to {APP_NAME}
          </h1>
        </div>

        {/* Form */}
        <form
          onSubmit={handleSubmit}
          className="space-y-4 rounded-lg border border-border bg-surface p-6 shadow-sm"
        >
          <Input
            label="Username or Email"
            value={usernameOrEmail}
            onChange={(e) => setUsernameOrEmail(e.target.value)}
            required
            autoFocus
          />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />

          {login.error && (
            <p className="text-sm text-red-600">
              Invalid credentials. Please try again.
            </p>
          )}

          <Button
            type="submit"
            className="w-full"
            loading={login.isPending}
          >
            Sign in
          </Button>
        </form>

        <p className="text-center text-sm text-slate-500">
          Don&apos;t have an account?{" "}
          <Link
            to="/register"
            className="font-medium text-brand-600 hover:text-brand-700"
          >
            Register
          </Link>
        </p>
      </div>
    </div>
  );
}
