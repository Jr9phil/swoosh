<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import sundaySvg    from '../assets/sunday.svg'
import mondaySvg    from '../assets/monday.svg'
import tuesdaySvg   from '../assets/tuesday.svg'
import wednesdaySvg from '../assets/wednesday.svg'
import thursdaySvg  from '../assets/thursday.svg'
import fridaySvg    from '../assets/friday.svg'
import saturdaySvg  from '../assets/saturday.svg'

const PLANET_SRCS = [sundaySvg, mondaySvg, tuesdaySvg, wednesdaySvg, thursdaySvg, fridaySvg, saturdaySvg]
const PLANET_POS = [
  { left: '-95px',  top: '-123px', width: '460px', height: '460px' },
  { left: '-110px', top: '-140px', width: '500px', height: '500px' },
  { left: '-95px',  top: '-117px', width: '430px', height: '430px' },
  { left: '-110px', top: '-140px', width: '500px', height: '500px' },
  { left: '-105px', top: '-138px', width: '500px', height: '500px' },
  { left: '-95px',  top: '-116px', width: '460px', height: '460px' },
  { left: '-110px', top: '-145px', width: '510px', height: '510px' },
]

const emit = defineEmits<{
  'open-modal': []
  'reset-timeline': []
}>()

const activeDow = ref(0)
const hdDay     = ref('')
const hdWeekday = ref('')
const hdMonth   = ref('')
const canvasEl  = ref<HTMLCanvasElement | null>(null)

interface DayConfig {
  name: string; prefix: string; bg: string
  star: { rB: number; rF: number; gB: number; gF: number; bB: number; bF: number }
  planet: { px: number; py: number; r: number }
  moons: number[][]
  btn: string; btnText: string; weekday: string; sep: string
}

const DAY_CONFIG: DayConfig[] = [
  { name: 'Sunday',    prefix: 'su', bg: '#020306', star: {rB:220,rF:25, gB:220,gF:18,bB:230,bF:15}, planet: {px:135,py:100,r:74}, moons: [],           btn: 'rgba(220,215,200,0.45)', btnText: 'rgba(245,242,235,0.9)', weekday: 'rgba(220,215,200,0.52)', sep: 'rgba(210,205,190,0.22)' },
  { name: 'Monday',    prefix: 'mo', bg: '#010206', star: {rB:180,rF:40, gB:210,gF:25,bB:245,bF:10}, planet: {px:140,py:95, r:81}, moons: [],           btn: 'rgba(100,160,255,0.45)', btnText: 'rgba(160,200,255,0.9)', weekday: 'rgba(120,170,255,0.52)', sep: 'rgba(100,150,240,0.22)' },
  { name: 'Tuesday',   prefix: 'tu', bg: '#020104', star: {rB:235,rF:20, gB:210,gF:15,bB:170,bF:10}, planet: {px:120,py:85, r:70}, moons: [[239,216,30]], btn: 'rgba(255,160,80,0.45)',  btnText: 'rgba(255,180,100,0.9)', weekday: 'rgba(255,170,100,0.50)', sep: 'rgba(255,160,80,0.2)'  },
  { name: 'Wednesday', prefix: 'we', bg: '#010408', star: {rB:180,rF:40, gB:225,gF:20,bB:235,bF:10}, planet: {px:140,py:95, r:81}, moons: [],           btn: 'rgba(100,220,230,0.45)', btnText: 'rgba(180,240,245,0.9)', weekday: 'rgba(180,240,245,0.52)', sep: 'rgba(100,200,220,0.22)' },
  { name: 'Thursday',  prefix: 'th', bg: '#040200', star: {rB:240,rF:15, gB:220,gF:20,bB:160,bF:30}, planet: {px:145,py:105,r:81}, moons: [],           btn: 'rgba(240,200,80,0.45)',  btnText: 'rgba(255,220,100,0.9)', weekday: 'rgba(240,200,100,0.52)', sep: 'rgba(230,190,80,0.22)'  },
  { name: 'Friday',    prefix: 'fr', bg: '#050208', star: {rB:235,rF:20, gB:190,gF:30,bB:220,bF:20}, planet: {px:135,py:100,r:74}, moons: [],           btn: 'rgba(255,80,160,0.45)',  btnText: 'rgba(255,140,200,0.9)', weekday: 'rgba(255,120,190,0.52)', sep: 'rgba(240,100,180,0.22)' },
  { name: 'Saturday',  prefix: 'sa', bg: '#02010a', star: {rB:200,rF:35, gB:185,gF:30,bB:248,bF:7},  planet: {px:145,py:95, r:82}, moons: [[346,119,16]], btn: 'rgba(160,120,255,0.45)', btnText: 'rgba(200,170,255,0.9)', weekday: 'rgba(180,150,255,0.52)', sep: 'rgba(160,130,240,0.22)' },
]

