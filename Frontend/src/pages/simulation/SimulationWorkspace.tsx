import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { sessionsApi } from '../../api/sessions';
import { catalogApi } from '../../api/catalog';
import { Send, RotateCcw, Play, Pause, MessageSquare, BarChart3, Settings } from 'lucide-react';
import type { SessionDto, SimulationTemplateDto, TutorMessageDto, TutorMessageResponse } from '../../types';

export const SimulationWorkspace = () => {
  const { modelKey } = useParams<{ modelKey: string }>();
  const navigate = useNavigate();
  
  const [template, setTemplate] = useState<SimulationTemplateDto | null>(null);
  const [session, setSession] = useState<SessionDto | null>(null);
  const [parameters, setParameters] = useState<Record<string, number>>({});
  const [messages, setMessages] = useState<TutorMessageDto[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSendingCommand, setIsSendingCommand] = useState(false);
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [activeTab, setActiveTab] = useState<'simulation' | 'tutor' | 'data'>('simulation');

  useEffect(() => {
    const loadData = async () => {
      if (!modelKey) return;
      
      try {
        const templateData = await catalogApi.getTemplate(modelKey);
        setTemplate(templateData);
        
        // Initialize parameters with defaults
        const defaults: Record<string, number> = {};
        templateData.parameters.forEach(param => {
          defaults[param.name] = param.defaultValue;
        });
        setParameters(defaults);
        
        // Start session
        const sessionData = await sessionsApi.startSession({
          modelKey: modelKey,
          initialState: defaults,
        });
        setSession(sessionData);
        setParameters(sessionData.state);
      } catch (error) {
        console.error('Failed to load simulation:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadData();
  }, [modelKey]);

  const handleParameterChange = (paramName: string, value: number) => {
    setParameters(prev => ({
      ...prev,
      [paramName]: value,
    }));
  };

  const applyChanges = async () => {
    if (!session) return;
    
    setIsSendingCommand(true);
    try {
      // Find which parameter changed - for simplicity, send the first changed one
      // In a real app, we'd track changes better
      const paramEntries = Object.entries(parameters);
      for (const [paramName, value] of paramEntries) {
        if (session.state[paramName] !== value) {
          const result = await sessionsApi.submitCommand(session.id, {
            command: {
              type: 'set-parameter',
              parameter: paramName,
              value: value,
            },
            expectedVersion: session.version,
          });
          setSession(prev => prev ? {
            ...prev,
            version: result.version,
            state: result.state,
            result: result.result,
          } : null);
          break;
        }
      }
    } catch (error) {
      console.error('Failed to apply command:', error);
    } finally {
      setIsSendingCommand(false);
    }
  };

  const resetSimulation = async () => {
    if (!session) return;
    
    try {
      const resetSession = await sessionsApi.reset(session.id);
      setSession(resetSession);
      setParameters(resetSession.state);
    } catch (error) {
      console.error('Failed to reset simulation:', error);
    }
  };

  const sendMessage = async () => {
    if (!session || !newMessage.trim()) return;
    
    setIsSendingMessage(true);
    try {
      const response = await sessionsApi.sendMessage(session.id, {
        message: newMessage,
        level: 'Standard',
      });
      
      // Add user message and tutor response
      const userMsg: TutorMessageDto = {
        id: Date.now().toString(),
        role: 'Student',
        content: newMessage,
        isAiGenerated: false,
        createdAt: new Date().toISOString(),
      };
      
      const tutorMsg: TutorMessageDto = {
        id: response.messageId,
        role: 'Tutor',
        content: response.reply,
        isAiGenerated: response.isAiGenerated,
        provider: response.provider,
        createdAt: new Date().toISOString(),
      };
      
      setMessages(prev => [...prev, userMsg, tutorMsg]);
      setNewMessage('');
    } catch (error) {
      console.error('Failed to send message:', error);
    } finally {
      setIsSendingMessage(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-500">Loading simulation...</div>
      </div>
    );
  }

  if (!template || !session) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Failed to load simulation</p>
        <Button onClick={() => navigate('/explore')} className="mt-4">
          Back to Explore
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{template.title}</h1>
          <p className="text-sm text-gray-500">{template.subject} • {template.learningObjective}</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={resetSimulation}>
            <RotateCcw className="mr-2 h-4 w-4" />
            Reset
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Simulation Area */}
        <div className="lg:col-span-2 space-y-6">
          {/* Visualization */}
          <Card>
            <CardHeader>
              <CardTitle>Simulation</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="aspect-video bg-gradient-to-br from-blue-50 to-indigo-100 rounded-lg flex items-center justify-center">
                <div className="text-center">
                  <div className="text-4xl mb-4">⚛️</div>
                  <p className="text-gray-600 font-medium">{template.title}</p>
                  <p className="text-sm text-gray-500 mt-2">{template.governingRelationship}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Results/Observations */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <BarChart3 className="h-5 w-5 mr-2" />
                Observations
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                {session.result.observations.map((obs, index) => (
                  <div key={index} className="p-4 bg-gray-50 rounded-lg">
                    <p className="text-sm text-gray-600">{obs.label}</p>
                    <p className="text-2xl font-bold text-primary-600">
                      {obs.value.toFixed(3)} <span className="text-lg">{obs.unit}</span>
                    </p>
                    {obs.description && (
                      <p className="text-xs text-gray-500 mt-1">{obs.description}</p>
                    )}
                  </div>
                ))}
              </div>
              <div className="mt-4 p-4 bg-blue-50 rounded-lg">
                <p className="text-sm text-blue-800">{session.result.summary}</p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Controls and Tutor */}
        <div className="space-y-6">
          {/* Parameters */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Settings className="h-5 w-5 mr-2" />
                Parameters
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {template.parameters.map((param) => (
                <div key={param.name}>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {param.label} {param.unit && `(${param.unit})`}
                  </label>
                  <input
                    type="range"
                    min={param.min}
                    max={param.max}
                    step={param.step}
                    value={parameters[param.name] || param.defaultValue}
                    onChange={(e) => handleParameterChange(param.name, parseFloat(e.target.value))}
                    className="w-full"
                  />
                  <div className="flex justify-between text-xs text-gray-500">
                    <span>{param.min}</span>
                    <span className="font-medium text-gray-700">
                      {(parameters[param.name] || param.defaultValue).toFixed(2)}
                    </span>
                    <span>{param.max}</span>
                  </div>
                  {param.description && (
                    <p className="text-xs text-gray-500 mt-1">{param.description}</p>
                  )}
                </div>
              ))}
              <Button 
                onClick={applyChanges} 
                disabled={isSendingCommand}
                className="w-full"
              >
                {isSendingCommand ? 'Applying...' : 'Apply Changes'}
              </Button>
            </CardContent>
          </Card>

          {/* AI Tutor */}
          <Card className="h-96">
            <CardHeader>
              <CardTitle className="flex items-center">
                <MessageSquare className="h-5 w-5 mr-2" />
                AI Tutor
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col h-full pb-6">
              <div className="flex-1 overflow-y-auto space-y-3 mb-4">
                {messages.length === 0 ? (
                  <div className="text-center text-gray-500 text-sm py-4">
                    Ask the tutor anything about this simulation!
                  </div>
                ) : (
                  messages.map((msg) => (
                    <div
                      key={msg.id}
                      className={`p-3 rounded-lg ${
                        msg.role === 'Student'
                          ? 'bg-primary-50 ml-4'
                          : 'bg-gray-50 mr-4'
                      }`}
                    >
                      <p className="text-xs font-medium mb-1 text-gray-600">
                        {msg.role} {msg.isAiGenerated && msg.role === 'Tutor' && '(AI)'}
                      </p>
                      <p className="text-sm text-gray-800">{msg.content}</p>
                    </div>
                  ))
                )}
              </div>
              <div className="flex gap-2">
                <Input
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  placeholder="Ask a question..."
                  onKeyPress={(e) => e.key === 'Enter' && !e.shiftKey && sendMessage()}
                />
                <Button 
                  size="sm" 
                  onClick={sendMessage}
                  disabled={isSendingMessage || !newMessage.trim()}
                >
                  <Send className="h-4 w-4" />
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};
