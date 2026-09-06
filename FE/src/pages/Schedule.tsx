import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import type { ScheduleResult } from '../types';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Badge } from '../components/ui/Badge';
import { Play, CheckCircle2, AlertTriangle, BarChart2, ShieldAlert, Zap, Dna, Flame, Sparkles } from 'lucide-react';

const ALGO_METADATA: Record<string, { label: string; icon: any; color: string; bg: string; border: string; text: string }> = {
  greedy: {
    label: 'Greedy (Tham lam)',
    icon: Zap,
    color: 'slate',
    bg: 'bg-slate-100',
    border: 'border-slate-300',
    text: 'text-slate-700',
  },
  ga: {
    label: '🧬 Genetic Algorithm (Di truyền)',
    icon: Dna,
    color: 'emerald',
    bg: 'bg-emerald-100',
    border: 'border-emerald-300',
    text: 'text-emerald-800',
  },
  sa: {
    label: '🔥 Simulated Annealing (Luyện kim)',
    icon: Flame,
    color: 'amber',
    bg: 'bg-amber-100',
    border: 'border-amber-300',
    text: 'text-amber-800',
  },
  hybrid: {
    label: '⚡ Thuật toán Lai (Hybrid)',
    icon: Zap,
    color: 'purple',
    bg: 'bg-purple-100',
    border: 'border-purple-300',
    text: 'text-purple-800',
  },
};

