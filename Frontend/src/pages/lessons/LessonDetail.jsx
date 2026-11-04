import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { ArrowLeft, Play, Target, CheckCircle2, BookOpen } from 'lucide-react';
import { getLesson } from '../../api/lessons';
import { startSession } from '../../api/sessions';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function LessonDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [errorMsg, setErrorMsg] = useState('');

  const {
    data: lesson,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['lesson', id],
    queryFn: () => getLesson(id),
    enabled: !!id,
  });

  const launchMutation = useMutation({
    mutationFn: () => startSession({ lessonId: id }),
    onSuccess: (session) => {
      navigate(`/sessions/${session.id}`);
    },
    onError: (err) => {
      setErrorMsg(
        err.response?.data?.detail || 'Failed to start simulation session.'
      );
    },
  });

  if (isLoading) return <Spinner />;
  if (error || !lesson) {
    return (
      <div className="p-6 bg-rose-50 text-rose-700 rounded-xl max-w-2xl mx-auto">
        Lesson not found or failed to load.
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div>
        <Link
          to="/lessons"
          className="inline-flex items-center text-sm font-medium text-slate-500 hover:text-primary-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4 mr-1" /> Back to Lessons
        </Link>
      </div>

      {errorMsg && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl text-xs font-medium">
          {errorMsg}
        </div>
      )}

      {/* Hero Card */}
      <Card className="p-8 border-slate-200 bg-white">
        <div className="flex flex-col md:flex-row md:items-start justify-between gap-6">
          <div className="space-y-3 flex-1">
            <div className="flex items-center gap-2 flex-wrap">
              <Badge variant="primary" className="text-xs uppercase font-bold tracking-wider">
                {lesson.subjectName || lesson.subjectCode || 'Subject'}
              </Badge>
              {lesson.gradeLevel && (
                <Badge variant="default" className="text-xs">
                  {lesson.gradeLevel}
                </Badge>
              )}
              {lesson.status && (
                <Badge variant="success" className="text-xs">
                  {lesson.status}
                </Badge>
              )}
            </div>

            <h1 className="text-3xl font-extrabold text-slate-800 tracking-tight">
              {lesson.title}
            </h1>

            <p className="text-sm text-slate-600 leading-relaxed max-w-2xl">
              {lesson.description ||
                'Engage with this deterministic simulation to test hypothesis, manipulate physical and chemical variables, and learn via interactive AI tutoring.'}
            </p>
          </div>

          <div className="flex-shrink-0">
            <Button
              size="lg"
              variant="primary"
              loading={launchMutation.isPending}
              onClick={() => launchMutation.mutate()}
              className="w-full md:w-auto shadow-md shadow-primary-500/20"
            >
              <Play className="w-5 h-5 mr-2 fill-current" />
              Launch Simulation
            </Button>
          </div>
        </div>
      </Card>

      {/* Objectives & Concepts */}
      {lesson.objectives && lesson.objectives.length > 0 && (
        <div className="grid grid-cols-1 gap-4">
          <Card className="p-6">
            <div className="flex items-center gap-2 mb-4">
              <Target className="w-5 h-5 text-primary-600" />
              <h3 className="font-bold text-slate-800 text-base">
                Curriculum Learning Objectives
              </h3>
            </div>
            <ul className="space-y-2.5">
              {lesson.objectives.map((obj, i) => (
                <li key={i} className="flex items-start gap-2.5 text-sm text-slate-600">
                  <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0 mt-0.5" />
                  <span>{typeof obj === 'string' ? obj : obj.description || obj.code}</span>
                </li>
              ))}
            </ul>
          </Card>
        </div>
      )}
    </div>
  );
}
