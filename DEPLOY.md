# NextHappen — Guía de Re-despliegue

Guía paso a paso para actualizar **NextHappen** en producción. Pensada para que
**cualquier integrante con los accesos** pueda hacerlo sin conocer el detalle interno.

> **Arquitectura de despliegue**
> - **Frontend:** Vue 3 → **Vercel** (se despliega solo al hacer push a `main`).
> - **Backend:** microservicios .NET 9 en Docker → **servidor Linux (Oracle)** vía `docker compose`.
> - **Base de datos:** **MySQL 8 en Aiven** (gestionada).
> - **Pagos:** **Stripe** (webhook público hacia el gateway).

---

## 0. Requisitos previos (accesos que necesitas)

| Recurso | Qué necesitas |
|---------|---------------|
| **GitHub** (org `1ASI0657-2610-7940-Venti`) | Ser **colaborador** de los repos `next-happen-backend` y `next-happen-frontend` (permiso de push). |
| **Servidor Oracle** | Acceso **SSH** al servidor Linux donde corre el backend. |
| **Aiven** | Host, puerto, usuario (`avnadmin`) y contraseña de la instancia MySQL. |
| **Stripe** | Acceso al **dashboard** (modo test) para el webhook y las claves. |
| **Vercel** | Acceso al proyecto del frontend (para variables de entorno). |

Herramientas en tu máquina: `git`, y para conectarte a la BD el cliente `mysql`
(o usar el editor SQL web de Aiven).

---

## ⚠️ Orden de despliegue (IMPORTANTE)

Sigue este orden. **El backend va primero**; si publicas el frontend antes, las
pantallas nuevas llamarán a endpoints que el backend viejo aún no tiene.

```
1) Push del backend  →  2) Oracle (pull + rebuild)  →  3) Aiven (esquema + seed)
   →  4) Stripe (webhook)  →  5) Push del frontend (Vercel)  →  6) Verificación
```

---

## 1. Publicar el backend (GitHub)

```bash
git clone https://github.com/1ASI0657-2610-7940-Venti/next-happen-backend.git
cd next-happen-backend
# (si ya lo tienes clonado)
git checkout master
git pull

# Si tienes commits locales sin subir:
git push origin master
```

> **Si el push da error 403 "denied":** tu Git está autenticado con una cuenta sin
> permiso. Reautentícate con tu cuenta (colaboradora) usando `gh auth login` +
> `gh auth setup-git`, o limpia la credencial vieja en el *Administrador de credenciales
> de Windows*.
>
> **Si el push lo bloquea "secret detected":** revisa que no haya claves reales en el
> código. El `.env.example` debe tener valores VACÍOS; las claves reales van solo en el
> `.env` del servidor (gitignorado).

---

## 2. Actualizar el backend en el servidor Oracle (SSH)

```bash
ssh <usuario>@<ip-del-servidor-oracle>

cd ~/next-happen-backend        # ruta donde está clonado el repo
git pull origin master
```

### 2.1 Configurar el `.env` del servidor

El `.env` **no está en el repo** (contiene secretos). Verifica/edita que tenga:

```env
# ── JWT (idéntico en todos los servicios) ──
JWT_KEY=<clave_fuerte_min_32_bytes>
JWT_ISSUER=nexthappen
JWT_AUDIENCE=nexthappen-users

# ── Base de datos MySQL en Aiven (una cadena por servicio) ──
DB_CONNECTION_IAM=server=<host-aiven>;port=<puerto>;database=nexthappen_iam;user=avnadmin;password=<pass>;SslMode=Required
DB_CONNECTION_EVENTS=server=<host-aiven>;port=<puerto>;database=nexthappen_events;user=avnadmin;password=<pass>;SslMode=Required
DB_CONNECTION_TICKETS=server=<host-aiven>;port=<puerto>;database=nexthappen_tickets;user=avnadmin;password=<pass>;SslMode=Required
DB_CONNECTION_ENGAGEMENT=server=<host-aiven>;port=<puerto>;database=nexthappen_engagement;user=avnadmin;password=<pass>;SslMode=Required
DB_CONNECTION_NOTIFICATIONS=server=<host-aiven>;port=<puerto>;database=nexthappen_notifications;user=avnadmin;password=<pass>;SslMode=Required

# ── RabbitMQ (contenedor propio o servicio gestionado tipo CloudAMQP) ──
RABBITMQ_HOST=<host>
RABBITMQ_USER=<user>
RABBITMQ_PASS=<pass>
RABBITMQ_VHOST=/
RABBITMQ_USE_SSL=false

# ── Frontend (para CORS y el redirect de Stripe) ──
FRONTEND_ORIGIN=https://<tu-dominio-vercel>      # ej. https://next-happen.vercel.app

# ── Stripe (modo test) ──
STRIPE_SECRET_KEY=sk_test_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx                  # se completa en el paso 4
STRIPE_CURRENCY=pen
```

