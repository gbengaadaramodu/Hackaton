import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ClipboardList, Award, ArrowRight, CheckCircle2 } from 'lucide-react';
import { listAssessments, listMyAttempts } from '../../api/assessments';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function AssessmentList() {
  const { data: assessments = [], isLoading, error } = useQuery({
    queryKey: ['assessments'],
    queryFn: () => listAssessments(),
  });

  const { data: attempts = [] } = useQuery({
    queryKey: ['my-attempts'],
    queryFn: listMyAttempts,
  });

  const quizList = Array.isArray(assessments) ? assessments : (assessments?.items ?? []);
  const attemptList = Array.isArray(attempts) ? attempts : (attempts?.items ?? []);

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Curriculum Assessments</h1>
        <p className="text-sm text-slate-500 mt-1">
          Evaluate your understanding of simulation mechanics and curriculum objectives.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Available Quizzes (2 cols) */}
        <div className="lg:col-span-2 space-y-4">
          <h2 className="text-lg font-bold text-slate-800">Available Quizzes</h2>

          {isLoading ? (
            <Spinner />
          ) : error ? (
            <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
              Failed to load assessments: {error.message}
            </div>
          ) : quizList.length === 0 ? (
            <Card className="p-12 text-center bg-white border border-slate-200">
              <ClipboardList className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <h3 className="text-base font-semibold text-slate-700">No assessments found</h3>
              <p className="text-xs text-slate-400 mt-1">
                Assessments will appear here once attached to your enrolled lessons.
              </p>
            </Card>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {quizList.map((quiz) => (
                <Card
                  key={quiz.id}
                  className="p-5 flex flex-col justify-between hover:border-primary-300 transition-all group"
                >
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <Badge variant="primary" className="text-[11px]">
                        {quiz.questionCount ?? 0} Questions
                      </Badge>
                      <span className="text-[11px] text-slate-400">
                        v{quiz.version ?? 1}
                      </span>
                    </div>

                    <h3 className="font-bold text-slate-800 text-base group-hover:text-primary-600 transition-colors">
                      {quiz.title}
                    </h3>
                    <p className="text-xs text-slate-500 mt-1">
                      {quiz.modelKey ? `Model: ${quiz.modelKey}` : 'Curriculum comprehension check'}
                    </p>
                  </div>

                  <div className="pt-4 mt-4 border-t border-slate-100 flex items-center justify-between">
                    <span className="text-xs text-slate-400">Instant scoring</span>
                    <Link to={`/assessments/${quiz.id}`}>
                      <Button size="sm" variant="primary" className="text-xs">
                        Take Quiz <ArrowRight className="w-3.5 h-3.5 ml-1" />
                      </Button>
                    </Link>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* My Past Attempts (1 col) */}
        <div className="space-y-4">
          <h2 className="text-lg font-bold text-slate-800">Attempt History</h2>

          <Card className="p-4 divide-y divide-slate-100">
            {attemptList.length === 0 ? (
              <p className="text-xs text-slate-400 py-6 text-center">
                No quiz submissions recorded yet.
              </p>
            ) : (
              attemptList.map((attempt) => (
                <div key={attempt.attemptId || attempt.id} className="py-3 flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <h4 className="text-xs font-semibold text-slate-800 truncate">
                      {attempt.assessmentTitle || 'Curriculum Quiz'}
                    </h4>
                    <span className="text-[11px] text-slate-400 block mt-0.5">
                      {new Date(attempt.submittedAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                  </div>

                  <div className="text-right flex-shrink-0">
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
                      {attempt.passed ? 'Passed' : 'Reviewed'}
                    </Badge>
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
