import type { BacklogTask, BacklogTaskDetail, PendingQuestion } from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5144';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`);
  if (!response.ok) {
    throw new Error(`${path} failed: ${response.status}`);
  }
  return response.json() as Promise<T>;
}

export function getQuestions(status: 'open' | 'answered'): Promise<PendingQuestion[]> {
  return getJson<PendingQuestion[]>(`/api/questions?status=${status}`);
}

export function getBacklog(): Promise<BacklogTask[]> {
  return getJson<BacklogTask[]>('/api/backlog');
}

export function getBacklogTaskDetail(id: string): Promise<BacklogTaskDetail> {
  return getJson<BacklogTaskDetail>(`/api/backlog/${id}`);
}

export async function answerQuestion(id: string, answerText: string): Promise<PendingQuestion> {
  const response = await fetch(`${API_BASE_URL}/api/questions/${id}/answer`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ answerText }),
  });
  if (!response.ok) {
    throw new Error(`Failed to answer question: ${response.status}`);
  }
  return response.json() as Promise<PendingQuestion>;
}
