import clsx from 'clsx';

export default function Card({ children, className = '', ...props }) {
  return (
    <div
      className={clsx(
        'bg-white rounded-2xl border border-slate-100 shadow-sm transition-all hover:shadow-md',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}
