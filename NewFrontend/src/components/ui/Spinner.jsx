import clsx from 'clsx';

export default function Spinner({ size = 'md', className = '' }) {
  const sizes = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-3',
    lg: 'w-12 h-12 border-4',
  };

  return (
    <div className={clsx('flex items-center justify-center p-4', className)}>
      <div
        className={clsx(
          'animate-spin rounded-full border-primary-200 border-t-primary-600',
          sizes[size]
        )}
      />
    </div>
  );
}
