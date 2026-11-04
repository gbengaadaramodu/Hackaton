import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Play, CheckCircle, Clock, Award, ArrowRight } from 'lucide-react';
import { listSessions } from '../../api/sessions';
import { listMyAttempts } from '../../api/assessments';
import Card from '../ui/Card';
import Badge from '../ui/Badge';
import Spinner from '../ui/Spinner';

export default function RightWidgets() {
  const { data: sessions = [], isLoading: loadingSessions } = useQuery({
    queryKey: ['sessions', 'recent'],
    queryFn: () => listSessions(),
  });

  const { data: attempts = [], isLoading: loadingAttempts } = useQuery({
    queryKey: ['my-attempts'],
    queryFn: listMyAttempts,
  });

  const sessionList = Array.isArray(sessions) ? sessions : (sessions?.items ?? []);
  const attemptList = Array.isArray(attempts) ? attempts : (attempts?.items ?? []);

  return (
    <div className="space-y-6">
      {/* Recent Simulation Sessions */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-base font-bold text-slate-800">Recent Sessions</h3>
          <Link
            to="/sessions"
            className="text-xs font-semibold text-primary-600 hover:text-primary-700 flex items-center"
          >
            All <ArrowRight className="w-3 h-3 ml-0.5" />
          </Link>
        </div>

        {loadingSessions ? (
          <Spinner size="sm" />
        ) : sessionList.length === 0 ? (
          <Card className="p-4 text-center bg-white border border-slate-100">
            <Clock className="w-6 h-6 text-slate-300 mx-auto mb-1.5" />
            <p className="text-xs font-medium text-slate-600">No sessions yet</p>
            <p className="text-[11px] text-slate-400 mt-0.5">
              Launch a simulation from Explore or Lessons.
            </p>
          </Card>
        ) : (
          <div className="space-y-2.5">
            {sessionList.slice(0, 3).map((session) => (
              <Card
                key={session.id}
                className="p-3.5 hover:border-primary-200 transition-all flex items-center justify-between gap-3"
              >
                <div className="min-w-0">
                  <div className="flex items-center gap-1.5 mb-1">
                    <Badge
                      variant={
                        session.status === 'Completed'
                          ? 'success'
                          : session.status === 'Active'
                          ? 'primary'
                          : 'default'
                      }
                      className="text-[10px] px-1.5 py-0.2"
                    >
                      {session.status}
                    </Badge>
                  </div>
                  <h4 className="text-xs font-semibold text-slate-800 truncate">
                    {session.modelTitle || session.modelKey || 'Simulation Session'}
                  </h4>
                  <p className="text-[10px] text-slate-400 mt-0.5">
                    {new Date(session.lastActivityAt || session.startedAt).toLocaleDateString(
                      'en-US',
                      { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }
                    )}
                  </p>
                </div>

                <Link
                  to={`/sessions/${session.id}`}
                  className="w-8 h-8 rounded-xl bg-primary-50 text-primary-600 hover:bg-primary-600 hover:text-white flex items-center justify-center transition-colors flex-shrink-0"
                  title="Open Simulation Workspace"
                >
                  <Play className="w-3.5 h-3.5 fill-current" />
                </Link>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Assessment Activity */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-base font-bold text-slate-800">Recent Assessments</h3>
          <Link
            to="/assessments"
            className="text-xs font-semibold text-primary-600 hover:text-primary-700 flex items-center"
          >
            All <ArrowRight className="w-3 h-3 ml-0.5" />
          </Link>
        </div>

        {loadingAttempts ? (
          <Spinner size="sm" />
        ) : attemptList.length === 0 ? (
          <Card className="p-4 text-center bg-white border border-slate-100">
            <Award className="w-6 h-6 text-slate-300 mx-auto mb-1.5" />
            <p className="text-xs font-medium text-slate-600">No attempts yet</p>
            <p className="text-[11px] text-slate-400 mt-0.5">
              Take assessments to test comprehension.
            </p>
          </Card>
        ) : (
          <div className="space-y-2.5">
            {attemptList.slice(0, 3).map((attempt) => (
              <Card
                key={attempt.attemptId || attempt.id}
                className="p-3.5 flex items-center justify-between gap-3"
              >
                <div className="min-w-0">
                  <h4 className="text-xs font-semibold text-slate-800 truncate">
                    {attempt.assessmentTitle || 'Curriculum Quiz'}
                  </h4>
                  <p className="text-[10px] text-slate-400 mt-0.5">
                    Submitted{' '}
                    {new Date(attempt.submittedAt).toLocaleDateString('en-US', {
                      month: 'short',
                      day: 'numeric',
                    })}
                  </p>
                </div>
                <div className="text-right flex-shrink-0">
                  <span
                    className={`text-xs font-bold ${
                      attempt.passed ? 'text-emerald-600' : 'text-amber-600'
                    }`}
                  >
                    {Math.round(attempt.percentage)}%
                  </span>
                  <p className="text-[10px] text-slate-400">
                    {attempt.passed ? 'Passed' : 'Needs review'}
                  </p>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
