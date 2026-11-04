import { Bell, Search, Menu } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import clsx from 'clsx';

export default function Header({ onMenuClick }) {
  const { user } = useAuth();

  const initials = (user?.displayName ?? 'U')
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const primaryRole = user?.roles?.[0] ?? 'User';

  const roleBadgeColor = {
    Student: 'bg-blue-100 text-blue-700',
    Teacher: 'bg-green-100 text-green-700',
    Administrator: 'bg-red-100 text-red-700',
  }[primaryRole] ?? 'bg-slate-100 text-slate-600';

  return (
    <header className="h-16 bg-white border-b border-slate-200 flex items-center px-4 md:px-6 gap-4 flex-shrink-0">
      {/* Hamburger (mobile) */}
      <button
        onClick={onMenuClick}
        className="lg:hidden p-2 rounded-lg hover:bg-slate-100 transition-colors"
        aria-label="Open menu"
      >
        <Menu className="w-5 h-5 text-slate-600" />
      </button>

      {/* Search bar */}
      <div className="flex-1 max-w-md hidden sm:block">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search…"
            className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 bg-slate-50 text-sm text-slate-600 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-primary-300 focus:border-transparent"
          />
        </div>
      </div>

      <div className="ml-auto flex items-center gap-3">
        {/* Notification bell */}
        <button className="relative p-2 rounded-xl hover:bg-slate-100 transition-colors">
          <Bell className="w-5 h-5 text-slate-500" />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-primary-600" />
        </button>

        {/* User info */}
        <div className="flex items-center gap-2.5">
          <div className="w-9 h-9 rounded-full bg-primary-600 flex items-center justify-center text-white font-semibold text-sm">
            {initials}
          </div>
          <div className="hidden md:block">
            <p className="text-sm font-semibold text-slate-800 leading-tight">{user?.displayName ?? 'User'}</p>
            <span className={clsx('text-xs font-medium px-1.5 py-0.5 rounded-full', roleBadgeColor)}>
              {primaryRole}
            </span>
          </div>
        </div>
      </div>
    </header>
  );
}
