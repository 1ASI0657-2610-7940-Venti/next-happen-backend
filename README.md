# 🎉 NextHappen Platform

**NextHappen** es una plataforma de gestión de eventos en tiempo real que permite a organizadores crear, administrar y publicar eventos, mientras los usuarios pueden descubrir, guardar y comprar tickets.

## 🏗️ Arquitectura

El proyecto utiliza una **arquitectura de microservicios** con DDD (Domain-Driven Design), donde cada servicio es independiente, tiene su propia base de datos lógica y se comunica a través de contratos compartidos.

```
nexthappen-platform/
├── shared/
│   └── NextHappen.Contracts/        # Eventos de dominio compartidos
├── services/
│   ├── iam-service/                  # Autenticación y usuarios
│   ├── event-service/                # CRUD de eventos y stands
│   ├── ticket-service/               # Compra de tickets (próximo)
│   ├── engagement-service/           # Guardados y métricas (próximo)
│   └── notification-service/         # Notificaciones (próximo)
├── gateway/                          # API Gateway (próximo)
├── nexthappen-backend/               # Monolito original (legacy)
├── NextHappen.sln                    # Solución .NET
└── docker-compose.yml                # Orquestación local (próximo)
```

## 🔧 Tech Stack

| Componente | Tecnología |
|-----------|------------|
| Backend | .NET 9.0 (C#) |
| Base de datos | MySQL 8.0 |
| ORM | Entity Framework Core 9 + Pomelo |
| Auth | JWT (Bearer Token) |
| Documentación API | Swagger (Swashbuckle) |
| Frontend | Vue.js 3 + Vite |
| Mensajería | RabbitMQ (próximo) |

## 📦 Microservicios

### 🔑 IAM Service (`iam-service`) — Puerto 5001
Gestión de identidad y acceso: registro, login, JWT y perfil de usuario.

**Endpoints:**
| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/auth/register` | Registrar usuario |
| `POST` | `/api/auth/login` | Iniciar sesión (devuelve JWT) |
| `GET` | `/api/users/{id}` | Obtener perfil |
| `PUT` | `/api/users/{id}` | Actualizar perfil |

### 📅 Event Service (`event-service`) — Puerto 5002
CRUD completo de eventos, descubrimiento de eventos públicos y gestión de stands.

**Endpoints:**
| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/events` | Crear evento |
| `GET` | `/api/events` | Listar todos los eventos |
| `GET` | `/api/events/public` | Eventos públicos (discovery) |
| `GET` | `/api/events/{id}` | Detalle de evento |
| `PUT` | `/api/events/{id}` | Actualizar evento 🔒 |
| `DELETE` | `/api/events/{id}` | Eliminar evento 🔒 |
| `GET` | `/api/events/{id}/stands` | Stands del evento |
| `POST` | `/api/events/{id}/stands` | Asignar stand |
| `PUT` | `/api/stands/{id}` | Actualizar stand |
| `DELETE` | `/api/stands/{id}` | Eliminar stand |

> 🔒 = Requiere JWT con rol `Organizer` o `Admin`

### 🎫 Ticket Service (`ticket-service`) — Puerto 5003
Compra y gestión de tickets para eventos.

**Endpoints:**
| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/events/{id}/tickets/purchase` | Comprar tickets |
| `GET` | `/api/users/{id}/tickets` | Tickets del usuario |
| `GET` | `/api/tickets/{id}` | Detalle de ticket |
| `DELETE` | `/api/tickets/{id}` | Cancelar ticket |

### 📊 Engagement Service (`engagement-service`) — Puerto 5004
Eventos guardados y métricas de interacción.

**Endpoints:**
| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/users/{id}/saved-events/{eventId}` | Guardar evento 🔒 |
| `DELETE` | `/api/users/{id}/saved-events/{eventId}` | Quitar guardado 🔒 |
| `GET` | `/api/users/{id}/saved-events` | Eventos guardados 🔒 |
| `POST` | `/api/metrics` | Registrar métrica |
| `GET` | `/api/metrics` | Listar métricas |
| `POST` | `/api/metrics/event-view/{eventId}` | Registrar vista de evento |

### 🔔 Notification Service (`notification-service`) — Puerto 5005
Notificaciones al organizador del evento.

**Endpoints:**
| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/notifications/{userId}` | Notificaciones del usuario |
| `POST` | `/api/notifications` | Crear notificación |
| `POST` | `/api/notifications/{id}/read` | Marcar como leída |

### 🌐 API Gateway (`api-gateway`) — Puerto 5000
Punto de entrada único que redirige las peticiones al microservicio correcto usando YARP.

El frontend solo necesita apuntar a `http://localhost:5000` y el gateway se encarga de distribuir el tráfico.

| Ruta | Servicio destino |
|------|-----------------|
| `/api/auth/*` | iam-service (:5001) |
| `/api/users/*` | iam-service (:5001) |
| `/api/events/*` | event-service (:5002) |
| `/api/stands/*` | event-service (:5002) |
| `/api/tickets/*` | ticket-service (:5003) |
| `/api/users/*/saved-events/*` | engagement-service (:5004) |
| `/api/metrics/*` | engagement-service (:5004) |
| `/api/notifications/*` | notification-service (:5005) |

## 🚀 Cómo correr el proyecto

### Opción 1: Docker Compose (recomendado)

Un solo comando levanta todo: MySQL + 5 microservicios + API Gateway.

```bash
docker-compose up --build
```

| Servicio | URL |
|----------|-----|
| API Gateway | http://localhost:5000 |
| IAM Service | http://localhost:5001 |
| Event Service | http://localhost:5002 |
| Ticket Service | http://localhost:5003 |
| Engagement Service | http://localhost:5004 |
| Notification Service | http://localhost:5005 |
| MySQL | localhost:3307 |

```bash
# Detener todo
docker-compose down

# Detener y borrar volúmenes (reset DB)
docker-compose down -v
```

### Opción 2: Manual (desarrollo local)

### Prerequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [MySQL 8.0](https://dev.mysql.com/downloads/) corriendo en `localhost:3306`
- Base de datos `nexthappen` creada

```sql
CREATE DATABASE IF NOT EXISTS nexthappen;
```

### Correr un servicio individual

```bash
# IAM Service (puerto 5001)
cd services/iam-service
dotnet run

# Event Service (puerto 5002)
cd services/event-service
dotnet run

# Ticket Service (puerto 5003)
cd services/ticket-service
dotnet run

# Engagement Service (puerto 5004)
cd services/engagement-service
dotnet run

# Notification Service (puerto 5005)
cd services/notification-service
dotnet run

# API Gateway (puerto 5000) — iniciar al final
cd gateway/api-gateway
dotnet run
```

> **Tip:** Inicia primero todos los microservicios y luego el gateway. El frontend solo necesita conectarse a `http://localhost:5000`.

### Correr el monolito original (legacy)

```bash
cd nexthappen-backend
dotnet run
# Corre en http://localhost:5022
```

### Acceder a Swagger UI

- **IAM Service:** http://localhost:5001/swagger
- **Event Service:** http://localhost:5002/swagger
- **Monolito (legacy):** http://localhost:5022/swagger

## 🔐 Autenticación

Todos los servicios comparten la misma clave JWT, lo que significa que un token generado por `iam-service` es válido en `event-service` y cualquier otro servicio.

**Flujo:**
1. `POST /api/auth/login` en `iam-service` → Recibe un JWT
2. Usar ese JWT en el header `Authorization: Bearer <token>` para endpoints protegidos en cualquier servicio

## 🌿 Gitflow

El proyecto sigue **Gitflow** con **Conventional Commits**:

- `master` — Rama de producción
- `develop` — Rama de desarrollo activo
- `feature/*` — Ramas de nuevas funcionalidades

**Formato de commits:**
```
feat(scope): descripción
fix(scope): descripción
chore(scope): descripción
refactor(scope): descripción
docs(scope): descripción
```

## 📝 Frontend

El frontend (Vue.js) se encuentra en una carpeta separada y se conecta al backend a través de la variable de entorno `VITE_API_URL`. Actualmente apunta al monolito en `http://localhost:5022`.

> **Nota:** Durante la migración a microservicios, el frontend continuará usando el monolito. Una vez que se implemente el API Gateway, solo será necesario cambiar `VITE_API_URL` a la URL del gateway.

## 📋 Estado de la Migración

| Servicio | Estado |
|---------|--------|
| ✅ IAM Service | Completado |
| ✅ Event Service | Completado |
| ✅ Ticket Service | Completado |
| ✅ Engagement Service | Completado |
| ✅ Notification Service | Completado |
| ✅ API Gateway | Completado |

## 👥 Equipo

Proyecto desarrollado como parte de la plataforma NextHappen.

---

*Generado con ❤️ usando .NET 9 y arquitectura de microservicios*
