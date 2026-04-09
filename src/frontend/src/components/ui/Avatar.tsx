import { cn } from "@/lib/utils";

interface AvatarProps {
  src?: string | null;
  name: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

const sizes = { sm: "h-6 w-6 text-xs", md: "h-8 w-8 text-sm", lg: "h-10 w-10 text-base" };

export function Avatar({ src, name, size = "md", className }: AvatarProps) {
  const initials = name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  if (src) {
    return (
      <img
        src={src}
        alt={name}
        className={cn("rounded-full object-cover", sizes[size], className)}
      />
    );
  }

  return (
    <div
      className={cn(
        "flex items-center justify-center rounded-full bg-brand-100 font-medium text-brand-700 dark:bg-brand-900 dark:text-brand-300",
        sizes[size],
        className,
      )}
    >
      {initials}
    </div>
  );
}
