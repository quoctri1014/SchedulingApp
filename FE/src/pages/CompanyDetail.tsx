import { useState } from 'react';
import { ArrowLeft, Save, Building2, AlignLeft, BarChart3, Users as UsersIcon } from 'lucide-react';
import type { Company } from '../types';
import { Card, CardContent } from '../components/ui/Card';

interface CompanyDetailProps {
  company: Company;
  departmentCount: number;
  onBack: () => void;
}

export default function CompanyDetail({ company, departmentCount, onBack }: CompanyDetailProps) {
  const [formData, setFormData] = useState({
    name: company.name || '',
    description: company.description || '',
  });

  const [saving, setSaving] = useState(false);

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
            title="Quay lại danh mục"
          >
            <ArrowLeft className="w-6 h-6" />
          </button>
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-slate-900">Hồ sơ Công ty</h2>
            <p className="text-slate-500 mt-1 text-sm">Xem và cập nhật thông tin công ty</p>
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
        {/* Left Column: Summary */}
        <div className="md:col-span-1 space-y-6">
          <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden text-center">
            <CardContent className="p-6">
              <div className="w-16 h-16 mx-auto rounded-xl bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600 flex items-center justify-center shadow-sm mb-4">
                <Building2 className="w-8 h-8" />
              </div>
              <h3 className="text-sm font-semibold text-slate-800 leading-snug px-2">{formData.name}</h3>
              <p className="text-xs text-slate-500 font-mono mt-1.5 truncate" title={company.id}>ID: {company.id}</p>
              
              <div className="mt-6 pt-6 border-t border-slate-100 grid grid-cols-2 gap-4">
                <div className="bg-slate-50/80 rounded-lg p-3">
                  <div className="flex items-center justify-center gap-1.5 text-indigo-600 mb-1">
                    <UsersIcon className="w-3.5 h-3.5" />
                    <span className="font-semibold text-xs">Phòng ban</span>
                  </div>
                  <span className="text-lg font-bold text-slate-800">{departmentCount}</span>
                </div>
                <div className="bg-slate-50/80 rounded-lg p-3">
                  <div className="flex items-center justify-center gap-1.5 text-emerald-600 mb-1">
                    <BarChart3 className="w-3.5 h-3.5" />
                    <span className="font-semibold text-xs">Hoạt động</span>
                  </div>
                  <span className="text-lg font-bold text-slate-800">Tốt</span>
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
                <Building2 className="w-5 h-5 text-blue-600" />
                Thông tin chung
              </h3>
            </div>
            <CardContent className="p-6">
              <form className="space-y-6">
                <div className="space-y-2">
                  <label className="text-sm font-semibold text-slate-700">Tên công ty / Chi nhánh</label>
                  <input 
                    type="text" 
                    value={formData.name}
                    onChange={e => setFormData({...formData, name: e.target.value})}
                    className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all text-slate-800"
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-semibold text-slate-700 flex items-center gap-2">
                    <AlignLeft className="w-4 h-4 text-slate-400" />
                    Mô tả chi tiết
                  </label>
                  <textarea 
                    rows={4}
                    value={formData.description}
                    onChange={e => setFormData({...formData, description: e.target.value})}
                    className="w-full px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all resize-none"
                    placeholder="Nhập mô tả cho công ty..."
                  />
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
