export function StatusBadge({ status }: { status: string }) {
  const isHealthy = status === 'Healthy' || status === 'full' || status === 'Active';
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${isHealthy ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'}`}>
      {status}
    </span>
  );
}
