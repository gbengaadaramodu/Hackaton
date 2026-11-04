import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Compass, Search, Play, Filter, Sparkles, BookOpen } from 'lucide-react';
import { getCatalog } from '../../api/catalog';
import { startSession } from '../../api/sessions';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function Explore() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [selectedSubject, setSelectedSubject] = useState('ALL');

  const { data: catalog, isLoading, error } = useQuery({
    queryKey: ['catalog'],
    queryFn: getCatalog,
  });

  const launchMutation = useMutation({
    mutationFn: (modelKey) => startSession({ modelKey }),
    onSuccess: (session) => {
      navigate(`/sessions/${session.id}`);
    },
  });

  const simulations = catalog?.simulations || [];
  const catalogSubjects = catalog?.subjects || [];

  // The catalog keys subjects by code ("PHY") while each template carries the subject
  // name ("Physics"), so resolve codes to names and include names only templates mention.
  const subjectNames = Array.from(
    new Set([
      ...catalogSubjects.map((s) => s.name).filter(Boolean),
      ...simulations.map((sim) => sim.subject).filter(Boolean),
    ])
  );
  const subjects = ['ALL', ...subjectNames];
  const subjectName = (value) =>
    catalogSubjects.find((s) => s.code === value || s.name === value)?.name || value;

  const filteredSims = simulations.filter((sim) => {
    const matchesSearch =
      sim.title?.toLowerCase().includes(search.toLowerCase()) ||
      sim.description?.toLowerCase().includes(search.toLowerCase()) ||
      sim.domain?.toLowerCase().includes(search.toLowerCase());
    const matchesSubject =
      selectedSubject === 'ALL' ||
      subjectName(sim.subject) === selectedSubject ||
      subjectName(sim.domain) === selectedSubject;
    return matchesSearch && matchesSubject;
  });

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">
          Simulation Catalog & Models
        </h1>
        <p className="text-sm text-slate-500 mt-1">
          Browse verified mathematical and physical simulation templates across STEM curriculum subjects.
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
            placeholder="Search simulations, concepts, physical laws..."
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
          Failed to load catalog: {error.message}
        </div>
      ) : filteredSims.length === 0 ? (
        <Card className="p-12 text-center bg-white border border-slate-200">
          <Compass className="w-12 h-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-base font-semibold text-slate-700">No simulations matched</h3>
          <p className="text-xs text-slate-400 mt-1">
            Try adjusting your search criteria or subject filter.
          </p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredSims.map((sim) => (
            <Card
              key={sim.modelKey}
              className="p-6 flex flex-col justify-between hover:border-primary-300 transition-all group"
            >
              <div>
                <div className="flex items-center justify-between gap-2 mb-3">
                  <Badge variant="primary" className="text-xs uppercase font-bold tracking-wider">
                    {sim.subject || sim.domain || 'STEM'}
                  </Badge>
                  <span className="text-[11px] font-mono text-slate-400">
                    {sim.modelKey}
                  </span>
                </div>

                <h3 className="font-bold text-slate-800 text-base group-hover:text-primary-600 transition-colors">
                  {sim.title}
                </h3>
                <p className="text-xs text-slate-500 mt-2 line-clamp-3 leading-relaxed">
                  {sim.description || sim.governingRelationship || 'Deterministic simulation model.'}
                </p>

                {sim.learningObjective && (
                  <div className="mt-3 p-2.5 bg-slate-50 rounded-xl text-[11px] text-slate-600 flex items-start gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-primary-500 flex-shrink-0 mt-0.5" />
                    <span>Objective: {sim.learningObjective}</span>
                  </div>
                )}
              </div>

              <div className="pt-5 mt-4 border-t border-slate-100 flex items-center justify-between">
                <span className="text-xs text-slate-400">
                  {sim.renderer ? `Renderer: ${sim.renderer}` : 'Interactive Engine'}
                </span>
                <Button
                  size="sm"
                  variant="primary"
                  className="text-xs"
                  loading={launchMutation.isPending}
                  onClick={() => launchMutation.mutate(sim.modelKey)}
                >
                  <Play className="w-3.5 h-3.5 mr-1 fill-current" />
                  Launch
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