export default function Schedule() {
  const navigate = useNavigate();
  const [selectedAlgo, setSelectedAlgo] = useState('greedy');
  const [running, setRunning] = useState(false);
  const [elapsedMs, setElapsedMs] = useState(0);
  const [result, setResult] = useState<ScheduleResult | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [shiftIds, setShiftIds] = useState<number[]>([]);
  const [employeeIds, setEmployeeIds] = useState<string[]>([]);

  const timerRef = useRef<any>(null);

  const startTimer = () => {
    setElapsedMs(0);
    const startTime = Date.now();
    timerRef.current = setInterval(() => {
      setElapsedMs(Date.now() - startTime);
    }, 100);
  };

  const stopTimer = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
  };

  useEffect(() => {
    let active = true;

    Promise.all([api.getShifts(), api.getEmployees()])
      .then(([shifts, employees]) => {
        if (!active) return;
        setShiftIds(shifts.map((shift) => Number(shift.id)));
        setEmployeeIds(employees.map((employee) => String(employee.id)));
      })
      .catch((error: any) => {
        if (active) {
          setErrorMsg(error.response?.data?.errors?.[0] || error.message || 'Không tải được dữ liệu xếp lịch.');
        }
      });

    return () => {
      active = false;
      stopTimer();
    };
  }, []);

  const runSchedule = async () => {
    setRunning(true);
    setResult(null);
    setErrorMsg(null);
    startTimer();

    try {
      const res = await api.runSchedule(
        {
          shiftIds,
          employeeIds,
          fromDate: new Date().toISOString(),
          toDate: new Date().toISOString(),
          options: {},
        },
        selectedAlgo
      );
      setResult(res);
    } catch (error: any) {
      const backendMessage = error.response?.data?.errors?.[0];
      setErrorMsg(backendMessage || error.message || 'Đã xảy ra lỗi hệ thống khi chạy xếp lịch!');
    } finally {
      stopTimer();
      setRunning(false);
    }
  };

  const runAllAlgorithms = async () => {
    setRunning(true);
    setResult(null);
    setErrorMsg(null);
    startTimer();

    try {
      const runs = await api.runAllSchedule(1);
      if (runs && runs.length > 0) {
        const last = runs[runs.length - 1];
        setResult({
          schedule: { '1': [{ employeeId: '1', employeeName: 'Nhiều ca' }] },
          totalShifts: last.totalShifts,
          filledShifts: last.filledShifts,
          unfilledShifts: last.unfilledShifts,
          executionTimeMs: runs.reduce((acc, r) => acc + r.executionTimeMs, 0),
          totalPenaltyScore: last.totalPenaltyScore,
          hardViolationsCount: last.hardViolationsCount,
          softViolationsCount: last.softViolationsCount,
          penaltyBreakdown: last.penaltyBreakdownJson ? JSON.parse(last.penaltyBreakdownJson) : {},
          algorithmRunId: last.id,
        });
      }
    } catch (error: any) {
      const backendMessage = error.response?.data?.errors?.[0];
      setErrorMsg(backendMessage || error.message || 'Đã xảy ra lỗi hệ thống khi chạy đợt thực nghiệm hàng loạt!');
    } finally {
      stopTimer();
      setRunning(false);
    }
  };

  const activeMeta = ALGO_METADATA[selectedAlgo] || ALGO_METADATA.greedy;
  const IconComponent = activeMeta.icon;

  // Render text giải trình tổng hợp
  const renderSummaryText = (res: ScheduleResult) => {
    const filledPercent = res.totalShifts > 0 ? Math.round((res.filledShifts / res.totalShifts) * 100) : 0;
    let text = `Đã xếp đủ ${res.filledShifts}/${res.totalShifts} ca (${filledPercent}%). `;
    
    if (res.totalPenaltyScore !== undefined) {
      text += `Điểm phạt tổng: ${res.totalPenaltyScore.toFixed(1)}`;
      if (res.penaltyBreakdown && Object.keys(res.penaltyBreakdown).length > 0) {
        const breakdownParts = Object.entries(res.penaltyBreakdown).map(
          ([k, v]) => `${k} (+${v.toFixed(1)} điểm)`
        );
        text += `, gồm: ${breakdownParts.join(', ')}. `;
      } else {
        text += `. `;
      }
    }

    if (res.hardViolationsCount === 0 || res.hardViolationsCount === undefined) {
      text += `Không có vi phạm ràng buộc cứng.`;
    }

    return text;
  };

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight text-slate-900">Xếp lịch Tự động</h2>
          <p className="text-slate-500 text-sm mt-1">Lựa chọn thuật toán để tối ưu hóa phân công ca làm việc</p>
        </div>

        {/* Dynamic Badge của Thuật toán được chọn */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-400 font-medium uppercase tracking-wider">Thuật toán:</span>
          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full border text-xs font-semibold ${activeMeta.bg} ${activeMeta.border} ${activeMeta.text} transition-all duration-300 shadow-sm`}>
            <IconComponent className="w-4 h-4" />
            <span>{activeMeta.label}</span>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Panel chọn thuật toán */}
        <div className="lg:col-span-1 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Cấu hình chạy</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">Chọn thuật toán tối ưu</label>
                <select
                  className="w-full border-slate-200 rounded-xl shadow-sm focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 py-3 px-4 text-slate-800 bg-slate-50 outline-none transition-all focus:bg-white text-sm font-medium"
                  value={selectedAlgo}
                  onChange={(e) => setSelectedAlgo(e.target.value)}
                  disabled={running}
                >
                  <option value="greedy">Greedy (Tham lam)</option>
                  <option value="ga">Genetic Algorithm (Di truyền - GA)</option>
                  <option value="sa">Simulated Annealing (Luyện kim - SA)</option>
                  <option value="hybrid">Thuật toán Lai (Hybrid GA+SA)</option>
                </select>
              </div>

              {/* Status Indicator */}
              {running && (
                <div className="p-3.5 bg-indigo-50 border border-indigo-100 rounded-xl flex items-center justify-between text-indigo-700 text-xs font-medium animate-pulse">
                  <span className="flex items-center gap-2">
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-indigo-600"></span>
                    </span>
                    Đang tính toán phân công...
                  </span>
                  <span className="font-mono font-bold text-sm">{(elapsedMs / 1000).toFixed(1)}s</span>
                </div>
              )}

              <div className="space-y-3">
                <Button
                  onClick={runSchedule}
                  disabled={running}
                  variant="primary"
                  className="w-full h-12 text-sm font-semibold gap-2 shadow-lg shadow-indigo-500/20"
                >
                  {running ? (
                    <span className="flex items-center gap-2">
                      <svg className="animate-spin h-4 w-4 text-white" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                      </svg>
                      Đang tính toán ({(elapsedMs / 1000).toFixed(1)}s)...
                    </span>
                  ) : (
                    <>
                      <Play className="w-4 h-4 fill-white" /> Khởi chạy Xếp lịch ({selectedAlgo.toUpperCase()})
                    </>
                  )}
                </Button>

                <Button
                  onClick={runAllAlgorithms}
                  disabled={running}
                  variant="secondary"
                  className="w-full h-11 text-xs font-semibold gap-2 border-slate-300 hover:bg-slate-100 text-slate-700"
                >
                  <Sparkles className="w-4 h-4 text-purple-600" /> Chạy tất cả thuật toán (để so sánh)
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Panel Kết quả */}
        <div className="lg:col-span-2 space-y-6">
          <Card className="h-full">
            <CardHeader className="flex flex-row items-center justify-between border-b border-slate-100 pb-4">
              <CardTitle className="text-base font-bold text-slate-800">Kết quả Xếp lịch & Tóm tắt Tối ưu</CardTitle>
            </CardHeader>
            <CardContent className="pt-6">
              {errorMsg ? (
                <div className="p-4 rounded-xl bg-rose-50 border border-rose-100 text-rose-700 flex items-start gap-3">
                  <AlertTriangle className="w-5 h-5 text-rose-500 shrink-0 mt-0.5" />
                  <div>
                    <h4 className="font-semibold text-sm">Lỗi thực thi</h4>
                    <p className="text-xs mt-1 text-rose-600">{errorMsg}</p>
                  </div>
                </div>
              ) : !result ? (
                <div className="flex flex-col items-center justify-center py-20 text-center">
                  <div className="w-16 h-16 bg-slate-100 rounded-2xl flex items-center justify-center mb-3 text-slate-400">
                    <Play className="w-8 h-8 ml-1 stroke-[1.5]" />
                  </div>
                  <h4 className="text-base font-semibold text-slate-700">Chưa có kết quả thực thi</h4>
                  <p className="text-xs text-slate-400 max-w-sm mt-1">
                    Chọn thuật toán mong muốn bên trái và nhấn nút "Khởi chạy Xếp lịch" để xem chỉ số tối ưu.
                  </p>
                </div>
              ) : (
                <div className="space-y-6 animate-in fade-in duration-300">
                  {/* Cảnh báo vi phạm ràng buộc cứng nếu có */}
                  {result.hardViolationsCount && result.hardViolationsCount > 0 ? (
                    <div className="p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 flex items-start gap-3">
                      <ShieldAlert className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
                      <div>
                        <h4 className="font-bold text-sm text-rose-900">CẢNH BÁO VI PHẠM RÀNG BUỘC CỨNG</h4>
                        <p className="text-xs mt-1 text-rose-700">
                          Thuật toán đang vi phạm ràng buộc cứng — kết quả KHÔNG hợp lệ, cần kiểm tra lại Validator.
                        </p>
                      </div>
                    </div>
                  ) : (
                    <div className="p-5 rounded-2xl bg-emerald-50/80 border border-emerald-100/80 flex items-start gap-4">
                      <CheckCircle2 className="w-6 h-6 text-emerald-600 shrink-0 mt-0.5" />
                      <div className="flex-1">
                        <div className="flex items-center justify-between">
                          <h3 className="text-base font-bold text-emerald-900">Xếp lịch thành công</h3>
                          <Badge variant="success">Hoàn tất</Badge>
                        </div>

                        {/* Thống kê nhanh */}
                        <div className="grid grid-cols-3 gap-3 mt-4">
                          <div className="bg-white/80 p-3 rounded-xl border border-emerald-100/60">
                            <span className="text-[11px] font-medium text-slate-500 uppercase">Thời gian chạy</span>
                            <p className="text-base font-bold text-slate-900 mt-0.5">{result.executionTimeMs} ms</p>
                          </div>

                          <div className="bg-white/80 p-3 rounded-xl border border-emerald-100/60">
                            <span className="text-[11px] font-medium text-slate-500 uppercase">Tỷ lệ đáp ứng</span>
                            <p className="text-base font-bold text-emerald-700 mt-0.5">
                              {result.filledShifts}/{result.totalShifts} ca
                            </p>
                          </div>

                          <div className="bg-white/80 p-3 rounded-xl border border-emerald-100/60">
                            <span className="text-[11px] font-medium text-slate-500 uppercase">Điểm phạt tổng</span>
                            <p className="text-base font-bold text-indigo-600 mt-0.5">
                              {result.totalPenaltyScore !== undefined ? result.totalPenaltyScore.toFixed(1) : 0}
                            </p>
                          </div>
                        </div>

                        {/* Mục Giải trình Phân tích */}
                        <div className="mt-4 p-3.5 bg-white rounded-xl border border-emerald-200/50 text-xs text-slate-700 leading-relaxed">
                          <span className="font-bold text-slate-900">Phân tích kết quả: </span>
                          {renderSummaryText(result)}
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Nút Chuyển trang Dashboard */}
                  <div className="flex justify-end pt-2">
                    <Button
                      onClick={() => navigate('/')}
                      variant="outline"
                      className="gap-2 text-xs font-semibold"
                    >
                      <BarChart2 className="w-4 h-4 text-indigo-600" />
                      Xem trong Dashboard so sánh
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
