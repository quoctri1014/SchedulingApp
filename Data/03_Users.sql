SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- 1. Khai báo danh sách H?, Ð?m, Tên th?c t?
DECLARE @Ho TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20));
INSERT INTO @Ho (Name) VALUES 
(N'Nguy?n'), (N'Tr?n'), (N'Lê'), (N'Ph?m'), (N'Hoàng'), (N'Hu?nh'), 
(N'Vu'), (N'Võ'), (N'Ð?ng'), (N'Bùi'), (N'Ð?'), (N'H?'), (N'Ngô'), (N'Duong'), (N'Lý');

DECLARE @Dem TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20), Gender NVARCHAR(10));
INSERT INTO @Dem (Name, Gender) VALUES 
(N'Van', N'Male'), (N'Thành', N'Male'), (N'Minh', N'Male'), (N'Qu?c', N'Male'), (N'Ð?c', N'Male'), (N'H?u', N'Male'), (N'T?n', N'Male'),
(N'Th?', N'Female'), (N'Thanh', N'Female'), (N'Phuong', N'Female'), (N'Ng?c', N'Female'), (N'Th?o', N'Female'), (N'Mai', N'Female'), (N'Kim', N'Female');

DECLARE @Ten TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20));
INSERT INTO @Ten (Name) VALUES 
(N'An'), (N'Bình'), (N'Cu?ng'), (N'Dung'), (N'Hùng'), (N'Ki?t'), (N'Long'), (N'Nam'), (N'Phong'), (N'Quân'), (N'Son'), (N'Tâm'), (N'T?n'), (N'Thành'), (N'Tri?t'),
(N'Anh'), (N'B?o'), (N'Châu'), (N'Duyên'), (N'Huong'), (N'Linh'), (N'Mai'), (N'Nhung'), (N'Trinh'), (N'Th?o'), (N'Trang'), (N'Vân'), (N'Y?n'), (N'Nhi'), (N'Quyên');

-- 2. L?y danh sách ID Công ty và Phòng ban hi?n có trong DB
DECLARE @Companies TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER);
INSERT INTO @Companies (Id) SELECT Id FROM SchedulingAppDb.dbo.Companies WHERE IsActive = 1;

DECLARE @Departments TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER, CompanyId UNIQUEIDENTIFIER);
INSERT INTO @Departments (Id, CompanyId) SELECT Id, CompanyId FROM SchedulingAppDb.dbo.Departments WHERE IsActive = 1;

DECLARE @CompanyCount INT = (SELECT COUNT(*) FROM @Companies);
DECLARE @DeptCount INT = (SELECT COUNT(*) FROM @Departments);
DECLARE @RoleId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM SchedulingAppDb.dbo.SystemRoles);

-- Khai báo bi?n dùng trong vòng l?p
DECLARE @i INT = 1;
DECLARE @UserId UNIQUEIDENTIFIER;
DECLARE @UserCode NVARCHAR(50);
DECLARE @RandomHoId INT;
DECLARE @RandomHo NVARCHAR(20);
DECLARE @RandomDemRow TABLE (Name NVARCHAR(20), Gender NVARCHAR(10));
DECLARE @RandomDem NVARCHAR(20);
DECLARE @Gender NVARCHAR(10);
DECLARE @RandomTenId INT;
DECLARE @RandomTen NVARCHAR(20);
DECLARE @FullName NVARCHAR(100);
DECLARE @Email NVARCHAR(100);
DECLARE @Phone NVARCHAR(20);
DECLARE @SelectedCompanySeq INT;
DECLARE @SelectedCompanyId UNIQUEIDENTIFIER;
DECLARE @SelectedDeptId UNIQUEIDENTIFIER;

-- 3. Vòng l?p t?o 1.000 User ngu?i th?t
WHILE @i <= 1000
BEGIN
    SET @UserId = NEWID();
    SET @UserCode = CONCAT(N'NV', RIGHT(CONCAT(N'0000', @i), 4));
    
    -- Random H?
    SET @RandomHoId = ABS(CHECKSUM(NEWID())) % 15 + 1;
    SET @RandomHo = (SELECT Name FROM @Ho WHERE Id = @RandomHoId);
    
    -- Random Ð?m & Gi?i tính
    DELETE FROM @RandomDemRow;
    INSERT INTO @RandomDemRow SELECT TOP 1 Name, Gender FROM @Dem ORDER BY NEWID();
    SET @RandomDem = (SELECT Name FROM @RandomDemRow);
    SET @Gender = (SELECT Gender FROM @RandomDemRow);
    
    -- Random Tên
    SET @RandomTenId = ABS(CHECKSUM(NEWID())) % 30 + 1;
    SET @RandomTen = (SELECT Name FROM @Ten WHERE Id = @RandomTenId);
    
    -- G?n thông tin
    SET @FullName = CONCAT(@RandomHo, N' ', @RandomDem, N' ', @RandomTen);
    SET @Email = CONCAT(LOWER(REPLACE(@RandomTen, N' ', N'')), N'.', LOWER(REPLACE(@RandomHo, N' ', N'')), @i, N'@trungnamgroup.com.vn');
    SET @Phone = CONCAT(N'09', RIGHT(CONCAT(N'00000000', ABS(CHECKSUM(NEWID())) % 100000000), 8));

    -- Random ch?n 1 Công ty và 1 Phòng ban thu?c Công ty dó
    SET @SelectedCompanySeq = (@i % @CompanyCount) + 1;
    SET @SelectedCompanyId = (SELECT Id FROM @Companies WHERE Seq = @SelectedCompanySeq);
    SET @SelectedDeptId = (SELECT TOP 1 Id FROM @Departments WHERE CompanyId = @SelectedCompanyId ORDER BY NEWID());
    
    IF @SelectedDeptId IS NULL 
        SET @SelectedDeptId = (SELECT TOP 1 Id FROM @Departments ORDER BY NEWID());

    -- Chèn vào b?ng Users
    INSERT INTO SchedulingAppDb.dbo.Users 
        (Id, Code, FullName, Email, Phone, AvatarUrl, DateOfBirth, Gender, IsActive, CreatedAt, UpdatedAt, RoleId, CreatedBy, UpdatedBy)
    VALUES 
        (@UserId, @UserCode, @FullName, @Email, @Phone, NULL, DATEADD(DAY, - (ABS(CHECKSUM(NEWID())) % 7000 + 7000), GETDATE()), @Gender, 1, GETDATE(), NULL, @RoleId, NULL, NULL);

    -- Chèn vào b?ng UserCompanyAssignments
    INSERT INTO SchedulingAppDb.dbo.UserCompanyAssignments 
        (Id, UserId, CompanyId, IsPrimaryCompany, EffectiveFrom, EffectiveTo, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
    VALUES 
        (NEWID(), @UserId, @SelectedCompanyId, 1, '2026-01-01', NULL, 1, GETDATE(), NULL, NULL, NULL);

    -- Chèn vào b?ng UserDepartmentAssignments
    INSERT INTO SchedulingAppDb.dbo.UserDepartmentAssignments 
        (Id, UserId, CompanyId, DepartmentId, JobTitleId, DirectManagerUserId, IsPrimaryDepartment, EffectiveFrom, EffectiveTo, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
    VALUES 
        (NEWID(), @UserId, @SelectedCompanyId, @SelectedDeptId, NULL, NULL, 1, '2026-01-01', NULL, 1, GETDATE(), NULL, NULL, NULL);

    SET @i = @i + 1;
END;

