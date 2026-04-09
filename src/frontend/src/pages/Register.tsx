import { type FormEvent, useState } from "react";
import { Link } from "react-router";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useRegister } from "@/hooks/useAuth";
import { FolderGit2 } from "lucide-react";
import { APP_NAME } from "@/lib/constants";

export function Register() {
  const [form, setForm] = useState({
    username: "",
    email: "",
    displayName: "",
    password: "",
  });
  const register = useRegister();

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    register.mutate(form);
  }

  const updateField = (field: string) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((prev) => ({ ...prev, [field]: e.target.value }));

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-secondary p-4">
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center">
          <FolderGit2 className="mx-auto h-10 w-10 text-brand-600" />
          <h1 className="mt-3 text-2xl font-bold text-slate-900 dark:text-white">
            Create your {APP_NAME} account
          </h1>
        </div>

        <form
          onSubmit={handleSubmit}
          className="space-y-4 rounded-lg border border-border bg-surface p-6 shadow-sm"
        >
          <Input
            label="Display Name"
            value={form.displayName}
            onChange={updateField("displayName")}
            required
            autoFocus
          />
          <Input
            label="Username"
            value={form.username}
            onChange={updateField("username")}
            required
            pattern="[a-zA-Z0-9_-]+"
          />
          <Input
            label="Email"
            type="email"
            value={form.email}
            onChange={updateField("email")}
            required
          />
          <Input
            label="Password"
            type="password"
            value={form.password}
            onChange={updateField("password")}
            required
            minLength={8}
          />

          {register.error && (
            <p className="text-sm text-red-600">
              Registration failed. Username or email may already be taken.
            </p>
          )}

          <Button type="submit" className="w-full" loading={register.isPending}>
            Create Account
          </Button>
        </form>

        <p className="text-center text-sm text-slate-500">
          Already have an account?{" "}
          <Link
            to="/login"
            className="font-medium text-brand-600 hover:text-brand-700"
          >
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
