import { useState, useEffect } from 'react';
import { api } from '../services/api';
import type { Company, Department, MasterShift } from '../types';
import { Card, CardContent } from '../components/ui/Card';
import { Table, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/Table';
import { FolderTree, Users, Clock, Building2, Search, Eye } from 'lucide-react';
import CompanyDetail from './CompanyDetail';

export default function Categories() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [masterShifts, setMasterShifts] = useState<MasterShift[]>([]);
  
  const [activeTab, setActiveTab] = useState<'org' | 'masterShifts'>('org');
  const [selectedCompanyId, setSelectedCompanyId] = useState<string | null>(null);
  
  // Navigation State
  const [selectedCompany, setSelectedCompany] = useState<Company | null>(null);
  
  // Search state
  const [companySearchTerm, setCompanySearchTerm] = useState('');

  useEffect(() => {
    if (!selectedCompany) {
      api.getCompanies().then(data => {
        setCompanies(data);
        if (data.length > 0 && !selectedCompanyId) setSelectedCompanyId(data[0].id);
      });
      api.getDepartments().then(setDepartments);
      api.getMasterShifts().then(setMasterShifts);
    }
  }, [selectedCompany, selectedCompanyId]);

  const filteredCompanies = companies.filter(c => 
    c.name.toLowerCase().includes(companySearchTerm.toLowerCase())
  );

  if (selectedCompany) {
    const deptCount = departments.filter(d => d.companyId === selectedCompany.id).length;
    return <CompanyDetail company={selectedCompany} departmentCount={deptCount} onBack={() => setSelectedCompany(null)} />;
  }

  const TabButton = ({ id, label, icon: Icon, active }: { id: typeof activeTab, label: string, icon: any, active: boolean }) => (
    <button
      onClick={() => setActiveTab(id)}
      className={`flex items-center gap-2 px-6 py-2.5 text-sm font-semibold rounded-full transition-colors ${
        active 
          ? 'bg-blue-600 text-white' 
          : 'bg-transparent text-slate-500 hover:text-slate-800'
      }`}
    >
      <Icon className={`w-4 h-4 ${active ? 'text-white' : 'text-slate-400'}`} />
      {label}
    </button>
  );

  return (
    <div className="space-y-6 animate-fade-in-up h-[calc(100vh-8rem)] flex flex-col">
      <div>
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">Quản lý Danh mục</h2>
        <p className="text-slate-500 mt-1 text-sm">Thiết lập cơ cấu tổ chức và tiêu chuẩn vị trí</p>
      </div>

      <div className="flex p-1 bg-slate-100 rounded-full w-fit">
        <TabButton id="org" label="Công ty & Phòng ban" icon={Building2} active={activeTab === 'org'} />
        <TabButton id="masterShifts" label="Ca chuẩn" icon={Clock} active={activeTab === 'masterShifts'} />
      </div>

      <div className="mt-2 flex-1 min-h-0">
        {activeTab === 'org' && (
          <div className="grid grid-cols-12 gap-6 items-start h-full">
            {/* 40% Width: Companies */}
            <div className="col-span-12 lg:col-span-5 h-full flex flex-col">
              <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden flex flex-col h-full max-h-[700px]">
                <div className="bg-slate-50 p-4 border-b border-slate-200 space-y-3 shrink-0">
                  <h3 className="text-base font-semibold text-slate-800 flex items-center gap-2">
                    <FolderTree className="w-5 h-5 text-blue-600" />
                    Danh sách Công ty
                  </h3>
                  <div className="relative">
                    <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
                    <input 
                      type="text" 
                      placeholder="Tìm kiếm công ty..." 
                      value={companySearchTerm}
                      onChange={(e) => setCompanySearchTerm(e.target.value)}
                      className="w-full pl-9 pr-4 py-2 bg-white border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                    />
                  </div>
                </div>
                <CardContent className="p-0 overflow-y-auto flex-1 min-h-0">
                  <Table>
                    <TableHeader className="bg-white sticky top-0 z-10 shadow-sm">
                      <TableRow>
                        <TableHead className="w-20">Mã số</TableHead>
                        <TableHead>Tên Công ty</TableHead>
                        <TableHead className="w-16">{" "}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <tbody className="divide-y divide-slate-100">
                      {filteredCompanies.map((c) => {
                        const originalIndex = companies.findIndex(comp => comp.id === c.id);
                        return (
                        <TableRow 
                          key={c.id} 
                          onClick={() => setSelectedCompanyId(c.id)}
                          className={`cursor-pointer transition-colors group ${
                            selectedCompanyId === c.id ? 'bg-blue-50 hover:bg-blue-50' : 'hover:bg-slate-50'
                          }`}
                        >
                          <TableCell className="font-mono text-xs text-slate-500 font-medium whitespace-nowrap">
                            CT-{String(originalIndex + 1).padStart(2, '0')}
                          </TableCell>
                          <TableCell className={`text-sm ${selectedCompanyId === c.id ? 'font-semibold text-blue-700' : 'font-medium text-slate-800'}`}>
                            {c.name}
                          </TableCell>
                          <TableCell className="text-right">
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                setSelectedCompany(c);
                              }}
                              className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-100 rounded-md transition-colors opacity-0 group-hover:opacity-100"
                              title="Xem chi tiết"
                            >
                              <Eye className="w-4 h-4" />
                            </button>
                          </TableCell>
                        </TableRow>
                      )})}
                      {filteredCompanies.length === 0 && (
                        <TableRow><TableCell colSpan={3} className="text-center py-8 text-slate-400 text-sm">Không tìm thấy công ty nào</TableCell></TableRow>
                      )}
                    </tbody>
                  </Table>
                </CardContent>
              </Card>
            </div>

            {/* 60% Width: Departments */}
            <div className="col-span-12 lg:col-span-7 h-full flex flex-col">
              <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden flex flex-col h-full max-h-[700px]">
                <div className="bg-slate-50 p-4 border-b border-slate-200 flex justify-between items-center shrink-0 min-h-[105px]">
                  <div className="space-y-1">
                    <h3 className="text-base font-semibold text-slate-800 flex items-center gap-2">
                      <Users className="w-5 h-5 text-blue-600" />
                      Danh sách Phòng ban
                    </h3>
                    <p className="text-xs text-slate-500">
                      {selectedCompanyId ? companies.find(c => c.id === selectedCompanyId)?.name : 'Vui lòng chọn công ty'}
                    </p>
                  </div>
                  {selectedCompanyId && (
                    <span className="text-xs font-medium bg-blue-100 text-blue-700 px-2 py-1 rounded-md border border-blue-200">
                      Thuộc CT-{String(companies.findIndex(c => c.id === selectedCompanyId) + 1).padStart(2, '0')}
                    </span>
                  )}
                </div>
                <CardContent className="p-0 overflow-y-auto flex-1 min-h-0">
                  <Table>
                    <TableHeader className="bg-white sticky top-0 z-10 shadow-sm">
                      <TableRow>
                        <TableHead className="w-20">Mã số</TableHead>
                        <TableHead>Tên Phòng ban</TableHead>
                      </TableRow>
                    </TableHeader>
                    <tbody className="divide-y divide-slate-100">
                      {departments.filter(d => d.companyId === selectedCompanyId).map((d, index) => (
                        <TableRow key={d.id} className="hover:bg-slate-50 transition-colors">
                          <TableCell className="font-mono text-xs text-slate-500 font-medium whitespace-nowrap">PB-{String(index + 1).padStart(2, '0')}</TableCell>
                          <TableCell className="font-semibold text-slate-800 text-sm">{d.name}</TableCell>
                        </TableRow>
                      ))}
                      {departments.filter(d => d.companyId === selectedCompanyId).length === 0 && (
                        <TableRow>
                          <TableCell colSpan={2} className="text-center py-12 text-slate-400 text-sm">
                            {selectedCompanyId ? 'Công ty này chưa có phòng ban nào' : 'Vui lòng chọn công ty bên trái'}
                          </TableCell>
                        </TableRow>
                      )}
                    </tbody>
                  </Table>
                </CardContent>
              </Card>
            </div>
          </div>
        )}

        {activeTab === 'masterShifts' && (
          <Card className="border border-slate-200 shadow-sm rounded-xl overflow-hidden max-h-[700px] flex flex-col">
            <div className="bg-slate-50 p-4 border-b border-slate-200 shrink-0">
              <h3 className="text-base font-semibold text-slate-800 flex items-center gap-2">
                <Clock className="w-5 h-5 text-blue-600" />
                Danh sách Ca làm việc chuẩn
              </h3>
            </div>
            <CardContent className="p-0 overflow-y-auto flex-1 min-h-0">
              <Table>
                <TableHeader className="bg-white sticky top-0 z-10 shadow-sm">
                  <TableRow>
                    <TableHead className="w-24">Mã số</TableHead>
                    <TableHead>Tên Ca làm việc</TableHead>
                    <TableHead>Giờ Bắt đầu</TableHead>
                    <TableHead>Giờ Kết thúc</TableHead>
                    <TableHead>Qua đêm</TableHead>
                  </TableRow>
                </TableHeader>
                <tbody className="divide-y divide-slate-100">
                  {masterShifts.map((ms, index) => (
                    <TableRow key={ms.id} className="hover:bg-slate-50/50 transition-colors">
                      <TableCell className="font-mono text-xs text-slate-500 font-medium">CC-{String(index + 1).padStart(2, '0')}</TableCell>
                      <TableCell className="font-medium text-slate-800 text-sm">{ms.name}</TableCell>
                      <TableCell className="font-medium text-slate-600 text-sm">{ms.shiftStart}</TableCell>
                      <TableCell className="font-medium text-slate-600 text-sm">{ms.shiftEnd}</TableCell>
                      <TableCell className="text-slate-500 text-sm">
                        {ms.isOvernight ? (
                          <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-700 border border-blue-200">Qua đêm</span>
                        ) : (
                          <span className="text-slate-400">—</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                  {masterShifts.length === 0 && (
                    <TableRow><TableCell colSpan={5} className="text-center py-8 text-slate-400 text-sm">Không có dữ liệu</TableCell></TableRow>
                  )}
                </tbody>
              </Table>
            </CardContent>
          </Card>
        )}

      </div>
    </div>
  );
}
