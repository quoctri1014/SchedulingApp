import axios from 'axios';
import type {
  ApiResponse,
  Company,
  Department,
  Position,
  Employee,
  EmployeeAssignment,
  Shift,
  MasterShift,
  ScheduleResult,
  SolverInput,
  User,
  PagedResult,
  AlgorithmRun,
  AlgorithmSummary
} from '../types';

const API_BASE_URL = 'http://localhost:5000/api/v1';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Generic response extractor
const extractData = <T>(res: { data: ApiResponse<T> }): T => res.data.data as T;

export const api = {
  // Companies
  getCompanies: () => apiClient.get<ApiResponse<Company[]>>('/companies').then(extractData),
  
  // Departments
  getDepartments: () => apiClient.get<ApiResponse<Department[]>>('/departments').then(extractData),
  
  // Positions
  getPositions: () => apiClient.get<ApiResponse<Position[]>>('/positions').then(extractData),
  
  // Employees
  getEmployees: () => apiClient.get<ApiResponse<Employee[]>>('/employees').then(extractData),
  
  // Employee Assignments
  getEmployeeAssignments: (empId: number) => apiClient.get<ApiResponse<EmployeeAssignment[]>>(`/employees/${empId}/assignments`).then(extractData),
  
  // Shifts
  getShifts: () => apiClient.get<ApiResponse<Shift[]>>('/shifts').then(extractData),
  createShift: (shift: Omit<Shift, 'id'>) => apiClient.post<ApiResponse<Shift>>('/shifts', shift).then(extractData),
  updateShift: (id: string, shift: Shift) => apiClient.put<ApiResponse<Shift>>(`/shifts/${id}`, shift).then(extractData),
  
  // Master Shifts
  getMasterShifts: () => apiClient.get<ApiResponse<MasterShift[]>>('/CompanyShifts').then(extractData),
  
  // Scheduling
  runSchedule: (input: SolverInput, algorithm: string = 'greedy') => 
    apiClient.post<ApiResponse<ScheduleResult>>(`/schedule/run?algorithm=${algorithm}`, input).then(extractData),

  runAllSchedule: (runsPerAlgorithm: number = 1) =>
    apiClient.post<ApiResponse<AlgorithmRun[]>>('/schedule/run-all', { runsPerAlgorithm }).then(extractData),

  // Users (FaceAttendanceSystem)
  getUsers: (params?: { search?: string; companyId?: string; departmentId?: string; pageNumber?: number; pageSize?: number }) =>
    apiClient.get<ApiResponse<PagedResult<User>>>('/users', { params }).then(extractData),

  getUserById: (id: string) =>
    apiClient.get<ApiResponse<User>>(`/users/${id}`).then(extractData),

  // Algorithm Runs History & Metrics
  getAlgorithmRuns: (algorithm?: string) =>
    apiClient.get<ApiResponse<AlgorithmRun[]>>('/algorithmruns', { params: { algorithm } }).then(extractData),

  getLatestRunsByAlgorithm: () =>
    apiClient.get<ApiResponse<AlgorithmRun[]>>('/algorithmruns/latest-by-algorithm').then(extractData),

  getAlgorithmSummary: () =>
    apiClient.get<ApiResponse<AlgorithmSummary[]>>('/algorithmruns/summary').then(extractData),

  exportAlgorithmRunsCSV: () =>
    `${API_BASE_URL}/algorithmruns/export`,

  // Dashboard & Health
  getDashboardSummary: () =>
    apiClient.get<ApiResponse<any>>('/dashboard/summary').then(extractData),

  getHealthCheck: () =>
    apiClient.get<ApiResponse<any>>('/health').then(extractData),
};
