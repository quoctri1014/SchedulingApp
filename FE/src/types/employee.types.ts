export interface Employee {
  id: string;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  maxHoursPerWeek: number;
}

export interface EmployeeAssignment {
  id: number;
  employeeId: string;
  companyId: string;
  companyName?: string;
  departmentId: string;
  departmentName?: string;
  positionId: string;
  positionName?: string;
  certificateExpiryDate?: string;
  isPrimary: boolean;
  priorityOrder?: number;
  allowCrossDept: boolean;
  seniorityYears: number;
}

export interface EmployeeLeave {
  id: number;
  employeeId: string;
  startTime: string;
  endTime: string;
  isApproved: boolean;
}

export interface EmployeePreference {
  id: number;
  employeeId: string;
  preferenceType: string;
  details: string;
}