### 2.2 Reconstruir y levantar

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Esto reconstruye **todos** los servicios, incluido el **gateway** (que trae el fix de
enrutamiento necesario para reseñas, ventas y "Mis Entradas").

Verifica que todo esté arriba:
```bash
docker compose -f docker-compose.prod.yml ps
```

---

## 3. Actualizar la base de datos en Aiven

El proyecto usa `EnsureCreated` (no migraciones), así que **las tablas/columnas nuevas
hay que aplicarlas a mano una vez**. Los scripts están en `scripts/`.

### 3.1 Esquema (obligatorio) — tablas `Orders`, `Reviews`, columnas de QR/código

```bash
mysql --host <host-aiven> --port <puerto> --user avnadmin \
      --password=<pass> --ssl-mode=REQUIRED < scripts/aiven-schema-update.sql
```
> No borra datos (usa `ALTER` / `CREATE TABLE IF NOT EXISTS`). Ejecutar **una sola vez**;
> si algo ya existe, MySQL da un error inofensivo.

### 3.2 Datos demo (opcional) — usuarios + eventos de demostración

```bash
mysql --host <host-aiven> --port <puerto> --user avnadmin \
      --password=<pass> --ssl-mode=REQUIRED < scripts/seed-demo.sql
```
Credenciales que crea:
- Organizador: `organizador@nexthappen.demo` / `Demo1234!`
- Usuario: `usuario@nexthappen.demo` / `Demo1234!`

> **Alternativa vía API** (más robusta, no requiere cliente SQL):
> `node scripts/seed-demo.mjs https://<tu-backend>`

---

## 4. Configurar el webhook de Stripe

1. Entra a **https://dashboard.stripe.com/test/webhooks** → **Add endpoint**.
2. **Endpoint URL:** `https://<tu-dominio-backend>/api/payments/webhook`
3. **Eventos:** `checkout.session.completed` y `checkout.session.expired`.
4. Guarda y usa **"Reveal signing secret"** para copiar el `whsec_...`.
5. Pega ese valor en el `.env` del servidor (`STRIPE_WEBHOOK_SECRET=whsec_...`) y reinicia:
   ```bash
   docker compose -f docker-compose.prod.yml up -d ticket-service
   ```

> **Nota:** aunque el webhook falle, el pago igual funciona gracias al *confirm-on-return*
> (la app confirma el pago al volver de Stripe). El webhook es el mecanismo autoritativo
> recomendado para producción.

---

## 5. Publicar el frontend (Vercel)

### 5.1 Verificar variables de entorno en Vercel (una sola vez)

En **Vercel → tu proyecto → Settings → Environment Variables**, confirma que existan
(porque el `.env.production` ya no va en el repo):

| Variable | Valor |
|----------|-------|
| `VITE_API_URL` | `/proxy` |
| `VITE_GOOGLE_MAPS_API_KEY` | `<tu-api-key-de-google-maps>` |

Y en `vercel.json` (dentro del repo), confirma que el `destination` apunte a tu backend:
```json
{ "source": "/proxy/(.*)", "destination": "https://<tu-dominio-backend>/$1" }
```

### 5.2 Publicar

