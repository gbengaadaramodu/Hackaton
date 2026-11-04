import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  BookOpen,
  Compass,
  Play,
  ClipboardList,
  BarChart3,
  LogOut,
  GraduationCap,
  X,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import clsx from 'clsx';

const navItems = [
  { label: 'Dashboard',   icon: LayoutDashboard, to: '/dashboard' },
  { label: 'My Classes',  icon: Users,            to: '/classes' },
  { label: 'Lessons',     icon: BookOpen,         to: '/lessons' },
  { label: 'Explore',     icon: Compass,          to: '/explore' },
  { label: 'Sessions',    icon: Play,             to: '/sessions' },
  { label: 'Assessments', icon: ClipboardList,    to: '/assessments' },
  { label: 'Reports',     icon: BarChart3,        to: '/reports' },
];

export default function Sidebar({ onClose }) {
  const { user, logout } = useAuth();
  const initials = (user?.displayName ?? 'U')
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const primaryRole = user?.roles?.[0] ?? 'User';

  return (
    <div className="flex flex-col h-full w-64 bg-primary-700 text-white">
      {/* Logo */}
      <div className="flex items-center justify-between px-5 py-5 border-b border-white/10">
        <div className="flex items-center gap-2">
          <div className="bg-white/20 rounded-xl p-1.5">
            <GraduationCap className="w-5 h-5 text-white" />
          </div>
          <span className="font-bold text-lg tracking-tight">SimuLearn</span>
        </div>
        <button
          onClick={onClose}
          className="lg:hidden text-white/60 hover:text-white transition-colors"
        >
          <X className="w-5 h-5" />
        </button>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {navItems.map(({ label, icon: Icon, to }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              clsx(
                'flex items-center gap-3 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors',
                isActive
                  ? 'bg-white/20 text-white'
                  : 'text-white/70 hover:bg-white/10 hover:text-white'
              )
            }
          >
            <Icon className="w-4 h-4 flex-shrink-0" />
            {label}
          </NavLink>
        ))}

        {/* Separator */}
        <div className="border-t border-white/10 my-2" />

        {/* Logout */}
        <button
          onClick={logout}
          className="flex w-full items-center gap-3 px-4 py-2.5 rounded-xl text-sm font-medium text-white/70 hover:bg-white/10 hover:text-white transition-colors"
        >
          <LogOut className="w-4 h-4 flex-shrink-0" />
          Log Out
        </button>
      </nav>

      {/* User info at bottom */}
      <div className="px-4 py-4 border-t border-white/10">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-full bg-white/20 flex items-center justify-center font-semibold text-sm flex-shrink-0">
            {initials}
          </div>
          <div className="min-w-0">
            <p className="text-sm font-medium truncate">{user?.displayName ?? 'User'}</p>
            <p className="text-xs text-white/60 truncate">{primaryRole}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
