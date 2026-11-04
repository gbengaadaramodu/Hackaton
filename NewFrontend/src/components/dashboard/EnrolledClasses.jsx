import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Users, Plus, ArrowRight, BookOpen, KeyRound } from 'lucide-react';
import { listClasses, joinClass, createClass } from '../../api/classes';
import { useAuth } from '../../contexts/AuthContext';
import Card from '../ui/Card';
import Button from '../ui/Button';
import Modal from '../ui/Modal';
import Spinner from '../ui/Spinner';
import Badge from '../ui/Badge';

export default function EnrolledClasses() {
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
      setFormError(err.response?.data?.detail || 'Failed to join class. Verify the code.');
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
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-800">
            {isTeacher ? 'My Teaching Classes' : 'Enrolled Classes'}
          </h2>
          <p className="text-xs text-slate-500">
            {isTeacher
              ? 'Classes you manage and assign simulations to'
              : 'Courses and cohorts you actively belong to'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {isStudent && (
            <Button
              size="sm"
              variant="secondary"
              onClick={() => {
                setFormError('');
                setIsJoinOpen(true);
              }}
            >
              <KeyRound className="w-4 h-4" />
              Join Class
            </Button>
          )}
          {isTeacher && (
            <Button
              size="sm"
              variant="primary"
              onClick={() => {
                setFormError('');
                setIsCreateOpen(true);
              }}
            >
              <Plus className="w-4 h-4" />
              New Class
            </Button>
          )}
          <Link to="/classes">
            <Button size="sm" variant="outline">
              View All
            </Button>
          </Link>
        </div>
      </div>

      {isLoading ? (
        <Spinner />
      ) : error ? (
        <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
          Failed to load classes: {error.message}
        </div>
      ) : classList.length === 0 ? (
        <Card className="p-8 text-center bg-slate-50 border-dashed border-2 border-slate-200">
          <BookOpen className="w-10 h-10 text-primary-400 mx-auto mb-3" />
          <h3 className="font-semibold text-slate-700">No classes enrolled yet</h3>
          <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
            {isStudent
              ? 'Ask your instructor for a class join code to see curriculum lessons and track progress.'
              : 'Create your first classroom to begin enrolling students and distributing lessons.'}
          </p>
          {isStudent ? (
            <Button
              size="sm"
              className="mt-4"
              onClick={() => setIsJoinOpen(true)}
            >
              Enter Join Code
            </Button>
          ) : (
            <Button
              size="sm"
              className="mt-4"
              onClick={() => setIsCreateOpen(true)}
            >
              Create Classroom
            </Button>
          )}
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {classList.slice(0, 4).map((c) => (
            <Card key={c.id} className="p-5 flex flex-col justify-between hover:border-primary-200 group">
              <div>
                <div className="flex items-start justify-between gap-2 mb-2">
                  <span className="w-9 h-9 rounded-xl bg-primary-50 text-primary-600 flex items-center justify-center font-bold text-sm">
                    {c.name.charAt(0).toUpperCase()}
                  </span>
                  {c.joinCode && (
                    <Badge variant="primary" className="font-mono text-[11px]">
                      Code: {c.joinCode}
                    </Badge>
                  )}
                </div>
                <h3 className="font-semibold text-slate-800 text-base group-hover:text-primary-600 transition-colors">
                  {c.name}
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Instructor: <span className="font-medium text-slate-700">{c.teacherName || 'Assigned Staff'}</span>
                </p>
              </div>

              <div className="pt-4 mt-4 border-t border-slate-100 flex items-center justify-between">
                <div className="flex items-center gap-1.5 text-xs text-slate-500">
                  <Users className="w-3.5 h-3.5 text-slate-400" />
                  <span>{c.memberCount ?? 0} students</span>
                </div>
                <Link to={`/classes/${c.id}`}>
                  <Button size="sm" variant="ghost" className="text-primary-600 hover:text-primary-700 text-xs px-2 py-1">
                    View <ArrowRight className="w-3.5 h-3.5 ml-1" />
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
              Classroom Join Code
            </label>
            <input
              type="text"
              value={joinCode}
              onChange={(e) => setJoinCode(e.target.value)}
              placeholder="e.g. BIO-101-X9"
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm uppercase tracking-wider focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
            <p className="text-[11px] text-slate-400 mt-1">
              Obtain the alphanumeric join code from your teacher.
            </p>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsJoinOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              loading={joinMutation.isPending}
            >
              Join Classroom
            </Button>
          </div>
        </form>
      </Modal>

      {/* Create Class Modal */}
      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Create a New Classroom"
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
              Class Name
            </label>
            <input
              type="text"
              value={className}
              onChange={(e) => setClassName(e.target.value)}
              placeholder="e.g. Physics Grade 11 - Room 204"
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
            <Button
              type="submit"
              loading={createMutation.isPending}
            >
              Create Class
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
