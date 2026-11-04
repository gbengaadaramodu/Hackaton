import { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { Coins, Wallet, BarChart3, Play, Laptop, PieChart, ChevronRight, Calendar } from 'lucide-react';

export const Dashboard = () => {
  const { user } = useAuth();
  const [isLoading, setIsLoading] = useState(false);

  const instructors = [
    { name: 'Sarah Wilson', role: 'Computer Science', image: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&h=150&fit=crop&crop=face' },
    { name: 'Michael Chen', role: 'Database Systems', image: 'https://images.unsplash.com/photo-1507003211169-0a1272e8e0ba?w=150&h=150&fit=crop&crop=face' },
    { name: 'Elena Rodriguez', role: 'Object Oriented Programming', image: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face' },
  ];

  const enrolledCourses = [
    {
      title: 'Object oriented programming',
      subtitle: 'Computer Science • 40 hours',
      color: 'from-primary-100 to-primary-200',
      icon: Laptop,
    },
    {
      title: 'Fundamentals of database systems',
      subtitle: 'Information Systems • 45 hours',
      color: 'from-primary-50 to-primary-100',
      icon: PieChart,
    },
  ];

  useEffect(() => {
    setIsLoading(true);
    setTimeout(() => setIsLoading(false), 300);
  }, []);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px]">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
        <p className="mt-4 text-sm text-gray-500 font-medium">Loading dashboard...</p>
      </div>
    );
  }

  const displayName = user?.displayName || 'John';

  return (
    <div className="min-h-screen bg-[#fafafa] fade-in">
      <div className="rounded-3xl purple-gradient-light overflow-hidden shadow-xl mb-8 relative border border-primary-200/50">
        <div className="absolute top-0 left-0 w-full h-full opacity-10 pointer-events-none">
          <div className="w-full h-full" style={{ backgroundImage: 'radial-gradient(circle at 50% 50%, rgba(255,255,255,0.3) 0%, transparent 70%)' }} />
        </div>
        <div className="relative p-8 md:p-12 flex flex-col md:flex-row gap-8 items-start md:items-center">
          <div className="flex-1">
            <div className="flex items-center gap-2 text-primary-100/90 text-xs font-semibold mb-2 uppercase tracking-wider">
              September 4, 2025
            </div>
            <h1 className="text-white text-3xl md:text-4xl font-bold mb-2 tracking-tight leading-tight">
              Welcome back, {displayName}!
            </h1>
            <p className="text-primary-100/90 text-lg font-medium max-w-md">
              Always stay updated in your student portal
            </p>
          </div>
          <div className="w-full md:w-96 flex items-center justify-center relative">
            <div className="relative w-full h-64 md:h-72 rounded-2xl overflow-hidden shadow-2xl border border-white/30">
              <div className="absolute top-1/4 right-1/4 w-32 h-32 bg-white/90 rounded-full blur-2xl animate-pulse"></div>
              <div className="absolute bottom-1/4 left-1/4 w-48 h-48 bg-white/80 rounded-full blur-xl"></div>
              <div className="relative z-10 w-full h-full flex items-center justify-center p-4">
                <div className="w-full h-full flex items-end gap-2">
                  <div className="relative w-24 h-32">
                    <div className="absolute bottom-0 w-16 h-16 bg-orange-200 rounded-full border-4 border-orange-100 overflow-hidden"></div>
                    <div className="absolute bottom-4 left-1 w-12 h-8 bg-orange-100 rounded-t-full border-2 border-orange-100"></div>
                    <div className="absolute top-1/3 left-2 w-1 h-1 bg-gray-700 rounded-full"></div>
                    <div className="absolute top-1/3 right-2 w-1 h-1 bg-gray-700 rounded-full"></div>
                    <div className="absolute bottom-1/3 left-4 w-4 h-1 bg-orange-300 rounded-full"></div>
                    <div className="absolute top-0 left-1 w-14 h-6 bg-gray-800 rounded-t-lg border-b-2 border-gray-700 rotate-[-10deg]"></div>
                    <div className="absolute top-4 left-4 w-8 h-8 bg-gray-800 rounded-full"></div>
                    <div className="absolute bottom-0 left-1 w-14 h-14 bg-brown-200 rounded-full border-4 border-brown-100"></div>
                    <div className="absolute top-1/2 right-0 w-12 h-16 bg-blue-200 rounded-r-full border-l-4 border-blue-100"></div>
                  </div>
                </div>
              </div>
              <div className="absolute top-2 left-2 w-3 h-3 bg-red-400 rounded-full"></div>
              <div className="absolute top-1/3 left-1 w-2 h-2 bg-blue-400 rounded-full"></div>
              <div className="absolute bottom-2 right-2 w-4 h-4 bg-orange-400 rounded-full"></div>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <div className="lg:col-span-8 space-y-6">
          <section className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold text-gray-900">Finance</h2>
              <a href="#" className="text-primary-600 text-xs font-bold hover:underline">View all</a>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="rounded-2xl bg-white border border-gray-100 p-5 shadow-sm card-hover">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-10 h-10 rounded-xl bg-gray-100 flex items-center justify-center text-gray-600">
                    <Coins className="h-5 w-5" />
                  </div>
                  <div className="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-600">
                    <span className="text-[10px] font-bold">$</span>
                  </div>
                </div>
                <div className="text-2xl font-bold text-gray-900 mb-1">$ 10,000</div>
                <div className="text-sm text-gray-500 font-medium">Total Payable</div>
              </div>
              <div className="rounded-2xl bg-white border-2 border-primary-200 p-5 shadow-sm card-hover relative overflow-hidden">
                <div className="flex items-center justify-between mb-4 relative z-10">
                  <div className="w-10 h-10 rounded-xl bg-white flex items-center justify-center text-primary-600 shadow-sm">
                    <Wallet className="h-5 w-5" />
                  </div>
                  <div className="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-600">
                    <span className="text-[10px] font-bold">$</span>
                  </div>
                </div>
                <div className="text-2xl font-bold text-gray-900 mb-1 relative z-10">$ 5,000</div>
                <div className="text-sm text-gray-500 font-medium relative z-10">Total Paid</div>
              </div>
              <div className="rounded-2xl bg-white border border-gray-100 p-5 shadow-sm card-hover">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-10 h-10 rounded-xl bg-gray-100 flex items-center justify-center text-gray-600">
                    <BarChart3 className="h-5 w-5" />
                  </div>
                  <div className="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center text-gray-600">
                    <span className="text-[10px] font-bold">+</span>
                  </div>
                </div>
                <div className="text-2xl font-bold text-gray-900 mb-1">$ 300</div>
                <div className="text-sm text-gray-500 font-medium">Others</div>
              </div>
            </div>
          </section>

          <section className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold text-gray-900">Enrolled Courses</h2>
              <a href="#" className="text-primary-600 text-xs font-bold hover:underline">See all</a>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {enrolledCourses.map((course) => {
                const Icon = course.icon;
                return (
                  <div key={course.title} className="rounded-3xl bg-white border border-gray-100 p-6 shadow-sm card-hover group relative overflow-hidden">
                    <div className={`h-24 rounded-2xl bg-gradient-to-br ${course.color} flex items-center justify-center mb-6 relative z-10 shadow-inner`}>
                      <Icon className="h-10 w-10 text-primary-600/80" />
                    </div>
                    <h3 className="text-lg font-bold text-gray-900 mb-1 group-hover:text-primary-600 transition-colors">{course.title}</h3>
                    <p className="text-sm text-gray-500 mb-4">{course.subtitle}</p>
                    <button className="w-full py-2.5 rounded-xl bg-primary-600 text-white font-semibold text-sm hover:bg-primary-700 transition-colors shadow-lg shadow-primary-200 flex items-center justify-center gap-2">
                      View <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                );
              })}
            </div>
          </section>
        </div>

        <div className="lg:col-span-4 space-y-6">
          <section className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold text-gray-900">Course Instructors</h2>
              <a href="#" className="text-primary-600 text-xs font-bold hover:underline">See all</a>
            </div>
            <div className="flex flex-wrap gap-3">
              {instructors.map((instructor) => (
                <div key={instructor.name} className="relative group">
                  <img
                    src={instructor.image}
                    alt={instructor.name}
                    className="w-14 h-14 rounded-full object-cover border-4 border-white shadow-sm group-hover:scale-105 transition-transform"
                  />
                  <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 w-2 h-2 rounded-full bg-green-400 border-2 border-white"></div>
                </div>
              ))}
              <div className="w-14 h-14 rounded-full border-2 border-dashed border-gray-300 flex items-center justify-center text-gray-400 text-sm font-medium hover:border-primary-300 hover:text-primary-600 transition-colors cursor-pointer">
                +
              </div>
            </div>
          </section>

          <section className="rounded-3xl bg-white border border-gray-100 p-6 shadow-sm space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-bold text-gray-900 text-sm">Daily notice</h3>
              <a href="#" className="text-primary-600 text-xs font-bold hover:underline">See more</a>
            </div>
            <div className="space-y-3">
              <div className="p-3 rounded-xl bg-gray-50 border border-gray-100">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-primary-100 flex items-center justify-center text-primary-600 flex-shrink-0">
                    <Play className="h-4 w-4" />
                  </div>
                  <div>
                    <div className="text-xs font-bold text-primary-700 mb-1">Prelim payment due</div>
                    <div className="text-xs text-gray-600 leading-relaxed">
                      Lorem ipsum dolor sit amet, consectetur adipiscing elit.
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section className="rounded-3xl bg-white border border-gray-100 p-6 shadow-sm space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-bold text-gray-900 text-sm">Exam schedule</h3>
              <a href="#" className="text-primary-600 text-xs font-bold hover:underline">See more</a>
            </div>
            <div className="space-y-3">
              <div className="p-3 rounded-xl bg-gray-50 border border-gray-100">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-primary-50 flex items-center justify-center text-primary-600 flex-shrink-0">
                    <Calendar className="h-4 w-4" />
                  </div>
                  <div>
                    <div className="text-xs font-bold text-gray-900 mb-1">Final Exams - Week 1</div>
                    <div className="text-xs text-gray-600 leading-relaxed">
                      Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc vulputate libero et velit interdum.
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
};