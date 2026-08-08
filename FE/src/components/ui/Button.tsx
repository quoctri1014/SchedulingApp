import type { ReactNode, ButtonHTMLAttributes } from 'react';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  children: ReactNode;
}

export function Button({ variant = 'primary', size = 'md', className, children, ...props }: ButtonProps) {
  const baseStyle = "inline-flex items-center justify-center rounded-xl font-medium transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none active:scale-[0.98]";
  
  const variants = {
    primary: "bg-gradient-primary text-white shadow-lg shadow-indigo-500/30 hover:shadow-indigo-500/50 hover:opacity-90 focus:ring-indigo-500",
    secondary: "bg-white text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50 hover:text-indigo-600 focus:ring-slate-500",
    outline: "bg-transparent text-indigo-600 border-2 border-indigo-100 hover:border-indigo-200 hover:bg-indigo-50 focus:ring-indigo-500",
    danger: "bg-gradient-to-r from-red-500 to-rose-600 text-white shadow-lg shadow-red-500/30 hover:shadow-red-500/50 hover:opacity-90 focus:ring-red-500",
    ghost: "bg-transparent text-slate-600 hover:bg-slate-100 hover:text-indigo-600 focus:ring-slate-500"
  };
  
  const sizes = {
    sm: "h-9 px-4 text-sm",
    md: "h-11 px-6 text-sm",
    lg: "h-14 px-8 text-base"
  };

  return (
    <button 
      className={cn(baseStyle, variants[variant], sizes[size], className)} 
      {...props}
    >
      {children}
    </button>
  );
}
