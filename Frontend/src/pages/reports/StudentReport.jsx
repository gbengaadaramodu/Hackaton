import { useQuery } from '@tanstack/react-query';
import {
  BarChart3,
  Award,
  Play,
  CheckCircle2,
  TrendingUp,
  Target,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { getStudentReport } from '../../api/reports';
import Card from '../../components/ui/Card';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function StudentReport() {
  const { user } = useAuth();
  const userId = user?.id;

  const {
    data: report,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['student-report', userId],
    queryFn: () => getStudentReport(userId),
    enabled: !!userId,
  });

  if (isLoading) return <Spinner />;
  if (error || !report) {
    return (
      <div className="p-6 bg-rose-50 text-rose-700 rounded-xl max-w-2xl mx-auto">
        Unable to load student report at this time.
      </div>
    );
  }

  const avgScore = report.averagePercentage != null ? Math.round(report.averagePercentage) : 0;
  const recentAttempts = Array.isArray(report.recentAttempts) ? report.recentAttempts : [];
  const objectives = Array.isArray(report.objectives) ? report.objectives : [];

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">
          Learning Progress & Analytics
        </h1>
        <p className="text-sm text-slate-500 mt-1">
          Detailed metrics covering simulation completions, assessment scores, and curriculum objective mastery.
        </p>
      </div>

      {/* Top Metrics Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="p-5 flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-primary-100 text-primary-700 flex items-center justify-center flex-shrink-0">
            <Play className="w-6 h-6 fill-current" />
          </div>
          <div>
            <span className="text-xs text-slate-500 font-medium">Sessions Started</span>
            <h3 className="text-2xl font-bold text-slate-800">
              {report.sessionsStarted ?? 0}
            </h3>
          </div>
        </Card>

        <Card className="p-5 flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-emerald-100 text-emerald-700 flex items-center justify-center flex-shrink-0">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-500 font-medium">Completed</span>
            <h3 className="text-2xl font-bold text-slate-800">
              {report.sessionsCompleted ?? 0}
            </h3>
          </div>
        </Card>

        <Card className="p-5 flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-amber-100 text-amber-700 flex items-center justify-center flex-shrink-0">
            <Award className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-500 font-medium">Quiz Attempts</span>
            <h3 className="text-2xl font-bold text-slate-800">
              {report.attempts ?? 0}
            </h3>
          </div>
        </Card>

        <Card className="p-5 flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-violet-100 text-violet-700 flex items-center justify-center flex-shrink-0">
            <TrendingUp className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-500 font-medium">Average Score</span>
            <h3 className="text-2xl font-bold text-slate-800">
              {avgScore}%
            </h3>
          </div>
        </Card>
      </div>

      {/* Split Grid: Objective Mastery & Recent Attempts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Objective Mastery */}
        <Card className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <Target className="w-5 h-5 text-primary-600" />
            <h3 className="font-bold text-slate-800 text-base">
              Curriculum Objective Mastery
            </h3>
          </div>

          {objectives.length === 0 ? (
            <p className="text-xs text-slate-400 py-6 text-center">
              Complete more assessments to calculate objective mastery.
            </p>
          ) : (
            <div className="space-y-4">
              {objectives.map((obj) => {
                const rate = obj.masteryRate != null ? Math.round(obj.masteryRate * 100) : 0;
                return (
                  <div key={obj.code} className="space-y-1.5">
                    <div className="flex justify-between items-center text-xs">
                      <span className="font-semibold text-slate-700 truncate max-w-[280px]">
                        {obj.description || obj.code}
                      </span>
                      <span className="font-bold text-slate-800">{rate}%</span>
                    </div>
                    <div className="w-full bg-slate-100 h-2 rounded-full overflow-hidden">
                      <div
                        className="bg-primary-600 h-full rounded-full transition-all duration-500"
                        style={{ width: `${Math.min(rate, 100)}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </Card>

        {/* Recent Attempts */}
        <Card className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <Award className="w-5 h-5 text-amber-500" />
            <h3 className="font-bold text-slate-800 text-base">
              Recent Quiz Performance
            </h3>
          </div>

          {recentAttempts.length === 0 ? (
            <p className="text-xs text-slate-400 py-6 text-center">
              No recent quiz submissions.
            </p>
          ) : (
            <div className="divide-y divide-slate-100">
              {recentAttempts.map((attempt) => (
                <div
                  key={attempt.attemptId || attempt.id}
                  className="py-3 flex items-center justify-between gap-3"
                >
                  <div>
                    <h4 className="text-xs font-semibold text-slate-800">
                      {attempt.assessmentTitle || 'Curriculum Quiz'}
                    </h4>
                    <span className="text-[10px] text-slate-400">
                      Submitted{' '}
                      {new Date(attempt.submittedAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                  </div>

                  <div className="text-right">
                    <span
                      className={`text-xs font-bold ${
                        attempt.passed ? 'text-emerald-600' : 'text-amber-600'
                      }`}
                    >
                      {Math.round(attempt.percentage)}%
                    </span>
                    <Badge
                      variant={attempt.passed ? 'success' : 'default'}
                      className="block text-[10px] mt-0.5"
                    >
                      {attempt.passed ? 'Passed' : 'Needs Review'}
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
