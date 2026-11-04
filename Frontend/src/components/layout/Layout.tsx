import type { ReactNode } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import {
  Home,
  CreditCard,
  FileText,
  BookOpen,
  Trash2,
  Award,
  Bell,
  Calendar,
  LogOut,
  ChevronRight,
  Search,
  GraduationCap,
} from 'lucide-react';

interface LayoutProps {
  children: ReactNode;
}

export const Layout = ({ children }: LayoutProps) => {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!isAuthenticated) {
    return <div className="min-h-screen bg-[#fafafa] flex items-center justify-center">{children}</div>;
  }

  const navigation = [
    { name: 'Dashboard', href: '/dashboard', icon: Home },
    { name: 'Payment Info', href: '/payment-info', icon: CreditCard },
    { name: 'Registration', href: '/registration', icon: FileText },
    { name: 'Courses', href: '/explore', icon: BookOpen, active: location.pathname.includes('/explore') || location.pathname.includes('/simulation') },
    { name: 'Drop Semester', href: '/drop-semester', icon: Trash2 },
    { name: 'Result', href: '/result', icon: Award },
    { name: 'Notice', href: '/notice', icon: Bell },
    { name: 'Schedule', href: '/schedule', icon: Calendar },
  ];

  return (
    <div className="min-h-screen bg-[#fafafa] flex flex-col">
      <aside className="fixed left-0 top-0 h-screen w-64 purple-gradient z-50 flex flex-col">
        <div className="flex items-center gap-3 px-6 py-8">
          <div className="w-12 h-12 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-lg border border-white/10">
            <GraduationCap className="h-6 w-6 text-primary-500" />
          </div>
          <div>
            <h1 className="text-white font-bold text-lg tracking-tight">Student<span className="text-primary-200">Portal</span></h1>
            <p className="text-primary-100 text-xs font-medium opacity-90">Academic Dashboard</p>
          </div>
        </div>

        <nav className="flex-1 px-4 py-2 overflow-y-auto">
          {navigation.map((item) => {
            const isActive = item.active || location.pathname === item.href;
            return (
              <Link
                key={item.name}
                to={item.href}
                className={`nav-item flex items-center gap-3 px-4 py-3 rounded-xl mb-1.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-white/25 text-white shadow-lg border border-white/10'
                    : 'text-primary-100/80 hover:bg-white/15 hover:text-white'
                }`}
              >
                <item.icon className="h-5 w-5" />
                <span>{item.name}</span>
                {isActive && <ChevronRight className="h-4 w-4 ml-auto text-primary-300" />}
              </Link>
            );
          })}
        </nav>

        <div className="px-4 py-4 border-t border-white/10">
          <button
            onClick={handleLogout}
            className="nav-item w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium text-primary-100/80 hover:bg-white/15 hover:text-white"
          >
            <LogOut className="h-5 w-5" />
            <span>Logout</span>
          </button>
        </div>
      </aside>

      <div className="flex-1 ml-64 pt-0">
        <header className="flex items-center justify-between px-8 py-6 md:px-10 lg:px-12">
          <div className="relative w-full max-w-md">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search"
              className="w-full pl-10 pr-4 py-2.5 rounded-2xl border border-gray-200 bg-white text-sm font-medium text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500/30 focus:border-primary-400 transition-all shadow-sm"
            />
          </div>

          <div className="flex items-center gap-4">
            <div className="hidden md:flex flex-col items-end text-right">
              <p className="text-sm font-bold text-gray-900">{user?.displayName || 'John Doe'}</p>
              <p className="text-xs text-gray-500 font-medium">3rd year</p>
            </div>
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white text-sm font-bold shadow-md border-2 border-white">
              {user?.displayName?.charAt(0).toUpperCase() || 'J'}
            </div>
          </div>
        </header>

        <main className="px-8 md:px-10 lg:px-12 pb-8">
          {children}
        </main>
      </div>
    </div>
  );
};