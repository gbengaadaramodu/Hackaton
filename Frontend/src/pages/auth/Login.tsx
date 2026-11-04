import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Input } from '../../components/ui/Input';
import { School, GraduationCap, Play, Users, ArrowRight, Eye, EyeOff, BookOpen } from 'lucide-react';

export const Login = () => {
  const [email, setEmail] = useState('admin@simulearn.demo');
  const [password, setPassword] = useState('SimuLearn2026!');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      await login({ email, password });
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.detail || 'Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#fafafa] flex flex-col">
      <header className="purple-gradient-light text-white shadow-sm border-b border-primary-200/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16 md:h-20">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 md:w-12 md:h-12 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-lg border border-white/30">
                <School className="h-5 w-5 md:h-6 md:w-6 text-primary-500" />
              </div>
              <div>
                <h1 className="text-lg md:text-xl font-bold tracking-tight">Simu<span className="text-primary-300">Learn</span></h1>
                <p className="text-[10px] md:text-xs text-primary-100/90 font-medium">Interactive Learning Platform</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="hidden md:flex items-center gap-2 bg-white/20 rounded-2xl px-3 py-2 backdrop-blur-sm border border-white/30">
                <GraduationCap className="h-4 w-4 text-primary-100/90" />
                <input type="text" placeholder="Search..." className="bg-transparent text-sm text-white placeholder-primary-100/70 outline-none w-32 md:w-48" />
              </div>
              <button className="flex items-center gap-2 px-4 py-2.5 rounded-2xl bg-white/15 hover:bg-white/25 transition-colors text-sm font-medium backdrop-blur-sm border border-white/30">
                <Play className="h-4 w-4 fill-current" /><span className="hidden sm:inline">GO LIVE</span>
              </button>
              <button className="w-9 h-9 rounded-2xl bg-white/15 hover:bg-white/25 transition-colors flex items-center justify-center border border-white/30">
                <Users className="h-4 w-4 text-primary-100/90" /><span className="absolute top-1 right-1 w-2 h-2 bg-green-400 border-2 border-white rounded-full"></span>
              </button>
              <div className="w-9 h-9 rounded-full bg-white border-2 border-white/30 flex items-center justify-center shadow-md">
                <span className="text-primary-600 font-bold text-sm">{email.charAt(0).toUpperCase()}</span>
              </div>
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12">
        <div className="flex flex-col lg:flex-row gap-8 items-center lg:items-start min-h-[calc(100vh-160px)]">
          <div className="lg:w-1/2 flex-1 space-y-6">
            <div className="space-y-4">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary-50 text-primary-600 text-xs font-bold uppercase tracking-wider">
                <span className="w-1.5 h-1.5 rounded-full bg-primary-600 animate-pulse"></span>
                Welcome back
              </div>
              <h2 className="text-3xl md:text-4xl font-bold text-gray-900 tracking-tight">Sign in to SimuLearn</h2>
              <p className="text-gray-500 text-lg max-w-md">Generative Interactive Educational Simulators</p>
            </div>

            <div className="bg-white rounded-3xl border border-gray-100 p-8 md:p-10 shadow-sm card-hover">
              <form onSubmit={handleSubmit} className="space-y-5">
                {error && (
                  <div className="rounded-xl bg-red-50 border border-red-100 p-4 flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-red-100 flex items-center justify-center text-red-600 flex-shrink-0">
                      <span className="text-xs font-bold">!</span>
                    </div>
                    <p className="text-sm text-red-800 font-medium">{error}</p>
                  </div>
                )}

                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-gray-700">Email address</label>
                  <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@school.edu" className="w-full" />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="block text-sm font-semibold text-gray-700">Password</label>
                    <button type="button" onClick={() => setShowPassword(!showPassword)} className="text-sm text-primary-600 font-medium hover:text-primary-700 transition-colors">{showPassword ? 'Hide' : 'Show'}</button>
                  </div>
                  <div className="relative">
                    <Input type={showPassword ? 'text' : 'password'} value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" className="w-full pr-12" />
                    <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">{showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}</button>
                  </div>
                </div>

                <button type="submit" className="w-full bg-primary-600 hover:bg-primary-700 text-white rounded-2xl font-semibold py-3 transition-colors shadow-lg shadow-primary-200 flex items-center justify-center gap-2" disabled={isLoading}>
                  {isLoading ? <span className="flex items-center gap-2"><svg className="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>Signing in...</span> : <span>Sign in</span>}
                  <ArrowRight className="h-4 w-4" />
                </button>
              </form>

              <div className="mt-4 text-right">
                <Link to="/forgot-password" className="text-sm text-primary-600 hover:text-primary-700 font-medium">Forgot password?</Link>
              </div>

              <div className="mt-6 text-center">
                <p className="text-sm text-gray-600">Don't have an account?{' '}<Link to="/register" className="font-semibold text-primary-600 hover:text-primary-700">Sign up</Link></p>
              </div>

              <div className="mt-8 pt-6 border-t border-gray-100 space-y-4">
                <div className="flex items-center gap-2 text-xs font-semibold text-gray-500 uppercase tracking-wider"><BookOpen className="h-3.5 w-3.5 text-primary-400" />Quick demo access</div>
                <div className="grid grid-cols-3 gap-2">
                  {[{ role: 'Student', email: 'student@simulearn.demo', password: 'SimuLearn2026!' }, { role: 'Teacher', email: 'teacher@simulearn.demo', password: 'SimuLearn2026!' }, { role: 'Admin', email: 'admin@simulearn.demo', password: 'SimuLearn2026!' }].map((demo) => (
                    <button key={demo.role} onClick={() => { setEmail(demo.email); setPassword(demo.password); }} className="px-3 py-2.5 rounded-xl bg-gray-50 hover:bg-primary-50 hover:border-primary-200 border border-transparent text-left transition-all group">
                      <div className="text-xs font-bold text-gray-900 group-hover:text-primary-600 transition-colors">{demo.role}</div>
                      <div className="text-[10px] text-gray-400 group-hover:text-gray-600 transition-colors">{demo.email}</div>
                    </button>
                  ))}
                </div>
              </div>

              <p className="text-[10px] text-gray-400 mt-6 text-center">Protected by security · Never share your credentials</p>
            </div>
          </div>

          <div className="hidden lg:block lg:w-1/2 flex items-center justify-center">
            <div className="relative w-full max-w-md h-[400px]">
              <div className="absolute top-0 right-0 w-64 h-64 bg-primary-100 rounded-full blur-3xl opacity-50"></div>
              <div className="absolute bottom-0 left-0 w-72 h-72 bg-primary-200 rounded-full blur-3xl opacity-50"></div>
              <div className="relative z-10 w-full h-full flex items-center justify-center p-8">
                <div className="w-48 h-64 bg-gradient-to-br from-white to-gray-50 rounded-3xl border border-gray-200 shadow-2xl overflow-hidden relative">
                  <div className="absolute top-1/4 right-1/4 w-24 h-24 bg-white/90 rounded-full blur-xl animate-pulse"></div>
                  <div className="absolute bottom-1/4 left-1/4 w-32 h-32 bg-white/80 rounded-full blur-lg"></div>
                  <div className="relative w-full h-full flex items-end gap-2 p-4">
                    <div className="relative w-20 h-28">
                      <div className="absolute bottom-0 w-14 h-14 bg-orange-200 rounded-full border-3 border-orange-100 overflow-hidden"></div>
                      <div className="absolute bottom-3 left-1 w-10 h-7 bg-orange-100 rounded-t-full border border-orange-100"></div>
                      <div className="absolute top-1/3 left-2 w-1 h-1 bg-gray-700 rounded-full"></div>
                      <div className="absolute top-1/3 right-2 w-1 h-1 bg-gray-700 rounded-full"></div>
                      <div className="absolute bottom-1/3 left-3 w-3 h-1 bg-orange-300 rounded-full"></div>
                      <div className="absolute top-0 left-1 w-12 h-5 bg-gray-800 rounded-t-lg border-b-2 border-gray-700 rotate-[-10deg]"></div>
                      <div className="absolute top-3 left-3 w-7 h-7 bg-gray-800 rounded-full"></div>
                      <div className="absolute bottom-0 left-1 w-12 h-12 bg-brown-200 rounded-full border-3 border-brown-100"></div>
                      <div className="absolute top-1/2 right-0 w-10 h-14 bg-blue-200 rounded-r-full border-l-3 border-blue-100"></div>
                    </div>
                  </div>
                  <div className="absolute top-1 left-1 w-2 h-2 bg-red-400 rounded-full"></div>
                  <div className="absolute top-1/3 left-1 w-1.5 h-1.5 bg-blue-400 rounded-full"></div>
                  <div className="absolute bottom-1 right-1 w-3 h-3 bg-orange-400 rounded-full"></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
