import { useAuth } from '../../contexts/AuthContext';

export default function WelcomeBanner() {
  const { user, isTeacher, isAdmin } = useAuth();
  const name = user?.displayName ?? 'there';
  const portalLabel = isAdmin ? 'Admin Portal' : isTeacher ? 'Teacher Portal' : 'Student Portal';

  const now = new Date();
  const dateStr = now.toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  return (
    <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary-600 to-primary-800 text-white px-8 py-8 shadow-lg">
      {/* Decorative circles */}
      <div className="absolute -top-8 -right-8 w-40 h-40 rounded-full bg-white/10" />
      <div className="absolute top-8 -right-2 w-24 h-24 rounded-full bg-white/10" />
      <div className="absolute -bottom-6 right-32 w-28 h-28 rounded-full bg-white/5" />

      <div className="relative z-10 flex items-center justify-between">
        <div>
          <p className="text-primary-200 text-sm font-medium mb-1">{dateStr}</p>
          <h1 className="text-2xl md:text-3xl font-bold mb-1">
            Welcome back, {name}! 👋
          </h1>
          <p className="text-primary-200 text-sm">
            Always stay updated in your learning portal
          </p>
        </div>

        {/* Decorative SVG student illustration */}
        <div className="hidden md:flex flex-col items-center gap-2 opacity-90">
          <svg
            width="80"
            height="80"
            viewBox="0 0 80 80"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            {/* Cap */}
            <ellipse cx="40" cy="24" rx="22" ry="6" fill="white" fillOpacity="0.3" />
            <rect x="36" y="12" width="8" height="12" rx="2" fill="white" fillOpacity="0.3" />
            {/* Head */}
            <circle cx="40" cy="36" r="12" fill="white" fillOpacity="0.4" />
            {/* Body */}
            <rect x="26" y="50" width="28" height="18" rx="6" fill="white" fillOpacity="0.3" />
            {/* Book */}
            <rect x="44" y="54" width="12" height="10" rx="2" fill="white" fillOpacity="0.5" />
            <line x1="50" y1="54" x2="50" y2="64" stroke="white" strokeOpacity="0.7" strokeWidth="1" />
          </svg>
          <span className="text-xs text-white/60 font-medium">{portalLabel}</span>
        </div>
      </div>
    </div>
  );
}
