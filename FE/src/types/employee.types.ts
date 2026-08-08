export interface Employee {
  id: number;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  maxHoursPerWeek: number;
}

export interface EmployeeAssignment {
  id: number;
  employeeId: number;
  companyId: number;
  companyName?: string;
  departmentId: number;
  departmentName?: string;
  positionId: number;
  positionName?: string;
  certificateExpiryDate?: string;
  isPrimary: boolean;
  priorityOrder?: number;
  allowCrossDept: boolean;
  seniorityYears: number;
}

export interface EmployeeLeave {
  id: number;
  employeeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface EmployeePreference {
  id: number;
  employeeId: number;
  preferenceType: string;
  details: string;
}
