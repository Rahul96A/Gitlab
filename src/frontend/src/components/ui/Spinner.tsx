import { cn } from "@/lib/utils";
import { Loader2 } from "lucide-react";

interface SpinnerProps {
  size?: "sm" | "md" | "lg";
  className?: string;
}

const sizes = { sm: "h-4 w-4", md: "h-6 w-6", lg: "h-10 w-10" };

export function Spinner({ size = "md", className }: SpinnerProps) {
  return (
    <div className="flex items-center justify-center p-8">
      <Loader2
        className={cn("animate-spin text-brand-600", sizes[size], className)}
      />
    </div>
  );
}
