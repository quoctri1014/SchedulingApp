export interface AlgorithmRun {
  id: string;
  algorithm: string;
  datasetSize: string;
  startedAt: string;
  completedAt: string;
  executionTimeMs: number;
  totalShifts: number;
  filledShifts: number;
  unfilledShifts: number;
  totalPenaltyScore: number;
  hardViolationsCount: number;
  softViolationsCount: number;
  penaltyBreakdownJson?: string;
  ranBy: string;
}

export interface AlgorithmSummary {
  algorithm: string;
  runCount: number;
  avgExecutionTimeMs: number;
  avgPenaltyScore: number;
  bestPenaltyScore: number;
  avgAccuracyPercentage: number;
  avgFilledPercentage?: number;
  lastRunAt: string;
}
