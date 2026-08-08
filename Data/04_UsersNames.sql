USE FaceAttendanceSystem;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

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

-- Get all User IDs to iterate
DECLARE @UserIds TABLE (Seq INT IDENTITY(1,1), Id UNIQUEIDENTIFIER);
INSERT INTO @UserIds (Id) SELECT Id FROM Users;

DECLARE @Count INT = (SELECT COUNT(*) FROM @UserIds);
DECLARE @i INT = 1;
DECLARE @CurrentId UNIQUEIDENTIFIER;

WHILE @i <= @Count
BEGIN
    SET @CurrentId = (SELECT Id FROM @UserIds WHERE Seq = @i);
    
    DECLARE @RandomHoId INT = ABS(CHECKSUM(NEWID())) % 15 + 1;
    DECLARE @RandomHo NVARCHAR(20) = (SELECT Name FROM @Ho WHERE Id = @RandomHoId);
    
    DECLARE @RandomDemRow TABLE (Name NVARCHAR(20), Gender NVARCHAR(10));
    DELETE FROM @RandomDemRow;
    INSERT INTO @RandomDemRow SELECT TOP 1 Name, Gender FROM @Dem ORDER BY NEWID();
    DECLARE @RandomDem NVARCHAR(20) = (SELECT Name FROM @RandomDemRow);
    DECLARE @Gender NVARCHAR(10) = (SELECT Gender FROM @RandomDemRow);
    
    DECLARE @RandomTenId INT = ABS(CHECKSUM(NEWID())) % 30 + 1;
    DECLARE @RandomTen NVARCHAR(20) = (SELECT Name FROM @Ten WHERE Id = @RandomTenId);
    
    DECLARE @FullName NVARCHAR(100) = CONCAT(@RandomHo, N' ', @RandomDem, N' ', @RandomTen);
    
    UPDATE Users
    SET FullName = @FullName,
        Gender = @Gender
    WHERE Id = @CurrentId;
    
    SET @i = @i + 1;
END
