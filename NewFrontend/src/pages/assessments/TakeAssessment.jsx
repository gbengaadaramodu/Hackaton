import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { ArrowLeft, CheckCircle2, XCircle, Award } from 'lucide-react';
import { getAssessment, submitAttempt } from '../../api/assessments';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Badge from '../../components/ui/Badge';
import Spinner from '../../components/ui/Spinner';

export default function TakeAssessment() {
  const { id } = useParams();
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [errorMsg, setErrorMsg] = useState('');

  const {
    data: quiz,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['assessment', id],
    queryFn: () => getAssessment(id),
    enabled: !!id,
  });

  const submitMutation = useMutation({
    mutationFn: (answersPayload) => submitAttempt(id, answersPayload),
    onSuccess: (data) => {
      setResult(data);
    },
    onError: (err) => {
      setErrorMsg(
        err.response?.data?.detail || 'Failed to submit quiz attempt.'
      );
    },
  });

  const isNumeric = (question) => question.type === 'Numeric';
  const allowsMultiple = (question) => question.type === 'MultipleChoice';

  const selectOption = (question, optionId) => {
    if (result) return;
    setAnswers((prev) => {
      const selected = prev[question.id]?.selectedOptionIds ?? [];
      const next = allowsMultiple(question)
        ? selected.includes(optionId)
          ? selected.filter((option) => option !== optionId)
          : [...selected, optionId]
        : [optionId];
      return { ...prev, [question.id]: { selectedOptionIds: next } };
    });
  };

  const setNumericValue = (question, rawValue) => {
    if (result) return;
    setAnswers((prev) => ({
      ...prev,
      [question.id]: {
        numericValue: rawValue === '' ? undefined : Number(rawValue),
      },
    }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!quiz) return;
    setErrorMsg('');

    // Mirror the API's AssessmentAnswer payload: option ids for choice questions,
    // numericValue for numeric ones.
    const answersList = (quiz.questions || []).map((question) => {
      const entry = answers[question.id] || {};
      if (isNumeric(question)) {
        return {
          questionId: question.id,
          numericValue:
            entry.numericValue === undefined ? null : entry.numericValue,
        };
      }

      return {
        questionId: question.id,
        selectedOptionIds: entry.selectedOptionIds ?? [],
      };
    });

    submitMutation.mutate({ answers: answersList });
  };

  if (isLoading) return <Spinner />;
  if (error || !quiz) {
    return (
      <div className="p-6 bg-rose-50 text-rose-700 rounded-xl max-w-2xl mx-auto">
        Assessment not found or failed to load.
      </div>
    );
  }

  const questions = quiz.questions || [];

  return (
    <div className="space-y-6 max-w-3xl mx-auto pb-12">
      <div>
        <Link
          to="/assessments"
          className="inline-flex items-center text-sm font-medium text-slate-500 hover:text-primary-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4 mr-1" /> Back to Quizzes
        </Link>
      </div>

      {errorMsg && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl text-xs font-medium">
          {errorMsg}
        </div>
      )}

      {/* Quiz Header */}
      <Card className="p-6 md:p-8">
        <div className="flex items-center justify-between gap-4">
          <div>
            <Badge variant="primary" className="text-xs mb-2">
              Curriculum Quiz
            </Badge>
            <h1 className="text-2xl font-bold text-slate-800">
              {quiz.title}
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              {questions.length} questions • Deterministic scoring with instant feedback
            </p>
          </div>
        </div>
      </Card>

      {/* Scored Result Banner */}
      {result && (
        <Card className="p-6 border-primary-200 bg-gradient-to-r from-primary-50 to-white animate-in fade-in">
          <div className="flex items-center gap-4">
            <div className="w-14 h-14 rounded-2xl bg-primary-600 text-white flex items-center justify-center font-bold text-lg flex-shrink-0 shadow-md">
              {Math.round(result.percentage)}%
            </div>
            <div>
              <h3 className="font-bold text-slate-800 text-lg">
                {result.passed ? 'Assessment Passed! 🎉' : 'Needs Review'}
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Score: {result.score} / {result.maxScore} points ({Math.round(result.percentage)}%)
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Questions Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {questions.map((q, idx) => {
          const options = q.options || [];
          const selectedOptionIds = answers[q.id]?.selectedOptionIds ?? [];
          const numericValue = answers[q.id]?.numericValue ?? '';
          const feedbackItem = result?.feedback?.find(
            (f) => f.questionId === q.id
          );

          return (
            <Card key={q.id || idx} className="p-6 space-y-4">
              <div className="flex items-start gap-3">
                <span className="w-6 h-6 rounded-full bg-slate-100 text-slate-700 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                  {idx + 1}
                </span>
                <div className="flex-1">
                  <h4 className="text-sm font-semibold text-slate-800">
                    {q.prompt}
                  </h4>
                  {q.learningObjectiveCode && (
                    <span className="text-[10px] text-slate-400 mt-0.5 block">
                      Target: {q.learningObjectiveCode}
                    </span>
                  )}
                  {allowsMultiple(q) && (
                    <span className="text-[10px] text-primary-500 mt-0.5 block">
                      Select every correct answer.
                    </span>
                  )}
                </div>
              </div>

              {/* Options or numeric entry */}
              {isNumeric(q) ? (
                <div className="pt-2">
                  <label className="block text-xs font-semibold text-slate-600 mb-1">
                    Your answer
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={numericValue}
                    onChange={(e) => setNumericValue(q, e.target.value)}
                    disabled={!!result}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:bg-slate-50"
                  />
                </div>
              ) : (
                <div className="space-y-2 pt-2">
                  {options.map((opt) => {
                    const isSelected = selectedOptionIds.includes(opt.id);
                    const inputType = allowsMultiple(q) ? 'checkbox' : 'radio';

                    return (
                      <label
                        key={opt.id}
                        className={`w-full text-left p-3 rounded-xl border text-xs font-medium transition-all flex items-center justify-between cursor-pointer ${
                          isSelected
                            ? 'border-primary-500 bg-primary-50 text-primary-900 ring-1 ring-primary-500'
                            : 'border-slate-200 hover:bg-slate-50 text-slate-700'
                        } ${result ? 'cursor-default' : ''}`}
                      >
                        <span className="flex items-center gap-2">
                          <input
                            type={inputType}
                            name={`question-${q.id}`}
                            checked={isSelected}
                            onChange={() => selectOption(q, opt.id)}
                            disabled={!!result}
                            className="accent-primary-600"
                          />
                          {opt.text}
                        </span>
                        {isSelected && !allowsMultiple(q) && (
                          <CheckCircle2 className="w-4 h-4 text-primary-600 flex-shrink-0" />
                        )}
                      </label>
                    );
                  })}
                </div>
              )}

              {/* Individual Question Feedback */}
              {feedbackItem && (
                <div
                  className={`p-3 rounded-xl text-xs flex items-start gap-2 ${
                    feedbackItem.correct
                      ? 'bg-emerald-50 text-emerald-800 border border-emerald-200'
                      : 'bg-rose-50 text-rose-800 border border-rose-200'
                  }`}
                >
                  {feedbackItem.correct ? (
                    <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0 mt-0.5" />
                  ) : (
                    <XCircle className="w-4 h-4 text-rose-600 flex-shrink-0 mt-0.5" />
                  )}
                  <div>
                    <span className="font-semibold block">
                      {feedbackItem.correct ? 'Correct!' : 'Incorrect'}
                    </span>
                    {!feedbackItem.correct && feedbackItem.correctAnswer && (
                      <p className="mt-0.5">
                        Correct answer: <span className="font-semibold">{feedbackItem.correctAnswer}</span>
                      </p>
                    )}
                    <p className="mt-0.5">{feedbackItem.explanation}</p>
                  </div>
                </div>
              )}
            </Card>
          );
        })}

        {!result && (
          <div className="pt-2 flex justify-end">
            <Button
              type="submit"
              size="lg"
              variant="primary"
              loading={submitMutation.isPending}
            >
              Submit Assessment Answers
            </Button>
          </div>
        )}
      </form>
    </div>
  );
}
