import { Zap, Dna, Flame, Sparkles } from 'lucide-react';

const ALGO_CONFIG: Record<string, { label: string; icon: any; bg: string; text: string }> = {
  greedy: { label: 'Greedy', icon: Zap, bg: 'bg-slate-100', text: 'text-slate-700' },
  ga: { label: 'GA (Di truyền)', icon: Dna, bg: 'bg-emerald-100', text: 'text-emerald-700' },
  sa: { label: 'SA (Luyện kim)', icon: Flame, bg: 'bg-amber-100', text: 'text-amber-700' },
  hybrid: { label: 'Hybrid (Lai)', icon: Sparkles, bg: 'bg-purple-100', text: 'text-purple-700' },
};

export function AlgorithmBadge({ algorithm }: { algorithm: string }) {
  const cfg = ALGO_CONFIG[algorithm.toLowerCase()] || ALGO_CONFIG.greedy;
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-semibold ${cfg.bg} ${cfg.text}`}>
      <Icon className="w-3.5 h-3.5" />
      {cfg.label}
    </span>
  );
}
