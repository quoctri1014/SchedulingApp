USE FaceAttendanceSystem;

EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

DELETE FROM UserDepartmentAssignments;
DELETE FROM AttendanceAdjustments;
DELETE FROM ApprovalWorkflowScopes;
DELETE FROM ApprovalWorkflowSteps;
DELETE FROM ShiftAssignments;
DELETE FROM RequestApprovals;
DELETE FROM AttendancePolicyScopes;
DELETE FROM Notifications;
DELETE FROM ShiftRosters;
DELETE FROM UserAttendanceMethodPermissions;
DELETE FROM ActivityLogs;
DELETE FROM ShiftRosterDetails;
DELETE FROM RefreshTokens;
DELETE FROM UserFCMTokens;
DELETE FROM UserWorksiteAssignments;
DELETE FROM EmployeeShiftOverrides;
DELETE FROM UserDeviceMappings;
DELETE FROM LeaveBalances;
DELETE FROM FaceProfiles;
DELETE FROM UserAuthAccounts;
DELETE FROM LeaveBalanceTransactions;
DELETE FROM LeaveImports;
DELETE FROM FaceVerificationLogs;
DELETE FROM AttendanceLogs;
DELETE FROM Requests;
DELETE FROM UserCompanyAssignments;
DELETE FROM AttendanceDailyResults;

DELETE FROM Users;
DELETE FROM Departments;
DELETE FROM CompanyShifts;
DELETE FROM DepartmentJobTitles;
DELETE FROM DepartmentOverrides;
DELETE FROM ShiftRosters;

EXEC sp_MSForEachTable 'ALTER TABLE ? WITH NOCHECK CHECK CONSTRAINT ALL';
