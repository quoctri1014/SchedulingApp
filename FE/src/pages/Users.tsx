import { useState, useEffect } from 'react';
import { Search, RefreshCw } from 'lucide-react';
import { api } from '../services/api';
import type { User, PagedResult } from '../types';
import { Button } from '../components/ui/Button';
import UserDetail from './UserDetail';

export default function Users() {
  const [data, setData] = useState<PagedResult<User> | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [search, setSearch] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(15);
  
  // Navigation State
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await api.getUsers({
        search: search.trim(),
        pageNumber: page,
        pageSize: pageSize,
      });
      setData(res);
    } catch (err) {
      console.error('Error fetching users:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!selectedUser) {
      fetchUsers();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, selectedUser]); // Re-fetch when backing out if needed

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    fetchUsers();
  };

  if (selectedUser) {
    return <UserDetail user={selectedUser} onBack={() => setSelectedUser(null)} />;
  }

  return (
    <div className="space-y-6 animate-fade-in-up">
      {/* Header & Stats */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Danh sách Người dùng & Phân công</h1>
          <p className="text-slate-500 text-sm mt-1">
            Đồng bộ dữ liệu từ FaceAttendanceSystem ({data?.totalCount?.toLocaleString() || 0} nhân sự)
          </p>
        </div>

        <button 
          onClick={fetchUsers} 
          className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-200 rounded-lg text-sm text-slate-700 font-semibold shadow-sm hover:bg-slate-50 transition-all"
        >
          <RefreshCw className={`w-4 h-4 text-blue-500 ${loading ? 'animate-spin' : ''}`} />
          Làm mới
        </button>
      </div>

      {/* Search & Filter bar */}
      <div className="bg-white p-2 border border-slate-200 rounded-xl shadow-sm shrink-0">
        <form onSubmit={handleSearchSubmit} className="flex gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              placeholder="Tìm kiếm theo Mã NV, Họ tên, Email..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-transparent rounded-lg text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
            />
          </div>
          <button 
            type="submit" 
            className="px-6 py-2 bg-blue-600 text-white text-sm font-semibold rounded-lg shadow-sm hover:bg-blue-700 transition-colors"
          >
            Tìm kiếm
          </button>
        </form>
      </div>

      {/* Main Table */}
      <div className="-mx-6 px-6">
        <div className="overflow-x-auto">
          {loading ? (
            <div className="p-16 text-center text-slate-500 flex flex-col items-center gap-4">
              <RefreshCw className="w-8 h-8 animate-spin text-blue-600" />
              <span className="font-medium text-sm">Đang tải danh sách người dùng...</span>
            </div>
          ) : !data || data.items.length === 0 ? (
            <div className="p-16 text-center text-slate-500 font-medium text-sm">
              Không tìm thấy người dùng nào phù hợp.
            </div>
          ) : (
            <table className="w-full text-left border-collapse">
              <thead className="bg-slate-50 sticky top-0 z-10 shadow-sm">
                <tr className="border-b border-slate-200 text-slate-600 font-semibold text-xs uppercase tracking-wider">
                  <th className="py-3 px-4 whitespace-nowrap">Mã NV</th>
                  <th className="py-3 px-4 whitespace-nowrap">Họ Tên</th>
                  <th className="py-3 px-4 whitespace-nowrap">Email</th>
                  <th className="py-3 px-4 whitespace-nowrap">Số Điện Thoại</th>
                  <th className="py-3 px-4 whitespace-nowrap">Công Ty</th>
                  <th className="py-3 px-4 whitespace-nowrap">Phòng Ban</th>
                  <th className="py-3 px-4 whitespace-nowrap text-center">Trạng Thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data.items.map((u) => {
                  const companyAssignments = u.companyAssignments || [];
                  const departmentAssignments = u.departmentAssignments || [];
                  const primaryComp = companyAssignments.find((ca) => ca.isPrimaryCompany) || companyAssignments[0];
                  const primaryDept = departmentAssignments.find((da) => da.isPrimaryDepartment) || departmentAssignments[0];
                  const name = u.fullName || u.name || 'N/A';
                  const code = u.code || u.id;

                  return (
                    <tr 
                      key={u.id} 
                      onClick={() => setSelectedUser(u)}
                      className="hover:bg-blue-50/50 transition-colors cursor-pointer group"
                    >
                      <td className="py-3 px-4">
                        <span className="font-mono text-xs font-semibold text-blue-600 bg-blue-50 px-2 py-1 rounded border border-blue-100">
                          {code}
                        </span>
                      </td>
                      <td className="py-3 px-4 font-semibold text-slate-800 text-sm whitespace-nowrap group-hover:text-blue-700 transition-colors">
                        {name}
                      </td>
                      <td className="py-3 px-4 text-sm text-slate-600 truncate max-w-[200px]" title={u.email}>
                        {u.email || '-'}
                      </td>
                      <td className="py-3 px-4 text-sm text-slate-600 whitespace-nowrap">
                        {u.phone || '-'}
                      </td>
                      <td className="py-3 px-4 text-sm text-slate-700 truncate max-w-[200px]" title={primaryComp?.companyName}>
                        {primaryComp?.companyName || <span className="text-slate-400 italic text-xs">Chưa phân công</span>}
                      </td>
                      <td className="py-3 px-4 text-sm text-slate-700 truncate max-w-[200px]" title={primaryDept?.departmentName}>
                        {primaryDept?.departmentName || <span className="text-slate-400 italic text-xs">Chưa phân công</span>}
                      </td>
                      <td className="py-3 px-4 text-center">
                        {u.isActive ? (
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700 border border-emerald-200">
                            Hoạt động
                          </span>
                        ) : (
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200">
                            Khóa
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalPages > 0 && (
          <div className="flex items-center justify-between py-4 mt-4 border-t border-slate-200">
            <div className="flex items-center gap-6">
              <span className="text-xs text-slate-500 font-medium">
                Trang {data.pageNumber} / {data.totalPages} ({data.totalCount} kết quả)
              </span>
              <div className="flex items-center gap-2">
                <span className="text-xs text-slate-500">Hiển thị:</span>
                <select
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(Number(e.target.value));
                    setPage(1);
                  }}
                  className="text-xs bg-white border border-slate-200 rounded-md px-2 py-1 outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 cursor-pointer"
                >
                  <option value={15}>15</option>
                  <option value={20}>20</option>
                  <option value={50}>50</option>
                  <option value={100}>100</option>
                </select>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                className="text-xs py-1 h-8"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Trước
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="text-xs py-1 h-8"
                disabled={page >= data.totalPages}
                onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              >
                Sau
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
