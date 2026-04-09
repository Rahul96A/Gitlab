interface GitLabLogoProps {
  className?: string;
}

/**
 * GitLab-style tanuki/fox logo in orange brand colors.
 */
export function GitLabLogo({ className = "h-10 w-10" }: GitLabLogoProps) {
  return (
    <svg viewBox="0 0 380 380" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      <path d="M190 353.5L253.3 158.7H126.7L190 353.5Z" fill="#E24329" />
      <path d="M190 353.5L126.7 158.7H30.2L190 353.5Z" fill="#FC6D26" />
      <path d="M30.2 158.7L9.9 221.2C8.1 226.7 10 232.8 14.8 236.3L190 353.5L30.2 158.7Z" fill="#FCA326" />
      <path d="M30.2 158.7H126.7L84.4 28.5C82.4 22.5 74 22.5 72 28.5L30.2 158.7Z" fill="#E24329" />
      <path d="M190 353.5L253.3 158.7H349.8L190 353.5Z" fill="#FC6D26" />
      <path d="M349.8 158.7L370.1 221.2C371.9 226.7 370 232.8 365.2 236.3L190 353.5L349.8 158.7Z" fill="#FCA326" />
      <path d="M349.8 158.7H253.3L295.6 28.5C297.6 22.5 306 22.5 308 28.5L349.8 158.7Z" fill="#E24329" />
    </svg>
  );
}
