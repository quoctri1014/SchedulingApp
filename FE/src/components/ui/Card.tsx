import type { ReactNode } from 'react';
import { cn } from './Button';

export function Card({ className, children }: { className?: string; children?: ReactNode }) {
  return (
    <div className={cn("glass-card rounded-2xl overflow-hidden relative group", className)}>
      <div className="absolute inset-0 bg-gradient-to-br from-white/40 to-white/0 opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none"></div>
      {children}
    </div>
  );
}

export function CardHeader({ className, children }: { className?: string; children?: ReactNode }) {
  return <div className={cn("px-6 py-5 border-b border-slate-100/50", className)}>{children}</div>;
}

export function CardTitle({ className, children }: { className?: string; children?: ReactNode }) {
  return <h3 className={cn("text-lg font-bold text-slate-800 tracking-tight", className)}>{children}</h3>;
}

export function CardContent({ className, children }: { className?: string; children?: ReactNode }) {
  return <div className={cn("p-6", className)}>{children}</div>;
}