```bash
cd next-happen-frontend
git checkout main
git pull
git push origin main        # Vercel redespliega automáticamente al detectar el push
```

Sigue el progreso en el dashboard de Vercel hasta que el deploy quede en **Ready**.

---

## 6. Verificación post-despliegue

### 6.1 Backend (reemplaza `<API>` por tu dominio de backend)

```bash
API=https://<tu-dominio-backend>

# Health del gateway
curl -s -o /dev/null -w "gateway: %{http_code}\n" $API/health

# Eventos públicos (deberían listar los eventos)
curl -s $API/api/events/public | head -c 200

# Login demo
curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"Email":"usuario@nexthappen.demo","Password":"Demo1234!"}'
```

### 6.2 Frontend (en el navegador)

1. Abre `https://<tu-dominio-vercel>` → el **home** debe mostrar los eventos con imágenes.
2. Inicia sesión como `usuario@nexthappen.demo` / `Demo1234!`.
3. Entra a un evento → **Comprar** → paga con la tarjeta de prueba `4242 4242 4242 4242`
   (fecha futura, cualquier CVC/ZIP).
4. Al volver, deberías ver **Mis Entradas** con el QR y el código corto.
5. Como **organizador** (`organizador@nexthappen.demo`): revisa **Ventas** (icono $) y
   **Validar entradas** (icono QR) → ingresa el código de la entrada → "Ingreso permitido".

Si los pasos 1–5 pasan, el re-despliegue está completo. ✅

---

## 7. Solución de problemas

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| `git push` → **403 denied** | Cuenta sin permiso o credencial equivocada | `gh auth login` con la cuenta colaboradora, o limpiar credencial en Windows. |
| Push bloqueado por **"secret detected"** | Una clave real en el código | Quitar la clave; usar placeholders vacíos en `.env.example`. |
| Compras se cobran pero **no aparece la entrada** | Webhook no configurado | Registrar el webhook (paso 4) o confiar en el *confirm-on-return* (ya incluido). |
| Reseñas / Ventas / "Mis Entradas" dan **404** | Gateway sin el fix de ruteo | Reconstruir el `api-gateway` (paso 2.2). |
| Error de **columna/tabla desconocida** (`Orders`, `ShortCode`, `Reviews`) | Falta el esquema en Aiven | Correr `scripts/aiven-schema-update.sql` (paso 3.1). |
| Tras pagar te lleva a **localhost** | `FRONTEND_ORIGIN` mal configurado | Ponerlo a tu dominio de Vercel en el `.env` del servidor y reiniciar `ticket-service`. |
| El **mapa** no carga | Falta la API key de Google Maps | Setear `VITE_GOOGLE_MAPS_API_KEY` en Vercel. |
| CORS bloqueado en el navegador | `FRONTEND_ORIGIN` no coincide con la URL real | Igualar `FRONTEND_ORIGIN` a la URL exacta de Vercel. |

---

## 8. Rollback (si algo sale mal)

- **Backend (Oracle):** vuelve al commit anterior y reconstruye.
  ```bash
  git log --oneline -5          # identifica el commit estable anterior
  git checkout <commit-estable>
  docker compose -f docker-compose.prod.yml up -d --build
  ```
- **Frontend (Vercel):** en el dashboard, **Deployments → deploy anterior → Promote to
  Production** (rollback en un clic).
- **Base de datos:** los scripts son aditivos y no destructivos; no requieren rollback.
  (No ejecutes `DROP DATABASE` en producción salvo que sepas exactamente qué haces.)

---

## Referencia rápida de scripts (`scripts/`)

| Script | Qué hace | Cuándo |
|--------|----------|--------|
| `aiven-schema-update.sql` | Crea tablas/columnas nuevas (Orders, Reviews, QR/código) | Paso 3.1 (una vez) |
| `seed-demo.sql` | Puebla usuarios + eventos demo (SQL) | Paso 3.2 (opcional) |
| `seed-demo.mjs` | Igual, pero vía API | Alternativa al 3.2 |

Documentación relacionada: `SPRINT4.md` (funcionalidades), `ARCHITECTURE.md` (arquitectura).
