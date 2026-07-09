/**
 * Seed de datos DEMO para NextHappen — vía API (no SQL directo).
 * Crea un organizador, un usuario y varios eventos publicados con imágenes.
 *
 * Uso:
 *   node scripts/seed-demo.mjs                       # contra http://localhost:5000
 *   node scripts/seed-demo.mjs https://api.tudominio # contra producción
 *   SEED_BASE_URL=https://api.tudominio node scripts/seed-demo.mjs
 *
 * Es idempotente: si el organizador/usuario ya existen, inicia sesión; y no
 * vuelve a crear eventos cuyo título ya exista.
 */

const BASE = (process.env.SEED_BASE_URL || process.argv[2] || 'http://localhost:5000').replace(/\/$/, '')

const ORG = { fullName: 'Ferias Lima (Demo)', email: 'organizador@nexthappen.demo', password: 'Demo1234!', role: 'Organizer' }
const USER = { fullName: 'Dario Salcedo (Demo)', email: 'usuario@nexthappen.demo', password: 'Demo1234!', role: 'User' }

const img = (id) => `https://images.unsplash.com/photo-${id}?w=900&q=80&auto=format&fit=crop`

// Fechas futuras a partir de hoy.
const daysFromNow = (d, hour = 18) => {
  const dt = new Date()
  dt.setDate(dt.getDate() + d)
  dt.setHours(hour, 0, 0, 0)
  return dt.toISOString()
}

const EVENTS = [
  {
    title: 'Feria Gastronómica de Barranco',
    description: 'Más de 40 emprendimientos de comida peruana, food trucks, música en vivo y talleres de cocina. Un fin de semana para disfrutar los sabores de Lima.',
    category: 'Gastronomía', price: 25, quantity: 200,
    address: 'Parque Municipal de Barranco', location: '-12.1467,-77.0206|Parque Municipal de Barranco, Lima',
    photos: [img('1414235077428-338989a2e8c0'), img('1555939594-58d7cb561ad1')],
    start: daysFromNow(4), end: daysFromNow(5),
  },
  {
    title: 'Festival Indie: Sonidos de la Ciudad',
    description: 'Bandas emergentes de rock, indie y fusión andina en un escenario íntimo. Descubre el nuevo sonido limeño antes que nadie.',
    category: 'Música y Conciertos', price: 45, quantity: 300,
    address: 'Anfiteatro del Parque de la Exposición', location: '-12.0664,-77.0378|Parque de la Exposición, Cercado de Lima',
    photos: [img('1470229722913-7c0e2dbbafd3'), img('1501281668745-f7f57925c3b4')],
    start: daysFromNow(9), end: daysFromNow(9),
  },
  {
    title: 'Mercado de Arte y Diseño Independiente',
    description: 'Ilustradores, ceramistas y diseñadores exhiben y venden sus piezas. Charlas de portafolio y arte en vivo durante toda la jornada.',
    category: 'Arte y Diseño', price: 15, quantity: 150,
    address: 'Casa Cultural, Miraflores', location: '-12.1211,-77.0297|Av. Larco, Miraflores, Lima',
    photos: [img('1536924940846-227afb31e2a5'), img('1513151233558-d860c5398176')],
    start: daysFromNow(6), end: daysFromNow(7),
  },
  {
    title: 'Feria del Libro Emergente',
    description: 'Editoriales independientes, fanzines y clubes de lectura. Presentaciones de autores locales y firma de ejemplares.',
    category: 'Literatura', price: 10, quantity: 180,
    address: 'Biblioteca de Pueblo Libre', location: '-12.0740,-77.0630|Pueblo Libre, Lima',
    photos: [img('1481627834876-b7833e8f5570'), img('1524995997946-a1c2e315a42f')],
    start: daysFromNow(12), end: daysFromNow(14),
  },
  {
    title: 'Expo Café & Barismo',
    description: 'Cafés de especialidad de todo el Perú, catas guiadas y competencia de baristas. Aprende a preparar el café perfecto.',
    category: 'Gastronomía', price: 30, quantity: 120,
    address: 'San Isidro', location: '-12.0972,-77.0365|Calle Los Libertadores, San Isidro, Lima',
    photos: [img('1442512595331-e89e73853f31'), img('1447933601403-0c6688de566e')],
    start: daysFromNow(16), end: daysFromNow(16),
  },
  {
    title: 'NightMarket Tech & Makers',
    description: 'Comunidad maker, robótica, impresión 3D y videojuegos indie. Demostraciones, networking y charlas de innovación.',
    category: 'Tecnología', price: 20, quantity: 250,
    address: 'Surco', location: '-12.1550,-76.9917|Av. Caminos del Inca, Surco, Lima',
    photos: [img('1540575467063-178a50c2df87'), img('1531482615713-2afd69097998')],
    start: daysFromNow(20), end: daysFromNow(20),
  },
  {
    title: 'Feria de Emprendimiento Cultural',
    description: 'Colectivos y marcas locales con propuestas sostenibles. Moda, accesorios, deco y talleres para toda la familia.',
    category: 'Emprendimiento', price: 12, quantity: 220,
    address: 'Jesús María', location: '-12.0730,-77.0490|Campo de Marte, Jesús María, Lima',
    photos: [img('1556740738-b6a63e27c4df'), img('1519671482749-fd09be7ccebf')],
    start: daysFromNow(3), end: daysFromNow(3),
  },
  {
    title: 'Noche de Danza y Folklore',
    description: 'Espectáculo de danzas tradicionales peruanas con agrupaciones invitadas. Una celebración de nuestra cultura viva.',
    category: 'Cultural', price: 35, quantity: 200,
    address: 'Callao Monumental', location: '-12.0566,-77.1181|Callao Monumental, Callao',
    photos: [img('1533174072545-7a4b6ad7a6c3'), img('1508700115892-45ecd05ae2ad')],
    start: daysFromNow(11), end: daysFromNow(11),
  },
]

