import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import type { AlgorithmRun, AlgorithmSummary } from '../types';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
// import { Badge } from '../components/ui/Badge';
import { 
  BarChart2, Download, Copy, Play, RefreshCw, CheckCircle, Clock, Award, TrendingUp 
} from 'lucide-react';
import { 
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell, CartesianGrid,
  LineChart, Line, Legend
} from 'recharts';

const KNOWN_ALGORITHMS = [
  { key: 'greedy', name: 'Greedy (Tham lam)', color: '#64748b' },
  { key: 'ga', name: 'Genetic Algorithm (GA)', color: '#10b981' },
  { key: 'sa', name: 'Simulated Annealing (SA)', color: '#f59e0b' },
  { key: 'hybrid', name: 'Hybrid (Greedy + GA + Local Search)', color: '#8b5cf6' },
];

export default function Dashboard() {
  const navigate = useNavigate();
  const [latestRuns, setLatestRuns] = useState<AlgorithmRun[]>([]);
  const [summaries, setSummaries] = useState<AlgorithmSummary[]>([]);
  const [allRuns, setAllRuns] = useState<AlgorithmRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [copySuccess, setCopySuccess] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [latestRes, summaryRes, allRunsRes] = await Promise.all([
        api.getLatestRunsByAlgorithm(),
        api.getAlgorithmSummary(),
        api.getAlgorithmRuns()
      ]);
      setLatestRuns(latestRes || []);
      setSummaries(summaryRes || []);
      setAllRuns(allRunsRes || []);
    } catch (err) {
      console.error('Error fetching algorithm metrics:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  // Filter only algorithms that have been run
  const activeAlgorithms = KNOWN_ALGORITHMS.filter(algo => 
    latestRuns.some(r => r.algorithm.toLowerCase() === algo.key)
  );

  const chartData = activeAlgorithms.map(algo => {
    const run = latestRuns.find(r => r.algorithm.toLowerCase() === algo.key);
    let breakdown: Record<string, number> = {};
    if (run?.penaltyBreakdownJson) {
      try {
        breakdown = JSON.parse(run.penaltyBreakdownJson);
      } catch (e) {}
    }

    return {
      key: algo.key,
      name: algo.name,
      color: algo.color,
      isRun: !!run,
      executionTimeMs: run ? run.executionTimeMs : 0,
      totalPenaltyScore: run ? run.totalPenaltyScore : 0,
      filledPercentage: run && run.totalShifts > 0 ? Math.round((run.filledShifts / run.totalShifts) * 100) : 0,
      breakdown,
      lastRunAt: run ? new Date(run.startedAt).toLocaleString('vi-VN') : 'Chưa chạy'
    };
  });

  // Prepare data for Optimization History (Line Chart)
  // Group runs by chronological order to form "iterations"
  const prepareHistoryData = () => {
    // Sort all runs chronologically
    const sortedRuns = [...allRuns].sort((a, b) => new Date(a.startedAt).getTime() - new Date(b.startedAt).getTime());
    
    // Determine max iterations among all algorithms to align X-axis
    const runsByAlgo: Record<string, AlgorithmRun[]> = {};
    activeAlgorithms.forEach(a => runsByAlgo[a.key] = []);
    
    sortedRuns.forEach(run => {
      const key = run.algorithm.toLowerCase();
      if (runsByAlgo[key]) {
        runsByAlgo[key].push(run);
      }
    });

    const maxIterations = Math.max(...Object.values(runsByAlgo).map(arr => arr.length));
    
    const historyData = [];
    for (let i = 0; i < maxIterations; i++) {
      const dataPoint: any = { iteration: `Lần ${i + 1}` };
      activeAlgorithms.forEach(algo => {
        const run = runsByAlgo[algo.key][i];
        if (run) {
          dataPoint[`${algo.key}_penalty`] = run.totalPenaltyScore;
          dataPoint[`${algo.key}_time`] = run.executionTimeMs;
        }
      });
      historyData.push(dataPoint);
    }
    return historyData;
  };

  const historyChartData = prepareHistoryData();

  const handleExportCSV = () => {
    window.open(api.exportAlgorithmRunsCSV(), '_blank');
  };

  const handleCopySummary = (s: AlgorithmSummary) => {
    const text = `Thuật toán ${s.algorithm.toUpperCase()}: chạy trung bình ${s.avgExecutionTimeMs}ms, điểm phạt trung bình ${s.avgPenaltyScore}, tỷ lệ ca đủ người trung bình ${s.avgFilledPercentage}% (qua ${s.runCount} lần chạy).`;
    navigator.clipboard.writeText(text);
    setCopySuccess(s.algorithm);
    setTimeout(() => setCopySuccess(null), 3000);
  };

  const hasAnyData = activeAlgorithms.length > 0;

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      {/* Top Header & Actions with Logo */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-6 rounded-2xl shadow-sm border border-slate-100">
        <div className="flex items-center gap-4">
          <img src="/assets/logo.png" alt="Logo" className="w-16 h-16 object-contain rounded-xl shadow-sm" />
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-slate-900">Bảng điều khiển & So sánh Thực nghiệm</h2>
            <p className="text-slate-500 text-sm mt-1">
              Tổng hợp dữ liệu thực tế từ tất cả các lần chạy xếp lịch trong CSDL
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <Button onClick={fetchData} variant="outline" size="sm" className="gap-1.5">
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>

          <Button onClick={handleExportCSV} variant="outline" size="sm" className="gap-1.5 text-indigo-600 border-indigo-200 bg-indigo-50/50 hover:bg-indigo-100">
            <Download className="w-3.5 h-3.5" />
            Xuất số liệu CSV
          </Button>

          <Button onClick={() => navigate('/schedule')} variant="primary" size="sm" className="gap-1.5">
            <Play className="w-3.5 h-3.5 fill-white" />
            Chạy Xếp Lịch
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Card className="h-72 animate-pulse bg-slate-100/50" />
          <Card className="h-72 animate-pulse bg-slate-100/50" />
        </div>
      ) : !hasAnyData ? (
        /* Empty State */
        <Card className="p-12 text-center flex flex-col items-center justify-center border-dashed">
          <div className="w-16 h-16 rounded-full bg-indigo-50 text-indigo-500 flex items-center justify-center mb-4">
            <BarChart2 className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-slate-800">Chưa có dữ liệu thực nghiệm</h3>
          <p className="text-sm text-slate-500 max-w-md mt-1 mb-6">
            Hệ thống chưa ghi nhận lần chạy thuật toán nào. Bạn hãy sang trang Lịch phân công để chạy thử và thu thập chỉ số thực tế.
          </p>
          <Button onClick={() => navigate('/schedule')} variant="primary" className="gap-2">
            <Play className="w-4 h-4 fill-white" />
            Chạy thử Thuật toán ngay
          </Button>
        </Card>
      ) : (
        <>
          {/* Section 1: Biểu đồ theo dõi quá trình Tối ưu hóa (Lịch sử) */}
          <Card className="overflow-hidden">
            <CardHeader className="flex flex-row items-center justify-between pb-2 bg-slate-50/50 border-b border-slate-100">
              <div>
                <CardTitle className="text-base font-bold text-slate-800">Tiến trình Tối ưu hóa Thuật toán</CardTitle>
                <p className="text-xs text-slate-500 mt-0.5">Biểu đồ thể hiện sự cải thiện Điểm phạt (Total Penalty) qua các lượt chạy sửa đổi</p>
              </div>
              <TrendingUp className="w-5 h-5 text-indigo-500" />
            </CardHeader>
            <CardContent className="pt-6">
              <div className="h-72 w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={historyChartData} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                    <XAxis dataKey="iteration" tick={{ fontSize: 12, fill: '#64748b' }} />
                    <YAxis tick={{ fontSize: 12, fill: '#64748b' }} label={{ value: 'Điểm phạt', angle: -90, position: 'insideLeft', style: { textAnchor: 'middle', fill: '#94a3b8', fontSize: 12 } }} />
                    <Tooltip
                      contentStyle={{ backgroundColor: '#1e293b', borderRadius: '12px', color: '#fff', fontSize: '13px', border: 'none' }}
                      itemStyle={{ color: '#e2e8f0' }}
                    />
                    <Legend wrapperStyle={{ paddingTop: '20px', fontSize: '13px' }} />
                    {activeAlgorithms.map(algo => (
                      <Line 
                        key={algo.key}
                        type="monotone" 
                        dataKey={`${algo.key}_penalty`} 
                        name={`${algo.name} (Penalty)`} 
                        stroke={algo.color} 
                        strokeWidth={3}
                        dot={{ r: 4, strokeWidth: 2 }}
                        activeDot={{ r: 6 }}
                        connectNulls
                      />
                    ))}
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>

          {/* Section 2: Biểu đồ so sánh thời gian & điểm phạt (Lần chạy mới nhất) */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Biểu đồ 1: Thời gian thực thi */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <div>
                  <CardTitle className="text-base font-bold text-slate-800">Thời gian thực thi mới nhất (ms)</CardTitle>
                  <p className="text-xs text-slate-400 mt-0.5">Lần chạy gần nhất của từng thuật toán</p>
                </div>
                <Clock className="w-4 h-4 text-slate-400" />
              </CardHeader>
              <CardContent className="pt-4">
                <div className="h-64 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={chartData} margin={{ top: 20, right: 20, left: -10, bottom: 20 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                      <XAxis dataKey="name" tick={{ fontSize: 11, fill: '#64748b' }} interval={0} />
                      <YAxis tick={{ fontSize: 11, fill: '#64748b' }} />
                      <Tooltip
                        formatter={(val: any, _name: any, item: any) => [
                          item.payload.isRun ? `${val} ms` : 'Chưa chạy',
                          'Thời gian'
                        ]}
                        contentStyle={{ backgroundColor: '#1e293b', borderRadius: '12px', color: '#fff', fontSize: '12px' }}
                      />
                      <Bar dataKey="executionTimeMs" radius={[6, 6, 0, 0]}>
                        {chartData.map((entry, index) => (
                          <Cell 
                            key={`cell-${index}`} 
                            fill={entry.isRun ? entry.color : '#e2e8f0'} 
                          />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* Biểu đồ 2: Điểm phạt tổng (Total Penalty Score) */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <div>
                  <CardTitle className="text-base font-bold text-slate-800">Điểm phạt tổng mới nhất</CardTitle>
                  <p className="text-xs font-semibold text-emerald-600 mt-0.5">↓ Càng thấp càng tối ưu</p>
                </div>
                <Award className="w-4 h-4 text-slate-400" />
              </CardHeader>
              <CardContent className="pt-4">
                <div className="h-64 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={chartData} margin={{ top: 20, right: 20, left: -10, bottom: 20 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                      <XAxis dataKey="name" tick={{ fontSize: 11, fill: '#64748b' }} interval={0} />
                      <YAxis tick={{ fontSize: 11, fill: '#64748b' }} />
                      <Tooltip
                        content={({ active, payload }) => {
                          if (active && payload && payload.length) {
                            const data = payload[0].payload;
                            return (
                              <div className="bg-slate-900 text-white p-3 rounded-xl shadow-xl text-xs space-y-1">
                                <p className="font-bold text-indigo-300">{data.name}</p>
                                {data.isRun ? (
                                  <>
                                    <p className="text-slate-200">Điểm phạt tổng: <span className="font-bold text-amber-400">{data.totalPenaltyScore.toFixed(1)}</span></p>
                                    {Object.keys(data.breakdown).length > 0 && (
                                      <div className="pt-1 border-t border-slate-700 mt-1">
                                        <p className="text-[10px] text-slate-400 uppercase font-semibold">Phân tích vi phạm mềm:</p>
                                        {Object.entries(data.breakdown).map(([k, v]) => (
                                          <p key={k} className="text-[11px] text-slate-300">• {k}: +{Number(v).toFixed(1)}</p>
                                        ))}
                                      </div>
                                    )}
                                  </>
                                ) : (
                                  <p className="text-slate-400 italic">Chưa từng được chạy</p>
                                )}
                              </div>
                            );
                          }
                          return null;
                        }}
                      />
                      <Bar dataKey="totalPenaltyScore" radius={[6, 6, 0, 0]}>
                        {chartData.map((entry, index) => (
                          <Cell 
                            key={`cell-pen-${index}`} 
                            fill={entry.isRun ? entry.color : '#e2e8f0'} 
                          />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Section 3: Bảng Thống kê Tổng hợp */}
          <Card className="overflow-hidden p-0">
            <CardHeader className="p-6 pb-4 border-b border-slate-100 flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-base font-bold text-slate-900">Bảng Tổng hợp Chỉ số Thực nghiệm</CardTitle>
                <p className="text-xs text-slate-500 mt-0.5">Chỉ hiển thị các thuật toán đã có dữ liệu thực nghiệm trong hệ thống</p>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-sm">
                  <thead>
                    <tr className="bg-slate-50/80 border-b border-slate-100 text-slate-500 font-medium text-xs uppercase tracking-wider">
                      <th className="py-3 px-6">Thuật toán</th>
                      <th className="py-3 px-4">Số lần chạy</th>
                      <th className="py-3 px-4">Lần chạy gần nhất</th>
                      <th className="py-3 px-4">Thời gian TB</th>
                      <th className="py-3 px-4">Điểm phạt TB</th>
                      <th className="py-3 px-4">% Đủ người TB</th>
                      <th className="py-3 px-6 text-right">Trích dẫn Báo cáo</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {activeAlgorithms.map(algo => {
                      const sum = summaries.find(s => s.algorithm.toLowerCase() === algo.key);
                      if (!sum) return null; // Should not happen since we filtered activeAlgorithms

                      return (
                        <tr key={algo.key} className="hover:bg-slate-50/50 transition-colors">
                          <td className="py-4 px-6 font-semibold text-slate-900">
                            <div className="flex items-center gap-2.5">
                              <span className="w-3 h-3 rounded-full" style={{ backgroundColor: algo.color }} />
                              {algo.name}
                            </div>
                          </td>
                          <td className="py-4 px-4 font-mono font-medium text-slate-700">
                            {sum.runCount} lần
                          </td>
                          <td className="py-4 px-4 text-xs text-slate-500">
                            {sum.lastRunAt ? new Date(sum.lastRunAt).toLocaleString('vi-VN') : '—'}
                          </td>
                          <td className="py-4 px-4 font-mono font-medium text-slate-800">
                            {sum.avgExecutionTimeMs} ms
                          </td>
                          <td className="py-4 px-4 font-mono font-bold text-amber-600">
                            {sum.avgPenaltyScore}
                          </td>
                          <td className="py-4 px-4 font-mono font-semibold text-emerald-600">
                            {sum.avgFilledPercentage}%
                          </td>
                          <td className="py-4 px-6 text-right">
                            <Button
                              onClick={() => handleCopySummary(sum)}
                              variant="outline"
                              size="sm"
                              className="gap-1.5 text-xs border-slate-200 hover:bg-indigo-50 hover:text-indigo-600 hover:border-indigo-200"
                            >
                              {copySuccess === sum.algorithm ? (
                                <>
                                  <CheckCircle className="w-3.5 h-3.5 text-emerald-600" />
                                  <span className="text-emerald-600 font-bold">Đã Copy!</span>
                                </>
                              ) : (
                                <>
                                  <Copy className="w-3.5 h-3.5 text-slate-400" />
                                  Copy tóm tắt
                                </>
                              )}
                            </Button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
