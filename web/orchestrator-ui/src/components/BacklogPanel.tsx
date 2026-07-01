import { useEffect, useState } from 'react';
import { getBacklog } from '../api';
import type { BacklogTask } from '../types';

const POLL_INTERVAL_MS = 5000;

export function BacklogPanel() {
  const [tasks, setTasks] = useState<BacklogTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function refresh() {
      try {
        const data = await getBacklog();
        if (!cancelled) {
          setTasks(data);
          setError(null);
        }
      } catch {
        if (!cancelled) {
          setError('Could not reach the orchestrator API. Is Flights.Orchestrator.Api running?');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    refresh();
    const interval = setInterval(refresh, POLL_INTERVAL_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  if (loading) {
    return <p>Loading backlog...</p>;
  }

  if (error) {
    return <p className="error-banner">{error}</p>;
  }

  if (tasks.length === 0) {
    return <p className="empty-state">No backlog yet — the Architect hasn't run, or is waiting on a question.</p>;
  }

  return (
    <table className="backlog-table">
      <thead>
        <tr>
          <th>Id</th>
          <th>Description</th>
          <th>Status</th>
          <th>Iterations</th>
          <th>Depends on</th>
        </tr>
      </thead>
      <tbody>
        {tasks.map((t) => (
          <tr key={t.id}>
            <td>{t.id}</td>
            <td>{t.description}</td>
            <td>
              <span className={`badge badge--status-${t.status.toLowerCase()}`}>{t.status}</span>
            </td>
            <td>{t.iterationCount}</td>
            <td>{t.dependsOnTaskIds.length > 0 ? t.dependsOnTaskIds.join(', ') : '-'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