// ── HTTP helpers ──
async function post(path, body, token) {
  const res = await fetch(BASE + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: JSON.stringify(body),
  })
  const text = await res.text()
  let data; try { data = JSON.parse(text) } catch { data = text }
  return { ok: res.ok, status: res.status, data }
}
async function get(path, token) {
  const res = await fetch(BASE + path, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  const text = await res.text()
  let data; try { data = JSON.parse(text) } catch { data = text }
  return { ok: res.ok, status: res.status, data }
}

async function ensureAccount(acc) {
  const reg = await post('/api/auth/register', {
    FullName: acc.fullName, Email: acc.email, Password: acc.password, Role: acc.role,
  })
  if (reg.ok) console.log(`  ✓ Cuenta creada: ${acc.email} (${acc.role})`)
  else console.log(`  • ${acc.email}: ${reg.data?.error || 'ya existía, continúo'}`)

  const login = await post('/api/auth/login', { Email: acc.email, Password: acc.password })
  if (!login.ok) throw new Error(`No pude iniciar sesión como ${acc.email}: ${login.data?.error || login.status}`)
  return login.data // { token, userId, ... }
}

async function main() {
  console.log(`\n🌱 Sembrando datos demo en: ${BASE}\n`)

  console.log('1) Cuentas demo')
  const org = await ensureAccount(ORG)
  const user = await ensureAccount(USER)

  console.log('\n2) Eventos publicados')
  const existing = await get('/api/events')
  const existingTitles = new Set((Array.isArray(existing.data) ? existing.data : []).map(e => e.title))

  let created = 0
  for (const ev of EVENTS) {
    if (existingTitles.has(ev.title)) { console.log(`  • Ya existe: ${ev.title}`); continue }
    const res = await post('/api/events', {
      Title: ev.title, Description: ev.description, Price: ev.price, Quantity: ev.quantity,
      Category: ev.category, Address: ev.address, Location: ev.location, Photos: ev.photos,
      StartDate: ev.start, EndDate: ev.end, IsPublic: true,
    }, org.token)
    if (res.ok) { console.log(`  ✓ ${ev.title}`); created++ }
    else console.log(`  ✗ ${ev.title} → ${res.status} ${JSON.stringify(res.data)}`)
  }

  console.log(`\n✅ Listo. Eventos nuevos: ${created}/${EVENTS.length}\n`)
  console.log('── Credenciales demo ──')
  console.log(`  Organizador: ${ORG.email}  /  ${ORG.password}`)
  console.log(`  Usuario:     ${USER.email}  /  ${USER.password}\n`)
}

main().catch(err => { console.error('\n❌ Error:', err.message, '\n'); process.exit(1) })
