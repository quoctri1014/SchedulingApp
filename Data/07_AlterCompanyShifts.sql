USE FaceAttendanceSystem;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyShifts]') AND name = 'IsOvernight')
BEGIN
    ALTER TABLE [dbo].[CompanyShifts] ADD [IsOvernight] BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyShifts]') AND name = 'BreakIsOvernight')
BEGIN
    ALTER TABLE [dbo].[CompanyShifts] ADD [BreakIsOvernight] BIT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyShifts]') AND name = 'MaxOvertimeBufferMinutes')
BEGIN
    ALTER TABLE [dbo].[CompanyShifts] ADD [MaxOvertimeBufferMinutes] INT NOT NULL DEFAULT 240;
END
GO
