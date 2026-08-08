USE FaceAttendanceSystem;
GO

-- Clear old data if needed (so we can safely insert)
DELETE FROM [dbo].[CompanyShifts];
GO

DECLARE @CompanyId UNIQUEIDENTIFIER = '6FB8C349-2032-490A-9B96-9A0B7D6CC4FB';

INSERT INTO [dbo].[CompanyShifts] 
    ([Id], [CompanyId], [Name], [ShiftStart], [ShiftEnd], [LunchStart], [LunchEnd], [IsDefault], [CreatedAt], [UpdatedAt], [IsActive], [CreatedBy], [UpdatedBy], [IsOvernight], [BreakIsOvernight], [MaxOvertimeBufferMinutes]) 
VALUES
     -- 1. KHỐI VĂN PHÒNG & HÀNH CHÍNH
     (NEWID(), @CompanyId, N'Ca Hành chính Tiêu chuẩn', '08:00:00.0000000', '17:00:00.0000000', '12:00:00.0000000', '13:00:00.0000000', 1, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240),
     (NEWID(), @CompanyId, N'Ca Hành chính Sớm', '07:30:00.0000000', '16:30:00.0000000', '11:30:00.0000000', '12:30:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240),
     (NEWID(), @CompanyId, N'Ca Hành chính Muộn', '08:30:00.0000000', '17:30:00.0000000', '12:30:00.0000000', '13:30:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240),
     (NEWID(), @CompanyId, N'Ca Sáng (Nửa ngày)', '08:00:00.0000000', '12:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 120),
     (NEWID(), @CompanyId, N'Ca Chiều (Nửa ngày)', '13:00:00.0000000', '17:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 120),

     -- 2. KHỐI NHÀ MÁY / VẬN HÀNH / SẢN XUẤT (3 CA 4 KÍP)
     (NEWID(), @CompanyId, N'Ca 1 - Sáng (Nhà máy/O&M)', '06:00:00.0000000', '14:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 240),
     (NEWID(), @CompanyId, N'Ca 2 - Chiều (Nhà máy/O&M)', '14:00:00.0000000', '22:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 240),
     (NEWID(), @CompanyId, N'Ca 3 - Đêm (Xuyên đêm)', '22:00:00.0000000', '06:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 1, NULL, 360),

     -- 3. KHỐI CÔNG TRƯỜNG & XÂY DỰNG
     (NEWID(), @CompanyId, N'Ca Công trường Ngày', '07:00:00.0000000', '17:00:00.0000000', '11:30:00.0000000', '13:00:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 360),
     (NEWID(), @CompanyId, N'Ca Thi công Đêm', '18:00:00.0000000', '04:00:00.0000000', '23:00:00.0000000', '00:00:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 1, 1, 360),

     -- 4. KHỐI CẢNG BIỂN & KHAI THÁC KHO BÃI
     (NEWID(), @CompanyId, N'Ca Cảng biển Ngày (12 tiếng)', '07:00:00.0000000', '19:00:00.0000000', '11:30:00.0000000', '12:30:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240),
     (NEWID(), @CompanyId, N'Ca Cảng biển Đêm (12 tiếng)', '19:00:00.0000000', '07:00:00.0000000', '23:30:00.0000000', '00:30:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 1, 1, 360),

     -- 5. KHỐI RESORT, DU LỊCH & SÂN GOLF
     (NEWID(), @CompanyId, N'Ca Sân Golf Sớm', '05:30:00.0000000', '14:00:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 240),
     (NEWID(), @CompanyId, N'Ca Sân Golf Trưa', '11:00:00.0000000', '19:30:00.0000000', NULL, NULL, 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, NULL, 240),
     (NEWID(), @CompanyId, N'Ca Lễ tân/Phục vụ Sáng', '06:00:00.0000000', '14:30:00.0000000', '11:30:00.0000000', '12:00:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240),
     (NEWID(), @CompanyId, N'Ca Lễ tân/Phục vụ Chiều', '14:00:00.0000000', '22:30:00.0000000', '18:00:00.0000000', '18:30:00.0000000', 0, GETDATE(), GETDATE(), 1, @CompanyId, @CompanyId, 0, 0, 240);
GO
