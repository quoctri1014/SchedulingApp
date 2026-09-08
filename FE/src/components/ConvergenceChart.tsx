import React from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  CartesianGrid,
} from 'recharts';
import type { ConvergencePoint } from '../types';
import { Card, CardContent, CardHeader, CardTitle } from './ui/Card';
import { TrendingDown, Award, Zap, Activity } from 'lucide-react';

interface ConvergenceChartProps {
  data?: ConvergencePoint[];
  algorithmName?: string;
  title?: string;
}

export const ConvergenceChart: React.FC<ConvergenceChartProps> = ({
  data = [],
  algorithmName = 'Thuật toán',
  title = 'Đường cong Hội tụ Quá trình Tối ưu (Convergence Curve)',
}) => {
  if (!data || data.length === 0) {
    return null;
  }

  const initialPenalty = data[0]?.bestPenalty ?? 0;
  const finalPenalty = data[data.length - 1]?.bestPenalty ?? 0;
  const improvementPct =
    initialPenalty > 0
      ? (((initialPenalty - finalPenalty) / initialPenalty) * 100).toFixed(1)
      : '0.0';

  // Format label thế hệ / iteration
  const formatXAxis = (tickItem: number) => {
    return `Gen ${tickItem}`;
  };

  // Custom tooltip
  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="bg-slate-900/95 backdrop-blur-sm border border-slate-800 text-white p-3 rounded-xl shadow-xl text-xs space-y-1.5">
          <p className="font-semibold text-slate-300 border-b border-slate-800 pb-1 flex items-center gap-1.5">
            <Activity className="w-3.5 h-3.5 text-indigo-400" />
            <span>Thế hệ / Vòng lặp: {label}</span>
          </p>
          {payload.map((entry: any, index: number) => (
            <div key={`item-${index}`} className="flex items-center justify-between gap-4">
              <span className="flex items-center gap-1.5" style={{ color: entry.color }}>
                <span
                  className="w-2 h-2 rounded-full inline-block"
                  style={{ backgroundColor: entry.color }}
                />
                {entry.name}:
              </span>
              <span className="font-mono font-bold text-white">
                {Number(entry.value).toFixed(2)}
              </span>
            </div>
          ))}
        </div>
      );
    }
    return null;
  };

  return (
    <Card className="border border-slate-200/80 shadow-sm overflow-hidden bg-white">
      <CardHeader className="border-b border-slate-100 bg-slate-50/50 pb-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div>
            <CardTitle className="text-sm font-bold text-slate-800 flex items-center gap-2">
              <TrendingDown className="w-4 h-4 text-indigo-600" />
              <span>{title}</span>
            </CardTitle>
            <p className="text-xs text-slate-500 mt-0.5">
              Theo dõi sự suy giảm hàm phạt qua các thế hệ của {algorithmName.toUpperCase()}
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-2.5 py-1 bg-indigo-50 border border-indigo-100 rounded-lg text-indigo-700 font-semibold flex items-center gap-1">
              <Zap className="w-3.5 h-3.5 text-indigo-600" />
              Giảm {improvementPct}%
            </span>
            <span className="px-2.5 py-1 bg-emerald-50 border border-emerald-100 rounded-lg text-emerald-700 font-semibold flex items-center gap-1">
              <Award className="w-3.5 h-3.5 text-emerald-600" />
              Tốt nhất: {finalPenalty.toFixed(1)}
            </span>
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-4 pb-3">
        {/* Quick summary metrics */}
        <div className="grid grid-cols-3 gap-3 mb-4">
          <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100">
            <span className="text-[10px] uppercase font-bold text-slate-400">Điểm phạt ban đầu</span>
            <p className="text-sm font-bold text-slate-700 mt-0.5">{initialPenalty.toFixed(2)}</p>
          </div>
          <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100">
            <span className="text-[10px] uppercase font-bold text-slate-400">Điểm phạt tối ưu</span>
            <p className="text-sm font-bold text-emerald-600 mt-0.5">{finalPenalty.toFixed(2)}</p>
          </div>
          <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100">
            <span className="text-[10px] uppercase font-bold text-slate-400">Số mốc theo dõi</span>
            <p className="text-sm font-bold text-indigo-600 mt-0.5">{data.length} thế hệ</p>
          </div>
        </div>

        {/* Chart */}
        <div className="w-full h-64 sm:h-72">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={data}
              margin={{ top: 10, right: 20, left: -10, bottom: 5 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
              <XAxis
                dataKey="iteration"
                tickFormatter={formatXAxis}
                tick={{ fontSize: 11, fill: '#64748b' }}
                stroke="#cbd5e1"
              />
              <YAxis
                tick={{ fontSize: 11, fill: '#64748b' }}
                stroke="#cbd5e1"
                domain={['dataMin - 2', 'auto']}
              />
              <Tooltip content={<CustomTooltip />} />
              <Legend
                verticalAlign="top"
                align="right"
                wrapperStyle={{ paddingBottom: '10px', fontSize: '12px' }}
              />
              <Line
                type="monotone"
                dataKey="bestPenalty"
                name="Tốt nhất (Best)"
                stroke="#4f46e5"
                strokeWidth={2.5}
                dot={{ r: 2, fill: '#4f46e5' }}
                activeDot={{ r: 5, fill: '#4f46e5', stroke: '#ffffff', strokeWidth: 2 }}
              />
              {data.some((d) => d.avgPenalty !== undefined && d.avgPenalty > 0) && (
                <Line
                  type="monotone"
                  dataKey="avgPenalty"
                  name="Trung bình (Avg)"
                  stroke="#94a3b8"
                  strokeWidth={1.8}
                  strokeDasharray="4 4"
                  dot={false}
                />
              )}
            </LineChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
};

export default ConvergenceChart;
