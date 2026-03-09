USE MotelDb;
GO

ALTER TABLE dbo.PaymentIntents
DROP CONSTRAINT CK_PaymentIntents_Provider;
GO

ALTER TABLE dbo.PaymentIntents
ADD CONSTRAINT CK_PaymentIntents_Provider
CHECK (Provider IN ('cash', 'payos', 'vietqr'));
GO