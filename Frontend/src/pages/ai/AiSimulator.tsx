import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { simulationRequestsApi } from '../../api/simulation-requests';
import { sessionsApi } from '../../api/sessions';
import { Brain, Send, Sparkles } from 'lucide-react';

export const AiSimulator = () => {
  const [prompt, setPrompt] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const navigate = useNavigate();

  const examples = [
    "Show me how changing resistance affects current in a circuit",
    "How does ramp angle affect acceleration down an incline?",
    "I want to understand Ohm's law with different voltages",
    "Demonstrate the relationship between force and motion on a slope"
  ];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!prompt.trim()) return;
    
    setIsLoading(true);
    try {
      const request = await simulationRequestsApi.create({ prompt });
      setResult(request);
      
      // If it has a scene/model, we can start a session
      if (request.scene?.modelKey || request.interpretation?.modelKey) {
        const modelKey = request.scene?.modelKey || request.interpretation?.modelKey;
        setTimeout(() => {
          navigate(`/simulation/${modelKey}`);
        }, 1500);
      }
    } catch (error) {
      console.error('Failed to create simulation request:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const useExample = (example: string) => {
    setPrompt(example);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="text-center">
        <div className="flex justify-center mb-4">
          <div className="p-3 bg-purple-100 rounded-full">
            <Brain className="h-8 w-8 text-purple-600" />
          </div>
        </div>
        <h1 className="text-3xl font-bold text-gray-900">AI-Powered Simulation</h1>
        <p className="mt-2 text-gray-600">
          Describe what you want to explore in natural language
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Sparkles className="h-5 w-5 mr-2 text-yellow-500" />
            What would you like to learn?
          </CardTitle>
          <CardDescription>
            Tell me about a physics concept you'd like to explore interactively
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="e.g., Show me how changing resistance affects current in a circuit..."
              className="w-full h-32 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent resize-none"
              disabled={isLoading}
            />
            <Button type="submit" disabled={isLoading || !prompt.trim()} className="w-full">
              <Send className="mr-2 h-4 w-4" />
              {isLoading ? 'Analyzing...' : 'Generate Simulation'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {!result && (
        <Card>
          <CardHeader>
            <CardTitle>Try an example</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {examples.map((example, index) => (
                <button
                  key={index}
                  onClick={() => useExample(example)}
                  className="p-3 text-left text-sm text-gray-700 border border-gray-200 rounded-lg hover:bg-gray-50 hover:border-primary-300 transition-colors"
                >
                  {example}
                </button>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {result && (
        <Card>
          <CardHeader>
            <CardTitle>Simulation Request Created</CardTitle>
            <CardDescription>Status: {result.status}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <p className="text-sm text-gray-600">"{result.prompt}"</p>
              {result.interpretation && (
                <div className="p-3 bg-blue-50 rounded-lg">
                  <p className="text-sm font-medium text-blue-900 mb-1">Interpretation</p>
                  <p className="text-sm text-blue-800">
                    Model: {result.interpretation.modelKey} • Confidence: {Math.round(result.interpretation.confidence * 100)}%
                  </p>
                  <p className="text-sm text-blue-700 mt-1">{result.interpretation.rationale}</p>
                </div>
              )}
              {result.status === 'Completed' && (
                <p className="text-sm text-green-600">Redirecting to simulation...</p>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
