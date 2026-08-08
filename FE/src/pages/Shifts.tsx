import { useState, useEffect } from 'react';
import { api } from '../services/api';
import type { Shift, Company, Department, Position, MasterShift } from '../types';
import { Card, CardContent } from '../components/ui/Card';
import { Table, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/Table';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Plus, X } from 'lucide-react';
import ShiftForm from '../components/ShiftForm';

export default function Shifts() {
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [masterShifts, setMasterShifts] = useState<MasterShift[]>([]);
  
  const [editingShift, setEditingShift] = useState<Shift | undefined>(undefined);

  useEffect(() => {
    loadShifts();
    api.getCompanies().then(setCompanies);
    api.getDepartments().then(setDepartments);
    api.getPositions().then(setPositions);
    api.getMasterShifts().then(setMasterShifts);
  }, []);

  const loadShifts = () => {
    api.getShifts().then(setShifts);
  };

  const handleSave = async (shift: Omit<Shift, 'id'> | Shift) => {
    try {
      if ('id' in shift && shift.id) {
        await api.updateShift(shift.id, shift);
      } else {
        await api.createShift(shift);
      }
      setIsModalOpen(false);
      setEditingShift(undefined);
      loadShifts();
    } catch (err) {
      console.error(err);
      alert('Đã xảy ra lỗi khi lưu ca làm việc.');
    }
  };

  const handleAddClick = () => {
    setEditingShift(undefined);
    setIsModalOpen(true);
  };

  const handleEditClick = (shift: Shift) => {
    setEditingShift(shift);
    setIsModalOpen(true);
  };

  return (
    <div className="space-y-8 animate-in fade-in duration-700 slide-in-from-bottom-4 relative">
      <div className="flex justify-between items-end">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-900">Yêu cầu Ca làm việc</h2>
          <p className="text-slate-500 mt-1">Danh sách các yêu cầu xếp lịch dài hạn</p>
        </div>
        <Button className="gap-2" onClick={handleAddClick}>
          <Plus className="w-4 h-4" />
          Thêm yêu cầu mới
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã Yêu Cầu</TableHead>
                <TableHead>Từ ngày</TableHead>
                <TableHead>Đến ngày</TableHead>
                <TableHead>Ca Chuẩn</TableHead>
                <TableHead>Địa điểm (Công ty)</TableHead>
                <TableHead>Yêu cầu nhân sự (SL / Vị trí)</TableHead>
                <TableHead>Hành động</TableHead>
              </TableRow>
            </TableHeader>
            <tbody>
              {shifts.map(s => {
                const ms = masterShifts.find(m => m.id === s.masterShiftId);
                return (
                  <TableRow key={s.id}>
                    <TableCell className="font-mono text-slate-400">REQ_{String(s.id).padStart(4, '0')}</TableCell>
                    <TableCell className="font-medium text-slate-800">{new Date(s.startDate).toLocaleDateString('vi-VN')}</TableCell>
                    <TableCell className="font-medium text-slate-800">{new Date(s.endDate).toLocaleDateString('vi-VN')}</TableCell>
                    <TableCell className="font-medium text-blue-600">{ms?.name || s.masterShiftId}</TableCell>
                    <TableCell className="text-slate-500">{s.companyName || s.companyId}</TableCell>
                    <TableCell>
                      <Badge className="bg-amber-100 text-amber-700">{s.requiredEmployeeCount} nhân viên (Vị trí {s.positionName || s.positionId})</Badge>
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="sm" className="text-indigo-600 hover:text-indigo-700" onClick={() => handleEditClick(s)}>Chỉnh sửa</Button>
                    </TableCell>
                  </TableRow>
                );
              })}
              {shifts.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-12 text-slate-500">
                    <p className="text-lg">Chưa có yêu cầu ca làm việc nào.</p>
                    <p className="text-sm mt-1">Hãy nhấn "Thêm yêu cầu mới" để bắt đầu thiết lập lịch trình.</p>
                  </TableCell>
                </TableRow>
              )}
            </tbody>
          </Table>
        </CardContent>
      </Card>

      {isModalOpen && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center p-6 border-b border-slate-100">
              <h3 className="text-xl font-bold text-slate-900">
                {editingShift ? 'Chỉnh sửa Yêu Cầu Ca Làm Việc' : 'Thêm Yêu Cầu Ca Làm Việc Mới'}
              </h3>
              <button onClick={() => setIsModalOpen(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6">
              <ShiftForm 
                initialData={editingShift}
                companies={companies}
                departments={departments}
                positions={positions}
                masterShifts={masterShifts}
                onSave={handleSave}
                onCancel={() => setIsModalOpen(false)}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
