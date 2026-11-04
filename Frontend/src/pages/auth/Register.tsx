import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Brain, Mail, Lock, User, GraduationCap, ArrowRight } from 'lucide-react';

export const Register = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    displayName: '',
    role: 'Student',
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      await register(formData);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.detail || 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  return (
    <div className="min-h-screen bg-[#fafafa] flex flex-col">
      <header className="purple-gradient-light text-white shadow-sm border-b border-primary-200/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16 md:h-20">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 md:w-12 md:h-12 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-lg border border-white/30">
                <GraduationCap className="h-5 w-5 md:h-6 md:w-6 text-primary-500" />
              </div>
              <div>
                <h1 className="text-lg md:text-xl font-bold tracking-tight">Simu<span className="text-primary-300">Learn</span></h1>
                <p className="text-[10px] md:text-xs text-primary-100/90 font-medium">Interactive Learning Platform</p>
              </div>
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1 max-w-md mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 flex items-center justify-center">
        <div className="w-full space-y-8">
          <div className="text-center">
            <div className="flex justify-center"><Brain className="h-12 w-12 text-primary-600" /></div>
            <h2 className="mt-6 text-3xl font-extrabold text-gray-900 tracking-tight">Create your account</h2>
            <p className="mt-2 text-sm text-gray-600">Join SimuLearn today</p>
          </div>

          <div className="bg-white rounded-3xl border border-gray-100 p-8 md:p-10 shadow-sm card-hover">
            <div className="space-y-6">
              <div className="flex items-center gap-3">
                <GraduationCap className="h-5 w-5 text-primary-600" />
                <h3 className="text-lg font-bold text-gray-900">Get started</h3>
              </div>
              <p className="text-sm text-gray-500">Create a new account to begin learning</p>

              {error && (
                <div className="rounded-xl bg-red-50 border border-red-100 p-4 flex items-start gap-3">
                  <div className="w-6 h-6 rounded-full bg-red-100 flex items-center justify-center text-red-600 flex-shrink-0">
                    <span className="text-xs font-bold">!</span>
                  </div>
                  <p className="text-sm text-red-800 font-medium">{error}</p>
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-5">
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <User className="h-4 w-4 text-gray-400" />
                    <label htmlFor="displayName" className="text-sm font-semibold text-gray-700">Full Name</label>
                  </div>
                  <Input id="displayName" name="displayName" type="text" value={formData.displayName} onChange={handleChange} required placeholder="John Doe" />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Mail className="h-4 w-4 text-gray-400" />
                    <label htmlFor="email" className="text-sm font-semibold text-gray-700">Email address</label>
                  </div>
                  <Input id="email" name="email" type="email" value={formData.email} onChange={handleChange} required placeholder="you@example.com" />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Lock className="h-4 w-4 text-gray-400" />
                    <label htmlFor="password" className="text-sm font-semibold text-gray-700">Password</label>
                  </div>
                  <Input id="password" name="password" type="password" value={formData.password} onChange={handleChange} required placeholder="Create a password" />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <GraduationCap className="h-4 w-4 text-gray-400" />
                    <label htmlFor="role" className="text-sm font-semibold text-gray-700">Role</label>
                  </div>
                  <select id="role" name="role" value={formData.role} onChange={handleChange} className="flex h-10 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-400 transition-all">
                    <option value="Student">Student</option>
                    <option value="Teacher">Teacher</option>
                  </select>
                </div>

                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading ? 'Creating account...' : 'Create account'}
                  <ArrowRight className="h-4 w-4 ml-2" />
                </Button>
              </form>

              <div className="mt-6 text-center">
                <p className="text-sm text-gray-600">Already have an account?{' '}<Link to="/login" className="font-semibold text-primary-600 hover:text-primary-700">Sign in</Link></p>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
