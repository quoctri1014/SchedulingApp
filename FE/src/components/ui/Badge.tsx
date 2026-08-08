import type { ReactNode } from 'react';
import { cn } from './Button';

export function Badge({ 
  children, 
  variant = 'default',
  className
}: { 
  children: ReactNode; 
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}) {
  const variants = {
    default: "bg-slate-100 text-slate-700 border-slate-200/60",
    success: "bg-emerald-50 text-emerald-700 border-emerald-200/60",
    warning: "bg-amber-50 text-amber-700 border-amber-200/60",
    danger: "bg-rose-50 text-rose-700 border-rose-200/60",
    info: "bg-indigo-50 text-indigo-700 border-indigo-200/60",
  };

  return (
    <span className={cn("inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold tracking-wide uppercase border", variants[variant], className)}>
      {children}
    </span>
  );
}
