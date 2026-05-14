-- ======================================
-- NextHappen: Database-per-Service Init
-- ======================================
-- Este script crea las bases de datos individuales
-- y migra los datos desde la DB monolítica (nexthappen).
-- Ejecutar una sola vez después de tener la DB monolítica.

-- 1. Crear las bases de datos
CREATE DATABASE IF NOT EXISTS nexthappen_iam;
CREATE DATABASE IF NOT EXISTS nexthappen_events;
CREATE DATABASE IF NOT EXISTS nexthappen_tickets;
CREATE DATABASE IF NOT EXISTS nexthappen_engagement;
CREATE DATABASE IF NOT EXISTS nexthappen_notifications;

-- 2. Migrar datos existentes (si la DB monolítica tiene datos)
-- NOTA: Las tablas se crean automáticamente por EF Core al iniciar cada servicio.
--       Solo necesitas copiar los datos después del primer arranque.

-- Copiar usuarios
-- INSERT INTO nexthappen_iam.Users SELECT * FROM nexthappen.Users;

-- Copiar eventos y stands
-- INSERT INTO nexthappen_events.Events SELECT * FROM nexthappen.Events;
-- INSERT INTO nexthappen_events.AssignedStands SELECT * FROM nexthappen.AssignedStands;

-- Copiar tickets
-- INSERT INTO nexthappen_tickets.Tickets SELECT * FROM nexthappen.Tickets;

-- Copiar engagement
-- INSERT INTO nexthappen_engagement.SavedEvents SELECT * FROM nexthappen.SavedEvents;
-- INSERT INTO nexthappen_engagement.Metrics SELECT * FROM nexthappen.Metrics;

-- Copiar notificaciones
-- INSERT INTO nexthappen_notifications.Notifications SELECT * FROM nexthappen.Notifications;
