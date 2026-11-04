import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Calendar, Play, CheckCircle2, Clock, AlertCircle } from 'lucide-react';
import { listAssignments } from '../../api/assignments';
import Card from '../ui/Card';
import Button from '../ui/Button';
import Badge from '../ui/Badge';
import Spinner from '../ui/Spinner';

export default function AssignmentsWidget() {
  const { data: assignments = [], isLoading, error } = useQuery({
    queryKey: ['assignments'],
    queryFn: () => listAssignments(),
  });

  const assignmentList = Array.isArray(assignments)
    ? assignments
    : (assignments?.items ?? []);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-800">Assigned Tasks</h2>
          <p className="text-xs text-slate-500">
            Interactive curriculum simulations and exercises assigned to you
          </p>
        </div>
      </div>

      {isLoading ? (
        <Spinner />
      ) : error ? (
        <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
          Failed to load assignments: {error.message}
        </div>
      ) : assignmentList.length === 0 ? (
        <Card className="p-6 text-center bg-white border border-slate-100">
          <Clock className="w-8 h-8 text-slate-300 mx-auto mb-2" />
          <p className="text-sm font-medium text-slate-700">All caught up!</p>
          <p className="text-xs text-slate-400 mt-0.5">
            You do not have any pending assignments at this time.
          </p>
        </Card>
      ) : (
        <div className="space-y-3">
          {assignmentList.slice(0, 5).map((assignment) => {
            const isCompleted = assignment.completed || assignment.isCompleted;
            const dueFormatted = assignment.dueAt || assignment.dueDate
              ? new Date(assignment.dueAt || assignment.dueDate).toLocaleDateString('en-US', {
                  month: 'short',
                  day: 'numeric',
                })
              : null;

            return (
              <Card
                key={assignment.id}
                className="p-4 flex items-center justify-between gap-4 hover:border-primary-200 transition-all"
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1 flex-wrap">
                    <Badge variant={isCompleted ? 'success' : 'primary'}>
                      {isCompleted ? 'Completed' : 'Pending'}
                    </Badge>
                    {assignment.className && (
                      <span className="text-xs text-slate-400">
                        {assignment.className}
                      </span>
                    )}
                  </div>
                  <h4 className="font-semibold text-slate-800 text-sm truncate">
                    {assignment.lessonTitle || assignment.title || 'Interactive Lesson'}
                  </h4>
                  {assignment.instructions && (
                    <p className="text-xs text-slate-500 truncate mt-0.5">
                      {assignment.instructions}
                    </p>
                  )}
                  {dueFormatted && (
                    <div className="flex items-center gap-1.5 text-[11px] text-slate-400 mt-1">
                      <Calendar className="w-3 h-3" />
                      <span>Due: {dueFormatted}</span>
                    </div>
                  )}
                </div>

                <div className="flex items-center gap-2 flex-shrink-0">
                  <Link
                    to={
                      assignment.lessonId
                        ? `/lessons/${assignment.lessonId}`
                        : `/sessions`
                    }
                  >
                    <Button
                      size="sm"
                      variant={isCompleted ? 'outline' : 'primary'}
                      className="text-xs px-3 py-1.5"
                    >
                      <Play className="w-3.5 h-3.5 mr-1 fill-current" />
                      {isCompleted ? 'Review' : 'Launch'}
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
