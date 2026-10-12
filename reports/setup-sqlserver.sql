-- Copies the Payments API data into SQL Server so SSRS (Report Builder) can report on it.
-- Run this in SSMS against your SQL Server Express instance (localhost\SQLEXPRESS).
-- Generated from src/PaymentsApi/payments.db. Real-looking emails are replaced with example.com addresses.

IF DB_ID(N'PaymentsReporting') IS NULL CREATE DATABASE PaymentsReporting;
GO
USE PaymentsReporting;
GO

DROP VIEW IF EXISTS dbo.vw_OutstandingByCustomer;
DROP VIEW IF EXISTS dbo.vw_PaymentsByDay;
DROP TABLE IF EXISTS dbo.Refunds;
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.Invoices;
DROP TABLE IF EXISTS dbo.Customers;
GO

CREATE TABLE dbo.Customers (
    Id INT NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NOT NULL
);

CREATE TABLE dbo.Invoices (
    Id INT NOT NULL PRIMARY KEY,
    CustomerId INT NOT NULL REFERENCES dbo.Customers(Id),
    Description NVARCHAR(500) NOT NULL,
    TotalCents BIGINT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE dbo.Payments (
    Id INT NOT NULL PRIMARY KEY,
    InvoiceId INT NOT NULL REFERENCES dbo.Invoices(Id),
    AmountCents BIGINT NOT NULL,
    IdempotencyKey NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE dbo.Refunds (
    Id INT NOT NULL PRIMARY KEY,
    PaymentId INT NOT NULL REFERENCES dbo.Payments(Id),
    AmountCents BIGINT NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO

INSERT INTO dbo.Customers VALUES (1, N'Maple Leaf Coffee', N'billing@mapleleafcoffee.example');
INSERT INTO dbo.Customers VALUES (2, N'Lakeshore Bikes', N'accounts@lakeshorebikes.example');
INSERT INTO dbo.Customers VALUES (3, N'Krystals Nails', N'krystalsnails@example.com');
INSERT INTO dbo.Customers VALUES (4, N'E2E Customer 1791595960316-457', N'e2e-1791595960316-457@example.com');
INSERT INTO dbo.Customers VALUES (5, N'E2E Customer 1791596579457-227', N'e2e-1791596579457-227@example.com');
INSERT INTO dbo.Customers VALUES (6, N'E2E Customer 1791596580618-715', N'e2e-1791596580618-715@example.com');
INSERT INTO dbo.Invoices VALUES (1, 1, N'POS terminal rental - September', 12500, N'PartiallyPaid', '2026-10-04 03:57:10.706');
INSERT INTO dbo.Invoices VALUES (2, 1, N'Card processing fees - September', 4275, N'Paid', '2026-10-04 03:57:10.744');
INSERT INTO dbo.Invoices VALUES (3, 2, N'Online checkout setup', 29900, N'PartiallyPaid', '2026-10-04 03:57:10.744');
INSERT INTO dbo.Invoices VALUES (4, 2, N'Card', 11100, N'PartiallyPaid', '2026-10-04 03:58:59.145');
INSERT INTO dbo.Invoices VALUES (5, 3, N'Card', 111100, N'PartiallyPaid', '2026-10-10 01:26:02.817');
INSERT INTO dbo.Invoices VALUES (6, 4, N'E2E test invoice', 1000, N'Open', '2026-10-10 01:32:40.825');
INSERT INTO dbo.Invoices VALUES (7, 5, N'E2E test invoice', 5000, N'Paid', '2026-10-10 01:42:59.983');
INSERT INTO dbo.Invoices VALUES (8, 6, N'E2E test invoice', 1000, N'Open', '2026-10-10 01:43:00.865');
INSERT INTO dbo.Payments VALUES (1, 1, 5000, NULL, '2026-10-04 03:57:10.706');
INSERT INTO dbo.Payments VALUES (2, 3, 29900, N'556a904d-2a0f-4366-b249-723d835901b4', '2026-10-04 03:58:01.558');
INSERT INTO dbo.Payments VALUES (3, 3, 23500, N'38d448ed-2005-4546-8a28-ab35840fd38d', '2026-10-04 03:58:46.213');
INSERT INTO dbo.Payments VALUES (4, 4, 11100, N'705e0900-ff27-47bd-8d6d-21529b8e795b', '2026-10-10 01:25:02.246');
INSERT INTO dbo.Payments VALUES (5, 5, 11100, N'51218c2d-ea36-498f-ba96-4bddd7500fa7', '2026-10-10 01:26:13.895');
INSERT INTO dbo.Payments VALUES (6, 5, 29900, N'0371ff3a-0d05-4fc0-bb0b-8e7cb9051848', '2026-10-10 01:26:19.413');
INSERT INTO dbo.Payments VALUES (7, 5, 11100, N'819420cb-78b7-4ba2-a6d2-c591f957b631', '2026-10-10 01:26:35.987');
INSERT INTO dbo.Payments VALUES (8, 5, 59000, N'dde0a194-8b08-41cd-b264-f4f274442770', '2026-10-10 01:26:46.746');
INSERT INTO dbo.Payments VALUES (9, 7, 5000, N'2c2bf31f-22e0-4da6-83cb-e8669444fc49', '2026-10-10 01:43:00.160');
INSERT INTO dbo.Payments VALUES (10, 1, 2500, N'import:BANK-2026-10-0001', '2026-10-11 04:06:46.000');
INSERT INTO dbo.Payments VALUES (11, 2, 4275, N'import:BANK-2026-10-0002', '2026-10-11 04:06:46.160');
INSERT INTO dbo.Payments VALUES (12, 1, 1000, N'csv-PA-0001', '2026-10-11 17:40:38.886');
INSERT INTO dbo.Payments VALUES (13, 3, 500, N'csv-PA-0002', '2026-10-11 17:40:39.309');
INSERT INTO dbo.Refunds VALUES (1, 2, 29900, N'Damaged', '2026-10-04 03:58:24.546');
INSERT INTO dbo.Refunds VALUES (2, 4, 1500, N'Damaged', '2026-10-10 01:25:16.410');
INSERT INTO dbo.Refunds VALUES (3, 5, 1500, N'Damaged', '2026-10-10 01:26:51.877');
GO

-- Report 1: what each customer still owes. Voided invoices are left out.
-- Paid = payments minus refunds, the same rule the API uses.
CREATE VIEW dbo.vw_OutstandingByCustomer AS
WITH InvoicePaid AS (
    SELECT i.Id AS InvoiceId,
           COALESCE((SELECT SUM(p.AmountCents) FROM dbo.Payments p WHERE p.InvoiceId = i.Id), 0)
         - COALESCE((SELECT SUM(r.AmountCents) FROM dbo.Refunds r JOIN dbo.Payments p ON p.Id = r.PaymentId WHERE p.InvoiceId = i.Id), 0) AS PaidCents
    FROM dbo.Invoices i
)
SELECT c.Name AS Customer,
       COUNT(i.Id) AS Invoices,
       CAST(SUM(i.TotalCents) / 100.0 AS DECIMAL(12,2)) AS Invoiced,
       CAST(SUM(ip.PaidCents) / 100.0 AS DECIMAL(12,2)) AS Paid,
       CAST(SUM(i.TotalCents - ip.PaidCents) / 100.0 AS DECIMAL(12,2)) AS Outstanding
FROM dbo.Customers c
JOIN dbo.Invoices i ON i.CustomerId = c.Id AND i.Status <> N'Void'
JOIN InvoicePaid ip ON ip.InvoiceId = i.Id
GROUP BY c.Name;
GO

-- Report 2: money in and out per day.
CREATE VIEW dbo.vw_PaymentsByDay AS
WITH Moves AS (
    SELECT CAST(CreatedAt AS DATE) AS Day, AmountCents AS PaymentCents, 0 AS RefundCents, 1 AS IsPayment FROM dbo.Payments
    UNION ALL
    SELECT CAST(CreatedAt AS DATE), 0, AmountCents, 0 FROM dbo.Refunds
)
SELECT Day,
       SUM(IsPayment) AS Payments,
       CAST(SUM(PaymentCents) / 100.0 AS DECIMAL(12,2)) AS Received,
       CAST(SUM(RefundCents) / 100.0 AS DECIMAL(12,2)) AS Refunded,
       CAST(SUM(PaymentCents - RefundCents) / 100.0 AS DECIMAL(12,2)) AS Net
FROM Moves
GROUP BY Day;
GO

-- Quick check: both should return rows.
SELECT * FROM dbo.vw_OutstandingByCustomer ORDER BY Outstanding DESC;
SELECT * FROM dbo.vw_PaymentsByDay ORDER BY Day;

