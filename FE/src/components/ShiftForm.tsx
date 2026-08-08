import { useState } from 'react';
import type { Shift, Company, Department, Position, MasterShift } from '../types';
import { Button } from './ui/Button';

interface ShiftFormProps {
  initialData?: Shift;
  companies: Company[];
  departments: Department[];
  positions: Position[];
  masterShifts: MasterShift[];
  onSave: (shift: Omit<Shift, 'id'> | Shift) => void;
  onCancel: () => void;
}

export default function ShiftForm({ initialData, companies, departments, positions, masterShifts, onSave, onCancel }: ShiftFormProps) {
  const [formData, setFormData] = useState({
    masterShiftId: initialData?.masterShiftId || '',
    companyId: initialData?.companyId || '',
    departmentId: initialData?.departmentId || '',
    positionId: initialData?.positionId || '',
    startDate: initialData?.startDate ? initialData.startDate.split('T')[0] : new Date().toISOString().slice(0, 10),
    endDate: initialData?.endDate ? initialData.endDate.split('T')[0] : new Date().toISOString().slice(0, 10),
    requiredEmployeeCount: initialData?.requiredEmployeeCount || 1,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (initialData?.id) {
      onSave({ ...formData, id: initialData.id } as Shift);
    } else {
      onSave(formData as Omit<Shift, 'id'>);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Công ty</label>
          <select 
            required
            value={formData.companyId}
            onChange={(e) => setFormData(prev => ({ 
              ...prev, 
              companyId: e.target.value,
              departmentId: '' 
            }))}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            <option value="">-- Chọn công ty --</option>
            {companies.map(c => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Phòng ban</label>
          <select 
            value={formData.departmentId}
            onChange={(e) => setFormData(prev => ({ ...prev, departmentId: e.target.value }))}
            disabled={!formData.companyId}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 disabled:bg-slate-100 disabled:text-slate-400"
          >
            <option value="">{formData.companyId ? "-- Chọn phòng ban (Tùy chọn) --" : "-- Vui lòng chọn công ty trước --"}</option>
            {formData.companyId && departments.filter(d => d.companyId === formData.companyId).map(d => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-semibold text-slate-700">Ca Chuẩn</label>
        <select 
          required
          value={formData.masterShiftId}
          onChange={(e) => setFormData(prev => ({ ...prev, masterShiftId: e.target.value }))}
          className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
        >
          <option value="">-- Chọn ca chuẩn --</option>
          {masterShifts.map(ms => (
            <option key={ms.id} value={ms.id}>{ms.name} ({ms.shiftStart} - {ms.shiftEnd})</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Từ ngày</label>
          <input 
            type="date"
            required
            value={formData.startDate}
            onChange={(e) => setFormData(prev => ({ ...prev, startDate: e.target.value }))}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Đến ngày</label>
          <input 
            type="date"
            required
            value={formData.endDate}
            min={formData.startDate}
            onChange={(e) => setFormData(prev => ({ ...prev, endDate: e.target.value }))}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Vị trí yêu cầu</label>
          <select 
            required
            value={formData.positionId}
            onChange={(e) => setFormData(prev => ({ ...prev, positionId: e.target.value }))}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            <option value="">-- Chọn vị trí --</option>
            {positions.map(p => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-semibold text-slate-700">Số lượng nhân sự cần thiết</label>
          <input 
            type="number"
            required
            min={1}
            value={formData.requiredEmployeeCount}
            onChange={(e) => setFormData(prev => ({ ...prev, requiredEmployeeCount: parseInt(e.target.value) || 1 }))}
            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          />
        </div>
      </div>

      <div className="flex justify-end gap-3 pt-4 border-t border-slate-200 mt-6">
        <Button type="button" variant="outline" onClick={onCancel}>Hủy</Button>
        <Button type="submit">Lưu yêu cầu</Button>
      </div>
    </form>
  );
}
