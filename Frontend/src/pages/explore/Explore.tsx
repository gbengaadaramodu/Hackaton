import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { catalogApi } from '../../api/catalog';
import { BookOpen, Atom, Calculator, Zap } from 'lucide-react';
import type { SimulationTemplateDto } from '../../types';

export const Explore = () => {
  const [templates, setTemplates] = useState<SimulationTemplateDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedSubject, setSelectedSubject] = useState('all');

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await catalogApi.getTemplates();
        setTemplates(data);
      } catch (error) {
        console.error('Failed to load templates:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadTemplates();
  }, []);

  const subjects = ['all', ...new Set(templates.map(t => t.subject))];

  const filteredTemplates = selectedSubject === 'all' 
    ? templates 
    : templates.filter(t => t.subject === selectedSubject);

  const getSubjectIcon = (subject: string) => {
    if (subject.toLowerCase().includes('physics')) return <Atom className="h-6 w-6" />;
    if (subject.toLowerCase().includes('math')) return <Calculator className="h-6 w-6" />;
    return <Zap className="h-6 w-6" />;
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Explore Simulations</h1>
        <p className="mt-1 text-sm text-gray-500">
          Discover interactive models to explore scientific concepts
        </p>
      </div>

      {/* Subject Filter */}
      <div className="flex flex-wrap gap-2">
        {subjects.map((subject) => (
          <Button
            key={subject}
            variant={selectedSubject === subject ? 'primary' : 'outline'}
            size="sm"
            onClick={() => setSelectedSubject(subject)}
          >
            {subject === 'all' ? 'All Subjects' : subject}
          </Button>
        ))}
      </div>

      {/* Templates Grid */}
      {isLoading ? (
        <div className="text-center py-12 text-gray-500">Loading simulations...</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredTemplates.map((template) => (
            <Card key={template.modelKey} className="hover:shadow-lg transition-shadow">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="p-2 bg-primary-100 rounded-lg text-primary-600">
                    {getSubjectIcon(template.subject)}
                  </div>
                  <span className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded-full">
                    {template.domain}
                  </span>
                </div>
                <CardTitle className="mt-4">{template.title}</CardTitle>
                <CardDescription>{template.description}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div>
                    <p className="text-xs font-medium text-gray-700 mb-1">Learning Objective</p>
                    <p className="text-sm text-gray-600">{template.learningObjective}</p>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-700 mb-1">Governing Relationship</p>
                    <p className="text-sm font-mono text-primary-700 bg-primary-50 p-2 rounded">
                      {template.governingRelationship}
                    </p>
                  </div>
                  <div className="flex items-center justify-between text-xs text-gray-500">
                    <span>{template.parameters.length} parameters</span>
                    <span>{template.subject}</span>
                  </div>
                  <Link to={`/simulation/${template.modelKey}`}>
                    <Button className="w-full">
                      <PlayCircle className="mr-2 h-4 w-4" />
                      Start Simulation
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};
