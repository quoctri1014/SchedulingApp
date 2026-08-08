USE FaceAttendanceSystem;

EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

DELETE FROM UserCompanyAssignments;
DELETE FROM UserDepartmentAssignments;
DELETE FROM CompanyShifts;
DELETE FROM Departments;
DELETE FROM Companies;

DELETE FROM Users WHERE CreatedAt >= '2026-08-08 00:00:00';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = '00000000-0000-0000-0000-000000000000')
BEGIN
    INSERT INTO Users (Id, Code, FullName, Email, IsActive, CreatedAt)
    VALUES ('00000000-0000-0000-0000-000000000000', 'ADMIN', 'System Administrator', 'admin@local', 1, GETDATE());
END

EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
