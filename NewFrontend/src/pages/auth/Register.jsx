import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { GraduationCap, UserPlus } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import Button from '../../components/ui/Button';

export default function Register() {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    displayName: '',
    role: 'Student',
    dateOfBirth: '',
    guardianEmail: '',
    guardianConsent: false,
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { register, login } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    const { name, type, value, checked } = e.target;
    setFormData({ ...formData, [name]: type === 'checkbox' ? checked : value });
  };

  // Mirrors the API rule: learners under 13 need recorded guardian consent.
  const ageFromDateOfBirth = (value) => {
    if (!value) return null;
    const birth = new Date(value);
    if (Number.isNaN(birth.getTime())) return null;
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const monthDelta = today.getMonth() - birth.getMonth();
    if (monthDelta < 0 || (monthDelta === 0 && today.getDate() < birth.getDate())) {
      age -= 1;
    }
    return age;
  };

  const age = ageFromDateOfBirth(formData.dateOfBirth);
  const needsGuardianConsent = age !== null && age < 13;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await register({
        email: formData.email,
        password: formData.password,
        displayName: formData.displayName,
        role: formData.role,
        dateOfBirth: formData.dateOfBirth || undefined,
        guardianEmail: needsGuardianConsent
          ? formData.guardianEmail
          : undefined,
        guardianConsent: needsGuardianConsent
          ? formData.guardianConsent
          : false,
      });
      // Auto login
      await login({ email: formData.email, password: formData.password });
      navigate('/dashboard');
    } catch (err) {
      setError(
        err.response?.data?.detail ||
          err.response?.data?.title ||
          'Failed to create account. Please check details and try again.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
        <div className="w-14 h-14 bg-gradient-to-tr from-primary-700 to-primary-500 rounded-2xl flex items-center justify-center mx-auto shadow-lg shadow-primary-500/30 text-white mb-4">
          <GraduationCap className="w-8 h-8" />
        </div>
        <h2 className="text-2xl font-bold tracking-tight text-slate-800">
          Create an Account
        </h2>
        <p className="mt-1 text-sm text-slate-500">
          Join the SimuLearn Interactive Simulation Platform
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-6 shadow-sm border border-slate-200/80 rounded-3xl sm:px-10">
          {error && (
            <div className="mb-4 p-3.5 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl text-xs font-medium">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Full Display Name
              </label>
              <input
                type="text"
                name="displayName"
                value={formData.displayName}
                onChange={handleChange}
                required
                placeholder="e.g. Maya Lin"
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Email Address
              </label>
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                required
                placeholder="maya.lin@school.edu"
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Password
              </label>
              <input
                type="password"
                name="password"
                value={formData.password}
                onChange={handleChange}
                required
                minLength={10}
                pattern="(?=.*[A-Za-z])(?=.*\d).{10,}"
                title="At least 10 characters, including a letter and a number"
                placeholder="At least 10 characters, letters and numbers"
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Account Role
              </label>
              <select
                name="role"
                value={formData.role}
                onChange={handleChange}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 bg-white focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="Student">Student Learner</option>
                <option value="Teacher">Instructor / Teacher</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Date of Birth (optional)
              </label>
              <input
                type="date"
                name="dateOfBirth"
                value={formData.dateOfBirth}
                onChange={handleChange}
                max={new Date().toISOString().split('T')[0]}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
              <p className="text-[11px] text-slate-400 mt-1">
                Learners under 13 need a guardian to approve the account.
              </p>
            </div>

            {needsGuardianConsent && (
              <div className="p-3.5 bg-amber-50 border border-amber-200 rounded-xl space-y-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Guardian Email
                  </label>
                  <input
                    type="email"
                    name="guardianEmail"
                    value={formData.guardianEmail}
                    onChange={handleChange}
                    required
                    placeholder="guardian@example.com"
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 bg-white focus:outline-none focus:ring-2 focus:ring-primary-500"
                  />
                </div>
                <label className="flex items-start gap-2 text-xs text-slate-700">
                  <input
                    type="checkbox"
                    name="guardianConsent"
                    checked={formData.guardianConsent}
                    onChange={handleChange}
                    required
                    className="mt-0.5 accent-primary-600"
                  />
                  <span>
                    My guardian consents to this account being created for a
                    learner under 13.
                  </span>
                </label>
              </div>
            )}

            <Button
              type="submit"
              loading={loading}
              className="w-full mt-2"
              size="lg"
            >
              <UserPlus className="w-4 h-4 mr-1.5" />
              Register
            </Button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-xs text-slate-500">
              Already have an account?{' '}
              <Link
                to="/login"
                className="font-semibold text-primary-600 hover:text-primary-700"
              >
                Sign In
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
