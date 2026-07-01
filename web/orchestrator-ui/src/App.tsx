import { useState } from 'react';
import './App.css';
import { BacklogPanel } from './components/BacklogPanel';
import { QuestionsPanel } from './components/QuestionsPanel';

type Tab = 'questions' | 'backlog';

function App() {
  const [tab, setTab] = useState<Tab>('questions');

  return (
    <div className="app">
      <header className="app__header">
        <h1>Flights Orchestrator</h1>
        <nav className="app__tabs">
          <button
            type="button"
            className={tab === 'questions' ? 'active' : ''}
            onClick={() => setTab('questions')}
          >
            Questions
          </button>
          <button
            type="button"
            className={tab === 'backlog' ? 'active' : ''}
            onClick={() => setTab('backlog')}
          >
            Backlog
          </button>
        </nav>
      </header>

      <main className="app__main">{tab === 'questions' ? <QuestionsPanel /> : <BacklogPanel />}</main>
    </div>
  );
}

export default App;
