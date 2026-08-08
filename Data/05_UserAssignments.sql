USE FaceAttendanceSystem;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Companies TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER);
INSERT INTO @Companies (Id) SELECT Id FROM Companies WHERE IsActive = 1;

DECLARE @Departments TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER, CompanyId UNIQUEIDENTIFIER);
INSERT INTO @Departments (Id, CompanyId) SELECT Id, CompanyId FROM Departments WHERE IsActive = 1;

DECLARE @CompanyCount INT = (SELECT COUNT(*) FROM @Companies);
DECLARE @DeptCount INT = (SELECT COUNT(*) FROM @Departments);

DECLARE @UserIds TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER);
INSERT INTO @UserIds (Id) SELECT Id FROM Users;
DECLARE @Count INT = (SELECT COUNT(*) FROM @UserIds);
DECLARE @i INT = 1;

WHILE @i <= @Count
BEGIN
    DECLARE @UserId UNIQUEIDENTIFIER = (SELECT Id FROM @UserIds WHERE Seq = @i);
    
    -- Random Company
    DECLARE @SelectedCompanySeq INT = (@i % @CompanyCount) + 1;
    DECLARE @SelectedCompanyId UNIQUEIDENTIFIER = (SELECT Id FROM @Companies WHERE Seq = @SelectedCompanySeq);
    
    -- Random Dept in that company
    DECLARE @SelectedDeptId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM @Departments WHERE CompanyId = @SelectedCompanyId ORDER BY NEWID());
    IF @SelectedDeptId IS NULL 
        SET @SelectedDeptId = (SELECT TOP 1 Id FROM @Departments ORDER BY NEWID());

    -- Assign Company
    IF NOT EXISTS (SELECT 1 FROM UserCompanyAssignments WHERE UserId = @UserId AND CompanyId = @SelectedCompanyId)
    BEGIN
        INSERT INTO UserCompanyAssignments (Id, UserId, CompanyId, IsPrimaryCompany, EffectiveFrom, IsActive, CreatedAt)
        VALUES (NEWID(), @UserId, @SelectedCompanyId, 1, '2026-01-01', 1, GETDATE());
    END

    -- Assign Dept
    IF NOT EXISTS (SELECT 1 FROM UserDepartmentAssignments WHERE UserId = @UserId AND DepartmentId = @SelectedDeptId)
    BEGIN
        INSERT INTO UserDepartmentAssignments (Id, UserId, CompanyId, DepartmentId, IsPrimaryDepartment, EffectiveFrom, IsActive, CreatedAt)
        VALUES (NEWID(), @UserId, @SelectedCompanyId, @SelectedDeptId, 1, '2026-01-01', 1, GETDATE());
    END
    
    SET @i = @i + 1;
END
