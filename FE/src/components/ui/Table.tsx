import type { ReactNode } from 'react';
import { cn } from './Button';

export function Table({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div className="w-full overflow-auto bg-transparent">
      <table className={cn("w-full caption-bottom text-sm", className)}>
        {children}
      </table>
    </div>
  );
}

export function TableHeader({ className, children }: { className?: string; children: ReactNode }) {
  return <thead className={cn("bg-slate-50/50 border-b border-slate-100", className)}>{children}</thead>;
}

export function TableRow({ className, children, onClick }: { className?: string; children: ReactNode; onClick?: () => void }) {
  return (
    <tr 
      onClick={onClick}
      className={cn("border-b border-slate-50 transition-colors hover:bg-slate-50/50", onClick && "cursor-pointer", className)}
    >
      {children}
    </tr>
  );
}

export function TableHead({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <th className={cn("h-12 px-6 text-left align-middle font-semibold text-slate-500 uppercase tracking-wider text-[11px]", className)}>
      {children}
    </th>
  );
}

export function TableCell({ className, children, colSpan }: { className?: string; children: ReactNode, colSpan?: number }) {
  return (
    <td colSpan={colSpan} className={cn("p-6 align-middle text-slate-700", className)}>
      {children}
    </td>
  );
}
