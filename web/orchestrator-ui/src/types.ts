export interface PendingQuestion {
  id: string;
  projectName: string;
  backlogTaskId: string | null;
  source: 'Architect' | 'BlockedTaskEscalation';
  questionText: string;
  contextJson: string | null;
  status: 'Open' | 'Answered';
  answerText: string | null;
  createdAt: string;
  answeredAt: string | null;
}

export interface BacklogTask {
  id: string;
  projectName: string;
  description: string;
  acceptanceCriteria: string;
  dependsOnTaskIds: string[];
  status: 'Pending' | 'InProgress' | 'Blocked' | 'NeedsInput' | 'Done';
  iterationCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface IterationRecord {
  iterationNumber: number;
  developerSummaryJson: string;
  reviewerVerdictJson: string | null;
  createdAt: string;
}

export interface BacklogTaskDetail {
  task: BacklogTask;
  iterations: IterationRecord[];
}
