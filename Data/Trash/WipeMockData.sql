USE FaceAttendanceSystem;

EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Xóa toàn bộ Users (cả 7 user gốc vì là mock)
DELETE FROM Users;

-- Xóa toàn bộ Departments (vì là mock)
DELETE FROM Departments;

-- Xóa các bảng liên kết
DELETE FROM UserCompanyAssignments;
DELETE FROM UserDepartmentAssignments;
DELETE FROM CompanyShifts;
DELETE FROM AttendanceDailyResults;
DELETE FROM ApprovalWorkflows;

EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