const planetSrc   = computed(() => PLANET_SRCS[activeDow.value])
const planetStyle = computed(() => PLANET_POS[activeDow.value])

let starCfg: DayConfig | null = null
let rafId: number | null = null
let removeResizeListener: (() => void) | null = null

function applyDay(dow: number) {
  const cfg = DAY_CONFIG[dow]!
  const s = document.documentElement.style
  s.setProperty('--header-bg',          cfg.bg)
  s.setProperty('--header-accent',      cfg.btn)
  s.setProperty('--header-accent-text', cfg.btnText)
  s.setProperty('--header-weekday',     cfg.weekday)
  s.setProperty('--header-sep',         cfg.sep)
  activeDow.value = dow
  starCfg = cfg
  const d = new Date()
  hdDay.value     = String(d.getDate()).padStart(2, '0')
  hdWeekday.value = cfg.name.toUpperCase()
  hdMonth.value   = d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' }).toUpperCase()
}

function initCanvas() {
  const canvas = canvasEl.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')!
  function resize() {
    if (!canvasEl.value) return
    canvasEl.value.width  = canvasEl.value.offsetWidth
    canvasEl.value.height = canvasEl.value.offsetHeight
  }
  resize()
  window.addEventListener('resize', resize)
  removeResizeListener = () => window.removeEventListener('resize', resize)

  const STARS = Array.from({ length: 160 }, (_, i) => ({
    x:       Math.abs(Math.sin(i * 127.1 + 0.5)),
    y:       Math.abs(Math.sin(i *  91.3 + 1.2)),
    radius:  0.3 + Math.abs(Math.sin(i * 41.7)) * 1.0,
    rnd_r:   Math.abs(Math.sin(i *  7.3 + 1.1)),
    rnd_g:   Math.abs(Math.sin(i *  3.7 + 2.4)),
    twinkle: Math.abs(Math.sin(i * 17.3)),
    speed:   0.25 + Math.abs(Math.sin(i * 53.1)) * 0.6,
  }))

  let t = 0
  function draw() {
    ctx.clearRect(0, 0, (canvas as HTMLCanvasElement).width, (canvas as HTMLCanvasElement).height)
    t += 0.007
    if (!starCfg) { rafId = requestAnimationFrame(draw); return }
    const { rB, rF, gB, gF, bB, bF } = starCfg.star
    const { px: cpx, py: cpy, r: cr } = starCfg.planet
    const w = (canvas as HTMLCanvasElement).width
    const h = (canvas as HTMLCanvasElement).height
    for (const s of STARS) {
      const px = s.x * w, py = s.y * h
      if ((px - cpx) ** 2 + (py - cpy) ** 2 < cr * cr) continue
      let skip = false
      for (const moon of starCfg.moons) {
        const [mx, my, mr] = moon as [number, number, number]
        if ((px - mx) ** 2 + (py - my) ** 2 < mr * mr) { skip = true; break }
      }
      if (skip) continue
      const alpha = 0.18 + Math.sin(t * s.speed + s.twinkle * 6.28) * 0.13
      ctx.beginPath()
      ctx.arc(px, py, s.radius, 0, Math.PI * 2)
      ctx.fillStyle = `rgba(${Math.round(rB + s.rnd_r * rF)},${Math.round(gB + s.rnd_g * gF)},${Math.round(bB + s.rnd_r * bF)},${Math.max(0.03, alpha).toFixed(3)})`
      ctx.fill()
    }
    rafId = requestAnimationFrame(draw)
  }
  draw()
}

