export interface SolverInput {
  shiftIds: number[];
  employeeIds: string[];
  fromDate: string;
  toDate: string;
  options: Record<string, any>;
}

export interface AssignedEmployee {
  employeeId: string;
  employeeName: string;
}

export interface ScheduleResult {
  schedule: Record<string, AssignedEmployee[]>;
  totalShifts: number;
  filledShifts: number;
  unfilledShifts: number;
  executionTimeMs: number;
  totalPenaltyScore: number;
  hardViolationsCount: number;
  softViolationsCount: number;
  penaltyBreakdown: Record<string, number>;
  algorithmRunId?: string;
}
