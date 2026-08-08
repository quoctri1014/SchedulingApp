export interface MasterShift {
  id: string;
  name: string;
  companyId?: string;
  shiftStart: string;
  shiftEnd: string;
  isOvernight: boolean;
}

export interface Shift {
  id: string;
  masterShiftId?: string;
  companyId: string;
  companyName?: string;
  departmentId: string;
  departmentName?: string;
  positionId: string;
  positionName?: string;
  startDate: string;
  endDate: string;
  requiredEmployeeCount: number;
}
