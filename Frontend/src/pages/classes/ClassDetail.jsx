import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Users,
  KeyRound,
  ArrowLeft,
  Calendar,
  BookOpen,
  Plus,
  Play,
  Mail,
} from 'lucide-react';
import { getClass, getClassMembers } from '../../api/classes';
import { listAssignments, createAssignment } from '../../api/assignments';
import { listLessons } from '../../api/lessons';
import { useAuth } from '../../contexts/AuthContext';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Modal from '../../components/ui/Modal';
import Spinner from '../../components/ui/Spinner';

export default function ClassDetail() {
  const { id } = useParams();
  const { isTeacher } = useAuth();
  const queryClient = useQueryClient();

  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [selectedLessonId, setSelectedLessonId] = useState('');
  const [instructions, setInstructions] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [assignError, setAssignError] = useState('');

  // Class Info
  const {
    data: classInfo,
    isLoading: loadingClass,
    error: classError,
  } = useQuery({
    queryKey: ['class', id],
    queryFn: () => getClass(id),
    enabled: !!id,
  });

  // Members
  const { data: members = [], isLoading: loadingMembers } = useQuery({
    queryKey: ['class-members', id],
    queryFn: () => getClassMembers(id),
    enabled: !!id,
  });

  // Assignments for this class
  const { data: assignments = [], isLoading: loadingAssignments } = useQuery({
    queryKey: ['class-assignments', id],
    queryFn: () => listAssignments(id),
    enabled: !!id,
  });

  // Available lessons to assign (for teachers)
  const { data: lessons = [] } = useQuery({
    queryKey: ['lessons'],
    queryFn: () => listLessons(),
    enabled: isTeacher,
  });

  const memberList = Array.isArray(members) ? members : (members?.items ?? []);
  const assignmentList = Array.isArray(assignments) ? assignments : (assignments?.items ?? []);
  const lessonList = Array.isArray(lessons) ? lessons : (lessons?.items ?? []);

  const assignMutation = useMutation({
    mutationFn: (req) => createAssignment(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['class-assignments', id] });
      setIsAssignModalOpen(false);
      setSelectedLessonId('');
      setInstructions('');
      setDueDate('');
      setAssignError('');
    },
    onError: (err) => {
      setAssignError(
        err.response?.data?.detail || 'Failed to create assignment for this class.'
      );
    },
  });

  if (loadingClass) return <Spinner />;
  if (classError || !classInfo) {
    return (
      <div className="p-6 bg-rose-50 text-rose-700 rounded-xl max-w-2xl mx-auto">
        Class not found or access denied.
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Back button */}
      <div>
        <Link
          to="/classes"
          className="inline-flex items-center text-sm font-medium text-slate-500 hover:text-primary-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4 mr-1" /> Back to Classes
        </Link>
      </div>

      {/* Class Header Banner */}
      <Card className="p-6 md:p-8 border-slate-200">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-3 mb-2">
              <span className="w-12 h-12 rounded-2xl bg-primary-100 text-primary-700 flex items-center justify-center font-bold text-xl">
                {classInfo.name.charAt(0).toUpperCase()}
              </span>
              <div>
                <h1 className="text-2xl font-bold text-slate-800">
                  {classInfo.name}
                </h1>
                <p className="text-xs text-slate-500">
                  Instructor: <span className="font-medium text-slate-700">{classInfo.teacherName}</span>
                </p>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3 flex-wrap">
            {classInfo.joinCode && (
              <div className="bg-primary-50 px-4 py-2 rounded-xl border border-primary-100 flex items-center gap-2">
                <KeyRound className="w-4 h-4 text-primary-600" />
                <div>
                  <span className="text-[10px] text-primary-500 uppercase font-bold block leading-none">
                    Join Code
                  </span>
                  <span className="font-mono text-sm font-bold text-primary-800">
                    {classInfo.joinCode}
                  </span>
                </div>
              </div>
            )}

            {isTeacher && (
              <Button
                variant="primary"
                onClick={() => {
                  setAssignError('');
                  setIsAssignModalOpen(true);
                }}
              >
                <Plus className="w-4 h-4 mr-1.5" />
                Assign Lesson
              </Button>
            )}
          </div>
        </div>
      </Card>

      {/* Grid: Assignments & Members */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Class Assignments */}
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-bold text-slate-800">Assigned Curriculum</h2>
            <span className="text-xs text-slate-400">{assignmentList.length} tasks</span>
          </div>

          {loadingAssignments ? (
            <Spinner />
          ) : assignmentList.length === 0 ? (
            <Card className="p-8 text-center bg-white border border-slate-100">
              <BookOpen className="w-8 h-8 text-slate-300 mx-auto mb-2" />
              <p className="text-sm font-medium text-slate-700">No assignments posted yet</p>
              <p className="text-xs text-slate-400 mt-0.5">
                {isTeacher
                  ? 'Assign an interactive simulation lesson to this classroom.'
                  : 'Check back later for assignments from your instructor.'}
              </p>
            </Card>
          ) : (
            <div className="space-y-3">
              {assignmentList.map((assignment) => (
                <Card
                  key={assignment.id}
                  className="p-4 flex items-center justify-between gap-4 hover:border-primary-200"
                >
                  <div className="min-w-0">
                    <h4 className="font-semibold text-slate-800 text-sm">
                      {assignment.lessonTitle || 'Interactive Lesson'}
                    </h4>
                    {assignment.instructions && (
                      <p className="text-xs text-slate-500 mt-0.5 truncate">
                        {assignment.instructions}
                      </p>
                    )}
                    {assignment.dueAt && (
                      <div className="flex items-center gap-1.5 text-xs text-slate-400 mt-1">
                        <Calendar className="w-3.5 h-3.5" />
                        <span>
                          Due:{' '}
                          {new Date(assignment.dueAt).toLocaleDateString('en-US', {
                            month: 'short',
                            day: 'numeric',
                          })}
                        </span>
                      </div>
                    )}
                  </div>

                  <Link to={`/lessons/${assignment.lessonId}`}>
                    <Button size="sm" variant="primary">
                      <Play className="w-3.5 h-3.5 mr-1 fill-current" />
                      Start
                    </Button>
                  </Link>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* Right Col: Class Members */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-bold text-slate-800">Class Roster</h2>
            <span className="text-xs text-slate-400">{memberList.length} enrolled</span>
          </div>

          <Card className="p-4 divide-y divide-slate-100">
            {loadingMembers ? (
              <Spinner size="sm" />
            ) : memberList.length === 0 ? (
              <p className="text-xs text-slate-400 py-4 text-center">
                No students enrolled yet.
              </p>
            ) : (
              memberList.map((member) => (
                <div key={member.studentId || member.userId || member.email} className="py-2.5 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-full bg-primary-50 text-primary-700 flex items-center justify-center font-semibold text-xs flex-shrink-0">
                    {(member.displayName || 'U').charAt(0).toUpperCase()}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-xs font-semibold text-slate-800 truncate">
                      {member.displayName}
                    </p>
                    <p className="text-[11px] text-slate-400 truncate">
                      {member.email}
                    </p>
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      </div>

      {/* Assign Lesson Modal */}
      <Modal
        isOpen={isAssignModalOpen}
        onClose={() => setIsAssignModalOpen(false)}
        title="Assign Simulation Lesson"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!selectedLessonId) return;
            assignMutation.mutate({
              classId: id,
              lessonId: selectedLessonId,
              instructions: instructions.trim() || undefined,
              dueAt: dueDate ? new Date(dueDate).toISOString() : undefined,
            });
          }}
          className="space-y-4"
        >
          {assignError && (
            <div className="p-3 bg-rose-50 text-rose-700 rounded-xl text-xs border border-rose-200">
              {assignError}
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Select Lesson
            </label>
            <select
              value={selectedLessonId}
              onChange={(e) => setSelectedLessonId(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 bg-white focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            >
              <option value="">-- Choose an approved lesson --</option>
              {lessonList.map((lesson) => (
                <option key={lesson.id} value={lesson.id}>
                  {lesson.title} ({lesson.subjectCode || 'General'})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Due Date (Optional)
            </label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Instructions / Objectives
            </label>
            <textarea
              value={instructions}
              onChange={(e) => setInstructions(e.target.value)}
              rows={3}
              placeholder="e.g. Complete the projectile velocity tests and observe trajectory changes."
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsAssignModalOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" loading={assignMutation.isPending}>
              Assign to Class
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
