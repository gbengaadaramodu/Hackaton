import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { BookOpen, Search, ArrowRight, Play, Tag, Filter } from 'lucide-react';
import { listLessons } from '../../api/lessons';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function LessonList() {
  const [search, setSearch] = useState('');
  const [selectedSubject, setSelectedSubject] = useState('ALL');

  const { data: lessons = [], isLoading, error } = useQuery({
    queryKey: ['lessons'],
    queryFn: () => listLessons(),
  });

  const lessonList = Array.isArray(lessons) ? lessons : (lessons?.items ?? []);

  const subjectLabel = (lesson) => lesson.subjectName || lesson.subjectCode;
  const subjects = ['ALL', ...new Set(lessonList.map(subjectLabel).filter(Boolean))];

  const filtered = lessonList.filter((l) => {
    const matchesSearch =
      l.title.toLowerCase().includes(search.toLowerCase()) ||
      (l.description && l.description.toLowerCase().includes(search.toLowerCase()));
    const matchesSubject =
      selectedSubject === 'ALL' || subjectLabel(l) === selectedSubject;
    return matchesSearch && matchesSubject;
  });

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Curriculum Lessons</h1>
        <p className="text-sm text-slate-500 mt-1">
          Explore interactive curriculum modules powered by physics, chemistry, and biology mathematical simulations.
        </p>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row gap-4 items-stretch sm:items-center justify-between">
        <div className="relative flex-1 max-w-md">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search lessons by topic or concept..."
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white"
          />
        </div>

        {/* Subject Filter Pills */}
        <div className="flex items-center gap-2 overflow-x-auto pb-1 sm:pb-0">
          <Filter className="w-4 h-4 text-slate-400 flex-shrink-0" />
          {subjects.map((sub) => (
            <button
              key={sub}
              onClick={() => setSelectedSubject(sub)}
              className={`px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors ${
                selectedSubject === sub
                  ? 'bg-primary-600 text-white'
                  : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'
              }`}
            >
              {sub}
            </button>
          ))}
        </div>
      </div>

      {isLoading ? (
        <Spinner />
      ) : error ? (
        <div className="p-4 bg-rose-50 text-rose-700 rounded-xl text-sm border border-rose-200">
          Failed to load lessons: {error.message}
        </div>
      ) : filtered.length === 0 ? (
        <Card className="p-12 text-center bg-white border border-slate-200">
          <BookOpen className="w-12 h-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-base font-semibold text-slate-700">No lessons matched</h3>
          <p className="text-xs text-slate-400 mt-1">
            Try adjusting your search criteria or subject filter.
          </p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filtered.map((lesson) => (
            <Card
              key={lesson.id}
              className="p-6 flex flex-col justify-between hover:border-primary-300 transition-all group"
            >
              <div>
                <div className="flex items-center justify-between gap-2 mb-3">
                  <Badge variant="primary" className="text-xs uppercase font-bold tracking-wider">
                    {lesson.subjectName || lesson.subjectCode || 'Subject'}
                  </Badge>
                  {lesson.gradeLevel && (
                    <span className="text-[11px] font-medium text-slate-400">
                      {lesson.gradeLevel}
                    </span>
                  )}
                </div>

                <h3 className="font-bold text-slate-800 text-base group-hover:text-primary-600 transition-colors">
                  {lesson.title}
                </h3>
                <p className="text-xs text-slate-500 mt-2 line-clamp-2">
                  {lesson.description ||
                    'Interactive simulation scenario with real-time computational models and AI tutoring.'}
                </p>
              </div>

              <div className="pt-5 mt-4 border-t border-slate-100 flex items-center justify-between">
                <span className="text-xs text-slate-400">
                  {lesson.modelKey || 'Simulation Model'}
                </span>
                <Link to={`/lessons/${lesson.id}`}>
                  <Button size="sm" variant="primary" className="text-xs">
                    <Play className="w-3.5 h-3.5 mr-1 fill-current" />
                    Launch
                  </Button>
                </Link>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
