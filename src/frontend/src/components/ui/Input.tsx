import { forwardRef, type InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, label, error, id, ...props }, ref) => {
    const inputId = id || label?.toLowerCase().replace(/\s+/g, "-");

    return (
      <div className="space-y-1.5">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-sm font-medium text-slate-700 dark:text-slate-300"
          >
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={cn(
            "flex h-9 w-full rounded-md border border-border bg-white px-3 py-1 text-sm shadow-sm transition-colors",
            "placeholder:text-slate-400",
            "focus:border-brand-500 focus:ring-1 focus:ring-brand-500 focus:outline-none",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "dark:bg-slate-800 dark:text-slate-200",
            error && "border-red-500 focus:border-red-500 focus:ring-red-500",
            className,
          )}
          {...props}
        />
        {error && (
          <p className="text-xs text-red-600 dark:text-red-400">{error}</p>
        )}
      </div>
    );
  },
);

Input.displayName = "Input";
