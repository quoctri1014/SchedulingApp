export * from './company.types';
export * from './employee.types';
export * from './shift.types';
export * from './schedule.types';
export * from './algorithmRun.types';
export * from './constants';

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  errors: string[];
}

export interface User {
  id: string;
  code?: string;
  name?: string;
  fullName?: string;
  email?: string;
  phone?: string;
  isActive?: boolean;
  companyName?: string;
  departmentName?: string;
  companyAssignments?: any[];
  departmentAssignments?: any[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
