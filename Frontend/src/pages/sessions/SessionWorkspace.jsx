import { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  Send,
  Sliders,
  Sparkles,
  RefreshCw,
  CheckCircle2,
  AlertCircle,
  Play,
  Pause,
  Bot,
  User,
} from 'lucide-react';
import {
  getSession,
  getMessages,
  sendMessage,
  submitCommand,
  changeStatus,
} from '../../api/sessions';
import { getCatalog } from '../../api/catalog';
import SvgScenePlayer from '../../components/sim/SvgScenePlayer';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function SessionWorkspace() {
  const { id } = useParams();
  const queryClient = useQueryClient();
  const [chatInput, setChatInput] = useState('');
  const [paramState, setParamState] = useState({});
  const messagesEndRef = useRef(null);

  // Fetch authoritative session state
  const {
    data: session,
    isLoading: loadingSession,
    error: sessionError,
  } = useQuery({
    queryKey: ['session', id],
    queryFn: () => getSession(id),
    enabled: !!id,
    refetchInterval: 15000,
  });

  // Catalog drives slider ranges (min/max/step/label/unit per model)
  const { data: catalog } = useQuery({
    queryKey: ['catalog'],
    queryFn: getCatalog,
  });

  // Sync parameters when session loads
  useEffect(() => {
    if (session?.state) {
      setParamState(session.state);
    }
  }, [session]);

  // Fetch AI Tutor messages
  const { data: messages = [], isLoading: loadingMessages } = useQuery({
    queryKey: ['session-messages', id],
    queryFn: () => getMessages(id),
    enabled: !!id,
  });

  // Scroll to bottom of chat
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Send message to AI Tutor mutation
  const sendMsgMutation = useMutation({
    mutationFn: (msg) => sendMessage(id, { message: msg }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session-messages', id] });
      setChatInput('');
    },
  });

  // Submit parameter update command mutation
  const commandMutation = useMutation({
    mutationFn: (paramChange) =>
      submitCommand(id, {
        command: {
          type: 'set-parameter',
          parameter: paramChange.param,
          value: Number(paramChange.value),
        },
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session', id] });
    },
  });

  // Change status (Complete / Pause / Resume)
  const statusMutation = useMutation({
    mutationFn: (newStatus) => changeStatus(id, newStatus),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session', id] });
    },
  });

  const handleSendMessage = (e) => {
    e.preventDefault();
    if (!chatInput.trim()) return;
    sendMsgMutation.mutate(chatInput.trim());
  };

  const handleParamChange = (param, value) => {
    setParamState((prev) => ({ ...prev, [param]: value }));
  };

  const handleApplyParam = (param, value) => {
    commandMutation.mutate({ param, value });
  };

  if (loadingSession) return <Spinner />;
  if (sessionError || !session) {
    return (
      <div className="p-6 bg-rose-50 text-rose-700 rounded-xl max-w-2xl mx-auto">
        Session not found or failed to load.
      </div>
    );
  }

  const result = session.result || {};
  const observations = result.observations || [];
  const modelInfo = session.model || result.model || {};
  const modelKey = session.modelKey || result.model?.modelKey;
  const simulations = catalog?.simulations || catalog?.templates || [];
  const template = simulations.find((s) => s.modelKey === modelKey);
  const paramDefs = template?.parameters || [];

  return (
    <div className="space-y-6 max-w-7xl mx-auto h-[calc(100vh-6rem)] flex flex-col">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 flex-shrink-0">
        <div className="flex items-center gap-3">
          <Link
            to="/sessions"
            className="p-1.5 rounded-xl border border-slate-200 text-slate-500 hover:bg-slate-50 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-slate-800">
                {modelInfo.title || session.modelKey || 'Simulation Workspace'}
              </h1>
              <Badge
                variant={
                  session.status === 'Completed'
                    ? 'success'
                    : session.status === 'Active'
                    ? 'primary'
                    : 'default'
                }
              >
                {session.status}
              </Badge>
            </div>
            <p className="text-xs text-slate-500">
              {modelInfo.domain || 'Physics'} • Objective: {modelInfo.learningObjective || 'Scientific Modeling'}
            </p>
          </div>
        </div>

        {/* Action Controls */}
        <div className="flex items-center gap-2">
          {session.status === 'Active' ? (
            <Button
              size="sm"
              variant="outline"
              loading={statusMutation.isPending}
              onClick={() => statusMutation.mutate('Paused')}
            >
              <Pause className="w-3.5 h-3.5 mr-1" /> Pause
            </Button>
          ) : (
            <Button
              size="sm"
              variant="outline"
              loading={statusMutation.isPending}
              onClick={() => statusMutation.mutate('Active')}
            >
              <Play className="w-3.5 h-3.5 mr-1" /> Resume
            </Button>
          )}

          {session.status !== 'Completed' && (
            <Button
              size="sm"
              variant="primary"
              loading={statusMutation.isPending}
              onClick={() => statusMutation.mutate('Completed')}
            >
              <CheckCircle2 className="w-3.5 h-3.5 mr-1" /> Complete Session
            </Button>
          )}
        </div>
      </div>

      {/* Main Split Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 flex-1 min-h-0">
        {/* Left / Center: Simulation Controls & Observations (7 cols) */}
        <div className="lg:col-span-7 flex flex-col gap-4 overflow-y-auto pr-1 min-w-0">
          <SvgScenePlayer modelKey={modelKey} state={session.state} />
          {/* Simulation Model State & Controls */}
          <Card className="p-5">
            <div className="flex items-center gap-2 mb-4">
              <Sliders className="w-5 h-5 text-primary-600" />
              <h3 className="font-bold text-slate-800 text-sm">
                Manipulate Variables & Parameters
              </h3>
            </div>

            <div className="space-y-4">
              {Object.entries(paramState).length === 0 ? (
                <p className="text-xs text-slate-400">
                  No adjustable state parameters declared in this model.
                </p>
              ) : (
                Object.entries(paramState).map(([key, val]) => {
                  const def = paramDefs.find(
                    (d) => d.name?.toLowerCase() === key.toLowerCase()
                  );
                  const min = def?.min ?? 0;
                  const max = def?.max ?? 100;
                  const step = def?.step ?? 0.5;
                  const label = def?.label || key.replace(/([A-Z])/g, ' $1');
                  const unit = def?.unit ? ` ${def.unit}` : '';
                  return (
                    <div key={key} className="space-y-1.5 bg-slate-50 p-3.5 rounded-xl border border-slate-100">
                      <div className="flex justify-between items-center text-xs">
                        <span className="font-semibold text-slate-700 capitalize">
                          {label}
                        </span>
                        <span className="font-mono text-primary-700 font-bold bg-primary-50 px-2 py-0.5 rounded-md">
                          {val}{unit}
                        </span>
                      </div>

                      <div className="flex items-center gap-3">
                        <input
                          type="range"
                          min={min}
                          max={max}
                          step={step}
                          value={val}
                          onChange={(e) => handleParamChange(key, e.target.value)}
                          className="w-full h-1.5 bg-slate-200 rounded-lg appearance-none cursor-pointer accent-primary-600"
                        />
                        <Button
                          size="sm"
                          variant="secondary"
                          className="text-xs px-2.5 py-1 whitespace-nowrap"
                          loading={commandMutation.isPending}
                          onClick={() => handleApplyParam(key, val)}
                        >
                          Apply
                        </Button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </Card>

          {/* Observations & Output */}
          <Card className="p-5 flex-1">
            <div className="flex items-center gap-2 mb-3">
              <Sparkles className="w-5 h-5 text-amber-500" />
              <h3 className="font-bold text-slate-800 text-sm">
                Real-Time Observations & Behavior
              </h3>
            </div>

            {result.summary && (
              <div className="p-3 bg-primary-50 text-primary-900 rounded-xl text-xs font-medium mb-3 border border-primary-100">
                {result.summary}
              </div>
            )}

            {observations.length === 0 ? (
              <p className="text-xs text-slate-400">
                Adjust variables and submit to observe deterministic physical transformations.
              </p>
            ) : (
              <ul className="space-y-2">
                {observations.map((obs, i) => (
                  <li
                    key={i}
                    className="p-2.5 bg-white border border-slate-100 rounded-xl text-xs text-slate-700 flex items-start gap-2"
                  >
                    <span className="w-2 h-2 rounded-full bg-emerald-500 mt-1.5 flex-shrink-0" />
                    <span className="break-words min-w-0">
                      {typeof obs === 'string'
                        ? obs
                        : obs.display
                          ? `${obs.label}: ${obs.display}`
                          : `${obs.label ?? obs.name}: ${obs.value} ${obs.unit ?? ''}`}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>

        {/* Right: AI Grounded Tutor Chat (5 cols) */}
        <div className="lg:col-span-5 flex flex-col h-full min-h-0 min-w-0 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          {/* Chat Header */}
          <div className="px-4 py-3 bg-slate-50 border-b border-slate-200 flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-xl bg-primary-600 text-white flex items-center justify-center">
              <Bot className="w-4 h-4" />
            </div>
            <div>
              <h3 className="font-bold text-slate-800 text-xs">
                Grounded AI Tutor
              </h3>
              <p className="text-[10px] text-slate-400">
                Answers strictly verified by simulation physics
              </p>
            </div>
          </div>

          {/* Messages Scroll Area */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3.5">
            {messages.length === 0 ? (
              <div className="text-center py-8">
                <Bot className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                <p className="text-xs text-slate-500 font-medium">
                  Have a question about this model?
                </p>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  Ask why an outcome occurred, or what happens when a variable changes.
                </p>
              </div>
            ) : (
              messages.map((m, idx) => {
                const isAssistant = m.role === 'assistant' || m.role === 'Assistant';
                return (
                  <div
                    key={m.id || idx}
                    className={`flex gap-2.5 ${isAssistant ? 'items-start' : 'items-start flex-row-reverse'}`}
                  >
                    <div
                      className={`w-7 h-7 rounded-full flex items-center justify-center text-xs flex-shrink-0 ${
                        isAssistant
                          ? 'bg-primary-100 text-primary-700'
                          : 'bg-slate-200 text-slate-700'
                      }`}
                    >
                      {isAssistant ? <Bot className="w-3.5 h-3.5" /> : <User className="w-3.5 h-3.5" />}
                    </div>
                    <div
                      className={`max-w-[85%] min-w-0 break-words rounded-2xl px-3.5 py-2 text-xs leading-relaxed ${
                        isAssistant
                          ? 'bg-slate-100 text-slate-800 rounded-tl-none'
                          : 'bg-primary-600 text-white rounded-tr-none'
                      }`}
                    >
                      {m.content}
                    </div>
                  </div>
                );
              })
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Message Input */}
          <form
            onSubmit={handleSendMessage}
            className="p-3 border-t border-slate-200 bg-white flex items-center gap-2"
          >
            <input
              type="text"
              value={chatInput}
              onChange={(e) => setChatInput(e.target.value)}
              placeholder="Ask the simulation tutor..."
              className="flex-1 min-w-0 px-3.5 py-2 rounded-xl border border-slate-200 text-xs focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            <Button
              type="submit"
              size="sm"
              variant="primary"
              aria-label="Send message"
              disabled={!chatInput.trim()}
              loading={sendMsgMutation.isPending}
              className="p-2.5 flex-shrink-0"
            >
              <Send className="w-3.5 h-3.5" />
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}


