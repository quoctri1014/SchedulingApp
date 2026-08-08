export function ElapsedTimer({ elapsedMs }: { elapsedMs: number }) {
  return (
    <span className="font-mono text-sm font-bold text-indigo-600">
      {(elapsedMs / 1000).toFixed(1)}s
    </span>
  );
}
