-- =====================================================================
--  NextHappen — Actualización de esquema para producción (Aiven MySQL)
--  Aplica los cambios de las funcionalidades nuevas SIN borrar datos.
--  Ejecutar UNA sola vez. Si algo ya existe, MySQL avisará con un error
--  inofensivo (significa que ya estaba aplicado).
--
--  Cómo conectarse a Aiven (SSL obligatorio):
--    mysql --host <HOST> --port <PORT> --user avnadmin \
--          --password=<PASS> --ssl-mode=REQUIRED
--  Luego pega este archivo, o córrelo por bloques en el editor de Aiven
--  seleccionando la base de datos correspondiente en cada sección.
-- =====================================================================


-- ============================================================
--  1) BASE DE DATOS DE TICKETS  →  nexthappen_tickets
--     - Columnas nuevas en Tickets (QR, código corto, etc.)
--     - Tabla nueva Orders (pedidos de Stripe)
-- ============================================================
USE nexthappen_tickets;

ALTER TABLE Tickets
  ADD COLUMN OrderId      char(36)      NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
  ADD COLUMN Price        decimal(10,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN QrCode       varchar(128)  NULL,
  ADD COLUMN ShortCode    varchar(16)   NULL,
  ADD COLUMN ValidatedAt  datetime(6)   NULL,
  ADD UNIQUE INDEX IX_Tickets_QrCode    (QrCode),
  ADD UNIQUE INDEX IX_Tickets_ShortCode (ShortCode),
  ADD INDEX        IX_Tickets_EventId   (EventId),
  ADD INDEX        IX_Tickets_OrderId   (OrderId);

CREATE TABLE IF NOT EXISTS Orders (
  Id                     char(36)      NOT NULL,
  UserId                 char(36)      NOT NULL,
  EventId                char(36)      NOT NULL,
  Quantity               int           NOT NULL,
  UnitPrice              decimal(10,2) NOT NULL,
  TotalAmount            decimal(10,2) NOT NULL,
  Currency               varchar(10)   NOT NULL,
  StripeSessionId        varchar(255)  NOT NULL,
  StripePaymentIntentId  varchar(255)  NULL,
  Status                 varchar(50)   NOT NULL,
  CreatedAt              datetime(6)   NOT NULL,
  PaidAt                 datetime(6)   NULL,
  PRIMARY KEY (Id),
  UNIQUE INDEX IX_Orders_StripeSessionId (StripeSessionId)
);


-- ============================================================
--  2) BASE DE DATOS DE ENGAGEMENT  →  nexthappen_engagement
--     - Tabla nueva Reviews (reseñas y calificaciones)
--     - No toca SavedEvents ni Metrics (favoritos/métricas se conservan)
-- ============================================================
USE nexthappen_engagement;

CREATE TABLE IF NOT EXISTS Reviews (
  Id         char(36)      NOT NULL,
  EventId    char(36)      NOT NULL,
  UserId     char(36)      NOT NULL,
  UserName   varchar(150)  NULL,
  Rating     int           NOT NULL,
  Comment    varchar(1000) NULL,
  CreatedAt  datetime(6)   NOT NULL,
  PRIMARY KEY (Id),
  INDEX IX_Reviews_EventId (EventId),
  UNIQUE INDEX IX_Reviews_UserId_EventId (UserId, EventId)
);
