export function EmptyState({ message }: { message: string }) {
  return (
    <div className="p-8 text-center text-slate-500 bg-slate-50 rounded-xl border border-dashed border-slate-200">
      <p>{message}</p>
    </div>
  );
}
