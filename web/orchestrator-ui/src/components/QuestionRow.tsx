import { useState } from 'react';
import type { PendingQuestion } from '../types';

interface Props {
  question: PendingQuestion;
  onAnswered: (id: string, answerText: string) => Promise<void>;
}

export function QuestionRow({ question, onAnswered }: Props) {
  const [expanded, setExpanded] = useState(false);
  const [answerText, setAnswerText] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isOpen = question.status === 'Open';

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!answerText.trim()) return;

    setSubmitting(true);
    setError(null);
    try {
      await onAnswered(question.id, answerText.trim());
      setExpanded(false);
      setAnswerText('');
    } catch {
      setError('Failed to submit answer. Try again.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <li className="question-row">
      <div className="question-row__header">
        <span className={`badge badge--${question.source === 'Architect' ? 'architect' : 'blocked'}`}>
          {question.source === 'Architect' ? 'Architect' : 'Blocked task'}
        </span>
        {question.backlogTaskId && <span className="question-row__task-id">{question.backlogTaskId}</span>}
        <span className="question-row__date">{new Date(question.createdAt).toLocaleString()}</span>
      </div>

      <p className="question-row__text">{question.questionText}</p>

      {question.status === 'Answered' && (
        <p className="question-row__answer">
          <strong>Answer:</strong> {question.answerText}
        </p>
      )}

      {isOpen && !expanded && (
        <button type="button" onClick={() => setExpanded(true)}>
          Answer
        </button>
      )}

      {isOpen && expanded && (
        <form onSubmit={handleSubmit} className="question-row__form">
          <textarea
            value={answerText}
            onChange={(e) => setAnswerText(e.target.value)}
            placeholder="Type your answer..."
            rows={3}
            autoFocus
          />
          <div className="question-row__form-actions">
            <button type="submit" disabled={submitting || !answerText.trim()}>
              {submitting ? 'Submitting...' : 'Submit'}
            </button>
            <button type="button" onClick={() => setExpanded(false)} disabled={submitting}>
              Cancel
            </button>
          </div>
          {error && <p className="question-row__error">{error}</p>}
        </form>
      )}
    </li>
  );
}
