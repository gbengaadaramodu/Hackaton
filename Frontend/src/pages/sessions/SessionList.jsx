import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Play, Clock, CheckCircle2, PauseCircle, ArrowRight } from 'lucide-react';
import { listSessions } from '../../api/sessions';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function SessionList() {
  const { data: sessions = [], isLoading, error } = useQuery({
    queryKey: ['sessions'],
    queryFn: () => listSessions(),
  });

  const sessionList = Array.isArray(sessions) ? sessions : (sessions?.items ?? []);

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Simulation Sessions</h1>
        <p className="text-sm text-slate-500 mt-1">
          Review previous runs, resume active simulations, and interact with the grounded AI tutor.
        </p>
      </div>

      {isLoading ? (
        <Spinner />
      ) : error ? (
        <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
          Failed to load sessions: {error.message}
        </div>
      ) : sessionList.length === 0 ? (
        <Card className="p-12 text-center bg-white border border-slate-200">
          <Play className="w-12 h-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-base font-semibold text-slate-700">No sessions recorded</h3>
          <p className="text-xs text-slate-400 mt-1 max-w-sm mx-auto">
            Start a session from the Lessons tab or Explore catalog to begin experimenting.
          </p>
          <Link to="/lessons">
            <Button size="sm" className="mt-4">
              Explore Lessons
            </Button>
          </Link>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {sessionList.map((session) => {
            const statusVariant =
              session.status === 'Completed'
                ? 'success'
                : session.status === 'Active'
                ? 'primary'
                : 'default';

            return (
              <Card
                key={session.id}
                className="p-6 flex flex-col justify-between hover:border-primary-300 transition-all group"
              >
                <div>
                  <div className="flex items-center justify-between gap-2 mb-3">
                    <Badge variant={statusVariant} className="text-xs">
                      {session.status}
                    </Badge>
                    <span className="text-[11px] text-slate-400 flex items-center gap-1">
                      <Clock className="w-3 h-3" />
                      {new Date(session.startedAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                  </div>

                  <h3 className="font-bold text-slate-800 text-base group-hover:text-primary-600 transition-colors">
                    {session.modelTitle || session.modelKey || 'Interactive Simulation'}
                  </h3>
                  <p className="text-xs text-slate-500 mt-1">
                    Version: {session.version ?? 1} • Commands: {session.commandCount ?? 0}
                  </p>
                </div>

                <div className="pt-4 mt-4 border-t border-slate-100 flex items-center justify-between">
                  <span className="text-xs text-slate-400 truncate max-w-[150px]">
                    {session.lessonTitle || 'Open Exploration'}
                  </span>
                  <Link to={`/sessions/${session.id}`}>
                    <Button size="sm" variant="primary" className="text-xs">
                      Resume <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </Button>
                  </Link>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
