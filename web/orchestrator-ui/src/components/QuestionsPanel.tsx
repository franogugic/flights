import { useCallback, useEffect, useState } from 'react';
import { answerQuestion, getQuestions } from '../api';
import type { PendingQuestion } from '../types';
import { QuestionRow } from './QuestionRow';

const POLL_INTERVAL_MS = 5000;

export function QuestionsPanel() {
  const [openQuestions, setOpenQuestions] = useState<PendingQuestion[]>([]);
  const [answeredQuestions, setAnsweredQuestions] = useState<PendingQuestion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      const [open, answered] = await Promise.all([getQuestions('open'), getQuestions('answered')]);
      setOpenQuestions(open);
      setAnsweredQuestions(answered);
      setError(null);
    } catch {
      setError('Could not reach the orchestrator API. Is Flights.Orchestrator.Api running?');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
    const interval = setInterval(refresh, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [refresh]);

  async function handleAnswered(id: string, answerText: string) {
    await answerQuestion(id, answerText);
    await refresh();
  }

  if (loading) {
    return <p>Loading questions...</p>;
  }

  if (error) {
    return <p className="error-banner">{error}</p>;
  }

  return (
    <div className="questions-panel">
      <section>
        <h2>Open questions ({openQuestions.length})</h2>
        {openQuestions.length === 0 ? (
          <p className="empty-state">Nothing waiting on you right now.</p>
        ) : (
          <ul className="question-list">
            {openQuestions.map((q) => (
              <QuestionRow key={q.id} question={q} onAnswered={handleAnswered} />
            ))}
          </ul>
        )}
      </section>

      <section>
        <h2>Answered ({answeredQuestions.length})</h2>
        {answeredQuestions.length === 0 ? (
          <p className="empty-state">No answered questions yet.</p>
        ) : (
          <ul className="question-list">
            {answeredQuestions.map((q) => (
              <QuestionRow key={q.id} question={q} onAnswered={handleAnswered} />
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
