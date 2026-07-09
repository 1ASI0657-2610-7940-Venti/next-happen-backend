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
│   ├── ticket-service/               # Compra de tickets
│   ├── engagement-service/           # Guardados y métricas
│   └── notification-service/         # Notificaciones
├── gateway/                          # API Gateway (YARP + JWT + rate limiting)
├── NextHappen.sln                    # Solución .NET
├── docker-compose.yml                # Orquestación local (dev)
└── docker-compose.prod.yml           # Orquestación producción
```

> El monolito original (`nexthappen-backend/`) fue **eliminado**: toda su
> funcionalidad ya está cubierta por los microservicios (los contextos de
> Metrics y SavedEvents viven en `engagement-service`).

## 🔧 Tech Stack

| Componente | Tecnología |
|-----------|------------|
| Backend | .NET 9.0 (C#) |
| Base de datos | MySQL 8.0 |
| ORM | Entity Framework Core 9 + Pomelo |
| Auth | JWT (Bearer Token) |
| Documentación API | Swagger (Swashbuckle) |
| Frontend | Vue.js 3 + Vite |
| Mensajería | RabbitMQ + MassTransit |
| Gateway | YARP + JWT perimetral + Rate limiting |

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

### Acceder a Swagger UI

- **IAM Service:** http://localhost:5001/swagger
- **Event Service:** http://localhost:5002/swagger
- Cada microservicio expone su propio `/swagger` y un `/health` (chequeo de BD).

## 🔐 Autenticación

Todos los servicios comparten la misma clave JWT, lo que significa que un token generado por `iam-service` es válido en `event-service` y cualquier otro servicio.

**Flujo:**
1. `POST /api/auth/login` en `iam-service` → Recibe un JWT
2. Usar ese JWT en el header `Authorization: Bearer <token>` para endpoints protegidos en cualquier servicio

El **API Gateway también valida el JWT** en el perímetro para las rutas sensibles
(`/api/users`, `/api/stands`, `/api/tickets`, `/api/*/saved-events`, `/api/notifications`),
mediante la política de autorización `authenticated`. Las rutas públicas
(`/api/auth`, `GET /api/events`, `/api/metrics`) permanecen abiertas. Los
microservicios mantienen su propia validación como defensa en profundidad.

## 🔐 Seguridad y configuración (IMPORTANTE)

**Ningún secreto real se versiona en el repositorio.** Los `appsettings.json`
solo contienen valores de **desarrollo** (clave JWT `DEV_ONLY_...`, `password=admin`
local, RabbitMQ `guest`). En producción, TODO se inyecta por variables de entorno.

### Puesta en marcha

```bash
cp .env.example .env      # completa valores reales; .env está en .gitignore
docker-compose up --build # dev: usa defaults si no defines .env
```

Variables clave (ver `.env.example`): `MYSQL_ROOT_PASSWORD`, `JWT_KEY` (≥32 bytes,
idéntica en todos los servicios y el gateway), `JWT_ISSUER`, `JWT_AUDIENCE`,
`FRONTEND_ORIGIN` (CORS), credenciales `RABBITMQ_*`, y para prod las cadenas
`DB_CONNECTION_*`. Genera la clave JWT con `openssl rand -base64 48`.

### ⚠️ Rotar credenciales expuestas

Estas credenciales estuvieron commiteadas en el historial de git y **deben rotarse**:
la clave JWT anterior, la contraseña de MySQL, las credenciales de CloudAMQP
(`moose.rmq.cloudamqp.com`) y la API key de Google Maps del frontend.

### Otras medidas aplicadas

- **CORS** con whitelist configurable (`Cors:AllowedOrigins`), no `AllowAnyOrigin`.
- **Rate limiting** en el gateway (100 req/min por IP).
- **Init de BD**: `EnsureCreated()` sólo si `Database:AutoCreate=true`; en fallo hace
  *fail-fast* (relanza la excepción) en vez de arrancar con una BD rota. En prod,
  ponlo en `false` y aplica migraciones EF de forma controlada (pendiente de adoptar).
- **`/health`** por servicio (verifica conexión a la BD) → útil para orquestadores.

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

El frontend (Vue.js) se encuentra en una carpeta separada y se conecta al backend
a través de la variable de entorno `VITE_API_URL`, que apunta al **API Gateway**
(`http://localhost:5000` en desarrollo).

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
