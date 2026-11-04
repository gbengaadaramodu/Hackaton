import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Users, Plus, KeyRound, ArrowRight, BookOpen } from 'lucide-react';
import { listClasses, joinClass, createClass } from '../../api/classes';
import { useAuth } from '../../contexts/AuthContext';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Modal from '../../components/ui/Modal';
import Spinner from '../../components/ui/Spinner';

export default function ClassList() {
  const { isTeacher, isStudent } = useAuth();
  const queryClient = useQueryClient();
  const [isJoinOpen, setIsJoinOpen] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [joinCode, setJoinCode] = useState('');
  const [className, setClassName] = useState('');
  const [formError, setFormError] = useState('');

  const { data: classes = [], isLoading, error } = useQuery({
    queryKey: ['classes'],
    queryFn: listClasses,
  });

  const classList = Array.isArray(classes) ? classes : (classes?.items ?? []);

  const joinMutation = useMutation({
    mutationFn: (code) => joinClass({ joinCode: code }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      setIsJoinOpen(false);
      setJoinCode('');
      setFormError('');
    },
    onError: (err) => {
      setFormError(err.response?.data?.detail || 'Failed to join class. Verify code.');
    },
  });

  const createMutation = useMutation({
    mutationFn: (name) => createClass({ name }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      setIsCreateOpen(false);
      setClassName('');
      setFormError('');
    },
    onError: (err) => {
      setFormError(err.response?.data?.detail || 'Failed to create class.');
    },
  });

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">
            {isTeacher ? 'Class Management' : 'My Enrolled Classes'}
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            {isTeacher
              ? 'Create student cohorts, generate join codes, and inspect class progress.'
              : 'View your enrolled classes, classmates, and active lesson assignments.'}
          </p>
        </div>

        <div className="flex items-center gap-3">
          {isStudent && (
            <Button
              variant="primary"
              onClick={() => {
                setFormError('');
                setIsJoinOpen(true);
              }}
            >
              <KeyRound className="w-4 h-4 mr-2" />
              Join with Code
            </Button>
          )}
          {isTeacher && (
            <Button
              variant="primary"
              onClick={() => {
                setFormError('');
                setIsCreateOpen(true);
              }}
            >
              <Plus className="w-4 h-4 mr-2" />
              Create Class
            </Button>
          )}
        </div>
      </div>

      {isLoading ? (
        <Spinner />
      ) : error ? (
        <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
          Failed to load classes: {error.message}
        </div>
      ) : classList.length === 0 ? (
        <Card className="p-12 text-center bg-white border border-slate-200">
          <BookOpen className="w-12 h-12 text-primary-300 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-slate-800">No classes found</h3>
          <p className="text-sm text-slate-500 mt-1 max-w-md mx-auto">
            {isStudent
              ? 'You are not enrolled in any classes. Join a class using your teacher’s code.'
              : 'You have not created any classes yet. Click "Create Class" above to get started.'}
          </p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {classList.map((cls) => (
            <Card
              key={cls.id}
              className="p-6 flex flex-col justify-between hover:border-primary-300 transition-all hover:shadow-md"
            >
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="w-10 h-10 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center font-bold text-base">
                    {cls.name.charAt(0).toUpperCase()}
                  </div>
                  {cls.joinCode && (
                    <Badge variant="primary" className="font-mono text-xs">
                      {cls.joinCode}
                    </Badge>
                  )}
                </div>

                <h3 className="text-lg font-bold text-slate-800 mb-1">
                  {cls.name}
                </h3>
                <p className="text-xs text-slate-500 mb-4">
                  Instructor: <span className="font-medium text-slate-700">{cls.teacherName || 'Assigned Instructor'}</span>
                </p>
              </div>

              <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
                <div className="flex items-center gap-1.5 text-xs text-slate-500">
                  <Users className="w-4 h-4 text-slate-400" />
                  <span>{cls.memberCount ?? 0} members</span>
                </div>
                <Link to={`/classes/${cls.id}`}>
                  <Button size="sm" variant="ghost" className="text-primary-600 hover:text-primary-700">
                    Open <ArrowRight className="w-4 h-4 ml-1" />
                  </Button>
                </Link>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Join Class Modal */}
      <Modal
        isOpen={isJoinOpen}
        onClose={() => setIsJoinOpen(false)}
        title="Join a Classroom"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!joinCode.trim()) return;
            joinMutation.mutate(joinCode.trim());
          }}
          className="space-y-4"
        >
          {formError && (
            <div className="p-3 bg-rose-50 text-rose-700 rounded-xl text-xs border border-rose-200">
              {formError}
            </div>
          )}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Classroom Code
            </label>
            <input
              type="text"
              value={joinCode}
              onChange={(e) => setJoinCode(e.target.value)}
              placeholder="e.g. CHEM-301"
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm uppercase tracking-wider focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsJoinOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" loading={joinMutation.isPending}>
              Join Classroom
            </Button>
          </div>
        </form>
      </Modal>

      {/* Create Class Modal */}
      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Create Classroom"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!className.trim()) return;
            createMutation.mutate(className.trim());
          }}
          className="space-y-4"
        >
          {formError && (
            <div className="p-3 bg-rose-50 text-rose-700 rounded-xl text-xs border border-rose-200">
              {formError}
            </div>
          )}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Class Title
            </label>
            <input
              type="text"
              value={className}
              onChange={(e) => setClassName(e.target.value)}
              placeholder="e.g. Physics Grade 10 - Block A"
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsCreateOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" loading={createMutation.isPending}>
              Create
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
