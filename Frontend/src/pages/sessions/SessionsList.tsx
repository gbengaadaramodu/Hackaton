import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { sessionsApi } from '../../api/sessions';
import { PlayCircle, Clock, Plus } from 'lucide-react';
import type { SessionSummaryDto } from '../../types';

export const SessionsList = () => {
  const [sessions, setSessions] = useState<SessionSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadSessions = async () => {
      try {
        const data = await sessionsApi.getSessions();
        setSessions(data);
      } catch (error) {
        console.error('Failed to load sessions:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadSessions();
  }, []);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">My Sessions</h1>
          <p className="mt-1 text-sm text-gray-500">
            Your learning sessions and progress
          </p>
        </div>
        <Link to="/explore">
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            New Session
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Sessions</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8 text-gray-500">Loading sessions...</div>
          ) : sessions.length === 0 ? (
            <div className="text-center py-8">
              <PlayCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-500 mb-4">No sessions yet</p>
              <Link to="/explore">
                <Button>Start Your First Simulation</Button>
              </Link>
            </div>
          ) : (
            <div className="space-y-3">
              {sessions.map((session) => (
                <Link key={session.id} to={`/session/${session.id}`}>
                  <div className="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer transition-colors">
                    <div className="flex items-center space-x-4">
                      <div className="p-2 bg-primary-100 rounded-lg">
                        <PlayCircle className="h-5 w-5 text-primary-600" />
                      </div>
                      <div>
                        <h4 className="font-medium text-gray-900">{session.modelTitle}</h4>
                        <div className="flex items-center text-xs text-gray-500 space-x-4 mt-1">
                          <span className="flex items-center">
                            <Clock className="h-3 w-3 mr-1" />
                            {formatDate(session.lastActivityAt)}
                          </span>
                          <span>{session.commandCount} commands</span>
                          <span>{session.messageCount} messages</span>
                          <span>Version {session.version}</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center space-x-3">
                      <span className={`px-2 py-1 text-xs rounded-full ${
                        session.status === 'Active' ? 'bg-green-100 text-green-800' : 
                        session.status === 'Completed' ? 'bg-blue-100 text-blue-800' : 
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {session.status}
                      </span>
                      <Button variant="outline" size="sm">Continue</Button>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
