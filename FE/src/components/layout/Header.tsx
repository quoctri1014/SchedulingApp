export function Header({ title }: { title: string }) {
  return (
    <header className="mb-6">
      <h1 className="text-2xl font-bold text-slate-800 tracking-tight">{title}</h1>
    </header>
  );
}
