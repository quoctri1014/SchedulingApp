import { useState } from 'react';
import { ArrowLeft, Save, User as UserIcon, Mail, Phone, Building2, MapPin, BadgeCheck, XCircle } from 'lucide-react';
import type { User } from '../types';
import { Card, CardContent } from '../components/ui/Card';

interface UserDetailProps {
  user: User;
  onBack: () => void;
}

export default function UserDetail({ user, onBack }: UserDetailProps) {
  const [formData, setFormData] = useState({
    code: user.code || user.id,
    fullName: user.fullName || user.name || '',
    email: user.email || '',
    phone: user.phone || '',
    isActive: user.isActive ?? true,
  });

  const [saving, setSaving] = useState(false);

  const primaryComp = user.companyAssignments?.find((ca: any) => ca.isPrimaryCompany) || user.companyAssignments?.[0];
  const primaryDept = user.departmentAssignments?.find((da: any) => da.isPrimaryDepartment) || user.departmentAssignments?.[0];

  const handleSave = () => {
    setSaving(true);
    // TODO: Call API to save
    setTimeout(() => {
      setSaving(false);
      alert('Đã lưu thông tin (Bản Demo)');
    }, 800);
  };

  return (
    <div className="space-y-6 animate-fade-in-up">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <button 
            onClick={onBack}
            className="p-2 hover:bg-slate-200 rounded-full transition-colors text-slate-500 hover:text-slate-800 shrink-0"
            title="Quay lại danh sách"
          >
            <ArrowLeft className="w-6 h-6" />
          </button>
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-slate-900">Hồ sơ Nhân sự</h2>
            <p className="text-slate-500 mt-1 text-sm">Xem và cập nhật thông tin cá nhân</p>
          </div>
        </div>
        <button 
          onClick={handleSave}
          disabled={saving}
          className="flex items-center justify-center gap-2 px-6 py-2.5 bg-blue-600 text-white font-semibold rounded-lg shadow-sm hover:bg-blue-700 transition-colors disabled:opacity-50"
        >
          <Save className="w-4 h-4" />
          {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Left Column: Avatar & Summary */}
        <div className="md:col-span-1 space-y-6">
          <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden text-center">
            <CardContent className="p-6">
              <div className="w-24 h-24 mx-auto rounded-full bg-gradient-to-br from-blue-100 to-indigo-100 text-blue-700 flex items-center justify-center font-black text-3xl shadow-sm mb-4">
                {formData.fullName.slice(0, 2).toUpperCase() || 'NV'}
              </div>
              <h3 className="text-xl font-bold text-slate-800">{formData.fullName}</h3>
              <p className="text-sm font-mono text-blue-600 font-semibold bg-blue-50 w-fit mx-auto px-3 py-1 rounded-full mt-2">
                {formData.code}
              </p>
              
              <div className="mt-6 space-y-3">
                <div className="flex items-center justify-center gap-2 text-sm text-slate-600">
                  <Mail className="w-4 h-4 text-slate-400" />
                  {formData.email || 'Chưa cập nhật email'}
                </div>
                <div className="flex items-center justify-center gap-2 text-sm text-slate-600">
                  <Phone className="w-4 h-4 text-slate-400" />
                  {formData.phone || 'Chưa cập nhật SĐT'}
                </div>
              </div>

              <div className="mt-6 pt-6 border-t border-slate-100 flex justify-center">
                {formData.isActive ? (
                  <span className="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-700">
                    <BadgeCheck className="w-4 h-4" /> Đang hoạt động
                  </span>
                ) : (
                  <span className="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-600">
                    <XCircle className="w-4 h-4" /> Đã khóa
                  </span>
                )}
              </div>
            </CardContent>
          </Card>

          <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden">
             <div className="bg-slate-50 p-4 border-b border-slate-200">
              <h3 className="text-sm font-semibold text-slate-800">Đơn vị trực thuộc</h3>
            </div>
            <CardContent className="p-4 space-y-4">
              <div>
                <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider block mb-1">Công ty</label>
                <div className="flex items-center gap-2 text-slate-800 font-medium text-sm">
                  <Building2 className="w-4 h-4 text-blue-500 shrink-0" />
                  {primaryComp?.companyName || 'Chưa phân công'}
                </div>
              </div>
              <div>
                <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider block mb-1">Phòng ban</label>
                <div className="flex items-center gap-2 text-slate-800 font-medium text-sm">
                  <MapPin className="w-4 h-4 text-indigo-500 shrink-0" />
                  {primaryDept?.departmentName || 'Chưa phân công'}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Edit Form */}
        <div className="md:col-span-2">
          <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden h-full">
            <div className="bg-slate-50 p-4 border-b border-slate-200">
              <h3 className="text-base font-semibold text-slate-800 flex items-center gap-2">
                <UserIcon className="w-5 h-5 text-blue-600" />
                Thông tin cá nhân
              </h3>
            </div>
            <CardContent className="p-6">
              <form className="space-y-6">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <label className="text-sm font-semibold text-slate-700">Mã nhân viên</label>
                    <input 
                      type="text" 
                      value={formData.code}
                      onChange={e => setFormData({...formData, code: e.target.value})}
                      className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all font-mono"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-semibold text-slate-700">Họ và Tên</label>
                    <input 
                      type="text" 
                      value={formData.fullName}
                      onChange={e => setFormData({...formData, fullName: e.target.value})}
                      className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-semibold text-slate-700">Email</label>
                    <input 
                      type="email" 
                      value={formData.email}
                      onChange={e => setFormData({...formData, email: e.target.value})}
                      className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-semibold text-slate-700">Số điện thoại</label>
                    <input 
                      type="tel" 
                      value={formData.phone}
                      onChange={e => setFormData({...formData, phone: e.target.value})}
                      className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                    />
                  </div>
                </div>

                <div className="pt-6 border-t border-slate-100">
                  <div className="flex items-center justify-between">
                    <div>
                      <h4 className="text-sm font-semibold text-slate-800">Trạng thái tài khoản</h4>
                      <p className="text-xs text-slate-500 mt-1">Cho phép nhân sự này truy cập vào hệ thống</p>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input 
                        type="checkbox" 
                        className="sr-only peer" 
                        checked={formData.isActive}
                        onChange={e => setFormData({...formData, isActive: e.target.checked})}
                      />
                      <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                    </label>
                  </div>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
