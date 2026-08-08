import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Categories from './pages/Categories';
import Shifts from './pages/Shifts';
import Schedule from './pages/Schedule';
import UsersPage from './pages/Users';

function Sidebar() {
  const location = useLocation();

  const NavItem = ({ to, children }: { to: string; children: React.ReactNode }) => {
    const isActive = location.pathname === to;
    return (
      <Link 
        to={to} 
        className={`block px-5 py-2.5 text-sm transition-colors duration-200 ${
          isActive 
            ? 'border-l-2 border-blue-600 bg-blue-50/50 text-blue-700 font-medium' 
            : 'border-l-2 border-transparent text-slate-600 hover:bg-slate-50 hover:text-slate-900'
        }`}
      >
        {children}
      </Link>
    );
  };

  return (
    <aside className="w-64 bg-white border-r border-slate-200 flex flex-col shrink-0">
      <div className="p-6 pb-8">
        <h1 className="text-lg font-bold tracking-tight text-slate-900">
          Scheduling<span className="text-blue-600">App</span>
        </h1>
      </div>
      
      <div className="flex-1 overflow-y-auto flex flex-col gap-1 custom-scrollbar">
        <NavItem to="/">Bảng điều khiển</NavItem>
        
        <div className="mt-6 mb-2 px-5 text-xs font-semibold text-slate-400 uppercase tracking-wider">
          Dữ liệu
        </div>
        <NavItem to="/categories">Danh mục</NavItem>
        <NavItem to="/users">Người dùng & Phân công</NavItem>
        <NavItem to="/shifts">Ca làm việc</NavItem>
        
        <div className="mt-6 mb-2 px-5 text-xs font-semibold text-slate-400 uppercase tracking-wider">
          Hoạt động
        </div>
        <NavItem to="/schedule">Chạy Xếp lịch</NavItem>
      </div>
    </aside>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <div className="flex h-screen bg-slate-50 font-sans text-slate-900 overflow-hidden antialiased">
        <Sidebar />
        <main className="flex-1 h-full overflow-y-auto custom-scrollbar">
          <div className="max-w-7xl mx-auto p-8 pb-12">
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/categories" element={<Categories />} />
              <Route path="/users" element={<UsersPage />} />
              <Route path="/shifts" element={<Shifts />} />
              <Route path="/schedule" element={<Schedule />} />
            </Routes>
          </div>
        </main>
      </div>
    </BrowserRouter>
  );
}