onMounted(() => {
  applyDay(new Date().getDay())
  initCanvas()
})

onUnmounted(() => {
  if (rafId !== null) cancelAnimationFrame(rafId)
  removeResizeListener?.()
})
</script>

<template>
  <div class="img-header">
    <canvas ref="canvasEl" class="star-canvas"></canvas>

    <img :src="planetSrc" :style="planetStyle" class="header-planet" alt="" />

    <!-- Add button -->
    <button class="header-add-btn" @click="emit('open-modal')" aria-label="New task">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">
        <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
      </svg>
    </button>

    <!-- Date display -->
    <div class="header-date" @click="emit('reset-timeline')">
      <span class="header-date-day">{{ hdDay }}</span>
      <span class="header-date-sep"></span>
      <div class="header-date-stack">
        <span class="header-date-weekday">{{ hdWeekday }}</span>
        <span class="header-date-month">{{ hdMonth }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.img-header {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  border-radius: 10px;
  margin-bottom: 20px;
  background: var(--header-bg, #010408);
  transition: background 0.6s ease;
}

@media (max-width: 640px) {
  .img-header {
    /* px-5 = 20px, pt-6 = 24px — bleed out of TasksView's container padding */
    margin-left: -20px;
    margin-right: -20px;
    margin-top: -24px;
    width: calc(100% + 40px);
    height: 220px;
    border-radius: 0;
    margin-bottom: 16px;
  }
}

.img-header::after {
  content: '';
  position: absolute; left: 0; right: 0; bottom: 0;
  height: 50%;
  background: linear-gradient(to bottom, transparent, rgba(0,0,0,0.75));
  pointer-events: none;
  z-index: 1;
}

.star-canvas {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
}

.header-planet {
  pointer-events: none;
  position: absolute;
  overflow: visible;
}


.header-date {
  position: absolute; bottom: 16px; right: 20px; z-index: 3;
  display: flex; align-items: flex-end; gap: 10px;
  cursor: pointer; user-select: none;
  transition: opacity .15s;
}
.header-date:hover { opacity: 0.8; }

.header-date-day {
  font-family: var(--font-mono);
  font-size: 48px; font-weight: 700;
  letter-spacing: -0.02em;
  color: rgba(255,255,255,0.45);
  line-height: 0.9;
}

.header-date-sep {
  width: 1px; height: 30px; flex-shrink: 0; margin-bottom: 4px;
  background: var(--header-sep, rgba(255,255,255,0.18));
  transition: background 0.6s ease;
}

.header-date-stack {
  display: flex; flex-direction: column;
  align-items: flex-start; justify-content: flex-end;
  gap: 3px; padding-bottom: 3px;
}

.header-date-weekday {
  font-family: var(--font-mono);
  font-size: 9.5px; font-weight: 700;
  letter-spacing: 0.20em; text-transform: uppercase;
  color: var(--header-weekday, rgba(255,255,255,0.4));
  transition: color 0.6s ease;
}

.header-date-month {
  font-family: var(--font-mono);
  font-size: 8.5px; font-weight: 700;
  letter-spacing: 0.16em; text-transform: uppercase;
  color: rgba(255,255,255,0.22);
}

.header-add-btn {
  position: absolute; bottom: 16px; left: 18px; z-index: 3;
  width: 40px; height: 40px;
  border-radius: 999px;
  border: 1.5px solid rgba(255,255,255,0.18);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.55);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: border-color .15s, background .15s, color .15s, transform .1s;
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
}
.header-add-btn:hover {
  border-color: var(--header-accent, rgba(255,255,255,0.45));
  background: rgba(255,255,255,0.10);
  color: var(--header-accent-text, rgba(255,255,255,0.9));
  transform: translateY(-1px);
}
.header-add-btn:active { transform: translateY(1px); }
.header-add-btn svg { width: 18px; height: 18px; stroke-width: 2px; }
</style>
