# Sprint 4 — Pagos con Stripe, QR, Reembolsos, Ventas y Reseñas

Este sprint convierte NextHappen en una plataforma **realmente vendible**: el cobro de
entradas se hace con **Stripe** (modo prueba), las entradas se emiten con **código QR**
validable en la puerta, se pueden **reembolsar**, el organizador tiene un **panel de
ventas** y los usuarios pueden dejar **reseñas y calificaciones**.

## Funcionalidades nuevas

| Feature | Backend | Frontend |
|--------|---------|----------|
| Cobro real con Stripe Checkout | `ticket-service` (`PaymentService`, `PaymentController`) | Botón "Comprar" → redirección a Stripe · páginas éxito/cancelado |
| Emisión de entradas SOLO tras pago confirmado | Webhook `checkout.session.completed` → emite tickets | — |
| Entrada digital con QR | `GET /api/tickets/{id}/qr` (PNG) | QR en "Mis Entradas" |
| Validación en la puerta | `POST /api/tickets/validate` (Organizer/Admin) | `/org/validate` |
| Reembolsos | `POST /api/tickets/{id}/refund` (Stripe Refunds) | Botón "Reembolsar" en "Mis Entradas" |
| Liberación de cupos | `POST /api/events/{id}/release` (expiración/reembolso) | — |
| Panel de ventas | `GET /api/events/{id}/sales`, `POST /api/sales/summary` | `/org/sales` |
| Reseñas y calificaciones | `engagement-service` (`ReviewService`, `ReviewsController`) | Sección de reseñas en el detalle del evento |
| Notificación de compra | `notification-service` consume `TicketPurchasedEvent` | Se ve en Notificaciones |

## Flujo de pago (por qué es correcto)

```
Frontend  →  POST /api/payments/checkout  →  ticket-service
                                              1. reserva cupos (bloqueo pesimista)
                                              2. crea Order = Pending
                                              3. crea Stripe Checkout Session
          ←  { checkoutUrl }
Frontend  →  redirige a Stripe  →  usuario paga (tarjeta 4242 4242 4242 4242)
Stripe    →  webhook checkout.session.completed  →  ticket-service
                                              4. Order = Paid
                                              5. emite N entradas con QR
                                              6. publica TicketPurchasedEvent (RabbitMQ)
Stripe    →  webhook checkout.session.expired    →  libera cupos, Order = Failed
```

**El dinero nunca se "cobra" sin que Stripe lo confirme**, y las entradas **nunca se
emiten antes del pago**. El webhook es idempotente (reintentos no duplican entradas).

## Configuración de Stripe (modo prueba)

1. Crea una cuenta gratis en <https://dashboard.stripe.com> y activa el **modo Test**.
2. Copia tus claves de prueba en <https://dashboard.stripe.com/test/apikeys>:
   - **Secret key** (`sk_test_...`)
3. En la raíz de `next-happen-backend`, copia `.env.example` a `.env` y completa:
   ```env
   STRIPE_SECRET_KEY=sk_test_xxx
   STRIPE_CURRENCY=pen
   FRONTEND_ORIGIN=http://localhost:5173
   # STRIPE_WEBHOOK_SECRET se completa en el paso siguiente
   ```
4. Instala la **Stripe CLI** (<https://stripe.com/docs/stripe-cli>) e inicia el reenvío
   de webhooks hacia el gateway:
   ```bash
   stripe login
   stripe listen --forward-to http://localhost:5000/api/payments/webhook
   ```
   La CLI imprime un **webhook signing secret** (`whsec_...`). Cópialo a tu `.env`:
   ```env
   STRIPE_WEBHOOK_SECRET=whsec_xxx
   ```
5. Reinicia `ticket-service` para que tome el secreto (`docker compose up -d ticket-service`).

> **Tarjeta de prueba:** `4242 4242 4242 4242`, cualquier fecha futura, cualquier CVC y ZIP.

## Cómo levantar todo

```bash
# 1) Backend (desde next-happen-backend/)
cp .env.example .env        # y completa las claves de Stripe
docker compose up -d --build

# 2) Reenvío de webhooks (en otra terminal)
stripe listen --forward-to http://localhost:5000/api/payments/webhook

# 3) Frontend (desde next-happen-frontend/)
npm install
npm run dev                 # http://localhost:5173  (VITE_API_URL=http://localhost:5000)
```

## ⚠️ Importante: reinicia el esquema de la base de datos

El proyecto usa `EnsureCreated()` (no migraciones). Como el **ticket-service** ahora
tiene una tabla nueva (`Orders`) y columnas nuevas en `Tickets` (`QrCode`, `OrderId`,
`Price`, `ValidatedAt`), y **engagement-service** una tabla nueva (`Reviews`), si ya
habías corrido el proyecto antes debes **recrear el volumen de MySQL** para que se
genere el esquema actualizado:

```bash
docker compose down -v      # ⚠️ borra los datos de MySQL (dev)
docker compose up -d --build
```

## Endpoints nuevos (vía gateway :5000)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/payments/checkout` | Usuario | Inicia el checkout; devuelve `checkoutUrl` |
| POST | `/api/payments/webhook` | Stripe (firma) | Recibe eventos de Stripe |
| GET  | `/api/tickets/{id}/qr` | Dueño/Admin | PNG del QR |
| POST | `/api/tickets/validate` | Organizer/Admin | Valida un QR en la puerta |
| POST | `/api/tickets/{id}/refund` | Dueño/Admin | Reembolsa y libera cupo |
| GET  | `/api/events/{id}/sales` | Organizer/Admin | Resumen de ventas del evento |
| POST | `/api/sales/summary` | Organizer/Admin | Resumen de varios eventos |
| POST | `/api/events/{id}/release` | interno | Devuelve cupos al inventario |
| GET  | `/api/events/{id}/reviews` | público | Reseñas + promedio + distribución |
| POST | `/api/events/{id}/reviews` | Usuario | Crea/actualiza reseña |

## Prueba rápida end-to-end

1. Inicia sesión como **usuario**, entra a un evento con precio y pulsa **Comprar**.
2. Paga en Stripe con `4242 4242 4242 4242`.
3. Vuelves a la app (página de éxito). En **Mis Entradas** aparece la entrada con su **QR**.
4. Inicia sesión como **organizador** → **Validar entradas** (icono QR) y pega el código
   del QR (visible en la respuesta del ticket) → debe decir "Ingreso permitido"; al
   repetir, "Entrada ya utilizada".
5. En **Ventas** (icono $) el organizador ve ingresos, entradas vendidas y validadas.
6. Desde **Mis Entradas**, el usuario puede **Reembolsar** una entrada activa.
