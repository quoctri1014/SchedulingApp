SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- 1. Khai báo danh sách Họ, Đệm, Tên thực tế
DECLARE @Ho TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20));
INSERT INTO @Ho (Name) VALUES 
(N'Nguyễn'), (N'Trần'), (N'Lê'), (N'Phạm'), (N'Hoàng'), (N'Huỳnh'), 
(N'Vũ'), (N'Võ'), (N'Đặng'), (N'Bùi'), (N'Đỗ'), (N'Hồ'), (N'Ngô'), (N'Dương'), (N'Lý');

DECLARE @Dem TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20), Gender NVARCHAR(10));
INSERT INTO @Dem (Name, Gender) VALUES 
(N'Văn', N'Male'), (N'Thành', N'Male'), (N'Minh', N'Male'), (N'Quốc', N'Male'), (N'Đức', N'Male'), (N'Hữu', N'Male'), (N'Tấn', N'Male'),
(N'Thị', N'Female'), (N'Thanh', N'Female'), (N'Phương', N'Female'), (N'Ngọc', N'Female'), (N'Thảo', N'Female'), (N'Mai', N'Female'), (N'Kim', N'Female');

DECLARE @Ten TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(20));
INSERT INTO @Ten (Name) VALUES 
(N'An'), (N'Bình'), (N'Cường'), (N'Dũng'), (N'Hùng'), (N'Kiệt'), (N'Long'), (N'Nam'), (N'Phong'), (N'Quân'), (N'Sơn'), (N'Tâm'), (N'Tấn'), (N'Thành'), (N'Triết'),
(N'Anh'), (N'Bảo'), (N'Châu'), (N'Duyên'), (N'Hương'), (N'Linh'), (N'Mai'), (N'Nhung'), (N'Trinh'), (N'Thảo'), (N'Trang'), (N'Vân'), (N'Yến'), (N'Nhi'), (N'Quyên');

-- 2. Lấy danh sách ID Công ty và Phòng ban hiện có trong DB
DECLARE @Companies TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER);
INSERT INTO @Companies (Id) SELECT Id FROM FaceAttendanceSystem.dbo.Companies WHERE IsActive = 1;

DECLARE @Departments TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER, CompanyId UNIQUEIDENTIFIER);
INSERT INTO @Departments (Id, CompanyId) SELECT Id, CompanyId FROM FaceAttendanceSystem.dbo.Departments WHERE IsActive = 1;

DECLARE @CompanyCount INT = (SELECT COUNT(*) FROM @Companies);
DECLARE @DeptCount INT = (SELECT COUNT(*) FROM @Departments);
DECLARE @RoleId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM FaceAttendanceSystem.dbo.SystemRoles);

-- Khai báo biến dùng trong vòng lặp
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

-- 3. Vòng lặp tạo 1.000 User người thật
WHILE @i <= 1000
BEGIN
    SET @UserId = NEWID();
    SET @UserCode = CONCAT(N'NV', RIGHT(CONCAT(N'0000', @i), 4));
    
    -- Random Họ
    SET @RandomHoId = ABS(CHECKSUM(NEWID())) % 15 + 1;
    SET @RandomHo = (SELECT Name FROM @Ho WHERE Id = @RandomHoId);
    
    -- Random Đệm & Giới tính
    DELETE FROM @RandomDemRow;
    INSERT INTO @RandomDemRow SELECT TOP 1 Name, Gender FROM @Dem ORDER BY NEWID();
    SET @RandomDem = (SELECT Name FROM @RandomDemRow);
    SET @Gender = (SELECT Gender FROM @RandomDemRow);
    
    -- Random Tên
    SET @RandomTenId = ABS(CHECKSUM(NEWID())) % 30 + 1;
    SET @RandomTen = (SELECT Name FROM @Ten WHERE Id = @RandomTenId);
    
    -- Gắn thông tin
    SET @FullName = CONCAT(@RandomHo, N' ', @RandomDem, N' ', @RandomTen);
    SET @Email = CONCAT(LOWER(REPLACE(@RandomTen, N' ', N'')), N'.', LOWER(REPLACE(@RandomHo, N' ', N'')), @i, N'@trungnamgroup.com.vn');
    SET @Phone = CONCAT(N'09', RIGHT(CONCAT(N'00000000', ABS(CHECKSUM(NEWID())) % 100000000), 8));

    -- Random chọn 1 Công ty và 1 Phòng ban thuộc Công ty đó
    SET @SelectedCompanySeq = (@i % @CompanyCount) + 1;
    SET @SelectedCompanyId = (SELECT Id FROM @Companies WHERE Seq = @SelectedCompanySeq);
    SET @SelectedDeptId = (SELECT TOP 1 Id FROM @Departments WHERE CompanyId = @SelectedCompanyId ORDER BY NEWID());
    
    IF @SelectedDeptId IS NULL 
        SET @SelectedDeptId = (SELECT TOP 1 Id FROM @Departments ORDER BY NEWID());

    -- Chèn vào bảng Users
    INSERT INTO FaceAttendanceSystem.dbo.Users 
        (Id, Code, FullName, Email, Phone, AvatarUrl, DateOfBirth, Gender, IsActive, CreatedAt, UpdatedAt, RoleId, CreatedBy, UpdatedBy)
    VALUES 
        (@UserId, @UserCode, @FullName, @Email, @Phone, NULL, DATEADD(DAY, - (ABS(CHECKSUM(NEWID())) % 7000 + 7000), GETDATE()), @Gender, 1, GETDATE(), NULL, @RoleId, NULL, NULL);

    -- Chèn vào bảng UserCompanyAssignments
    INSERT INTO FaceAttendanceSystem.dbo.UserCompanyAssignments 
        (Id, UserId, CompanyId, IsPrimaryCompany, EffectiveFrom, EffectiveTo, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
    VALUES 
        (NEWID(), @UserId, @SelectedCompanyId, 1, '2026-01-01', NULL, 1, GETDATE(), NULL, NULL, NULL);

    -- Chèn vào bảng UserDepartmentAssignments
    INSERT INTO FaceAttendanceSystem.dbo.UserDepartmentAssignments 
        (Id, UserId, CompanyId, DepartmentId, JobTitleId, DirectManagerUserId, IsPrimaryDepartment, EffectiveFrom, EffectiveTo, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
    VALUES 
        (NEWID(), @UserId, @SelectedCompanyId, @SelectedDeptId, NULL, NULL, 1, '2026-01-01', NULL, 1, GETDATE(), NULL, NULL, NULL);

    SET @i = @i + 1;
END;
