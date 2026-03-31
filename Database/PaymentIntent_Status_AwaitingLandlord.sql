USE MotelDb;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PaymentIntents_Status')
BEGIN
    ALTER TABLE dbo.PaymentIntents DROP CONSTRAINT CK_PaymentIntents_Status;
END
GO

ALTER TABLE dbo.PaymentIntents
ADD CONSTRAINT CK_PaymentIntents_Status
CHECK ([Status] IN (
    N'created',
    N'pending',
    N'awaiting_landlord',
    N'succeeded',
    N'failed',
    N'cancelled',
    N'expired'
));
GO

-- Dữ liệu cũ: VietQR đã báo "tôi đã thanh toán" nhưng chưa có payment succeeded/rejected
UPDATE pi
SET pi.Status = N'awaiting_landlord'
FROM dbo.PaymentIntents AS pi
WHERE pi.Provider = N'vietqr'
  AND pi.Status = N'succeeded'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.Payments AS p
      WHERE p.PaymentIntentId = pi.PaymentIntentId
        AND p.Status IN (N'succeeded', N'rejected')
  );
GO