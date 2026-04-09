<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useRecurringStore } from '../stores/recurring'
import type { Task } from '../types/task'
import { occursOnDay, toTimelineTask } from '../utils/recurring'
import { X, Menu } from 'lucide-vue-next'
import sundaySvg    from '../assets/sunday.svg'
import mondaySvg    from '../assets/monday.svg'
import tuesdaySvg   from '../assets/tuesday.svg'
import wednesdaySvg from '../assets/wednesday.svg'
import thursdaySvg  from '../assets/thursday.svg'
import fridaySvg    from '../assets/friday.svg'
import cometSvg     from '../assets/comet.svg'
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

const props = defineProps<{
  loading?: boolean
}>()

const emit = defineEmits<{
  'open-modal': []
  'jump-to-task': [task: Task]
  'create-task-for-date': [date: string]
}>()

const tasksStore = useTasksStore()
const recurringStore = useRecurringStore()

// ── Header / planet state ─────────────────────────────────────────────────

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

const WEEKDAYS = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday']

const activeDow      = ref(0)
const hdDay          = ref('')
const hdWeekday      = ref('')
const hdMonth        = ref('')
const hdMonthIndex   = ref(0)
const isHeaderToday  = ref(true)
const isYearBoundary = computed(() =>
  (hdMonthIndex.value === 0  && hdDay.value === '01') ||
  (hdMonthIndex.value === 11 && hdDay.value === '31')
)
const isAprilFools = computed(() => hdMonthIndex.value === 3 && hdDay.value === '01')
const canvasEl   = ref<HTMLCanvasElement | null>(null)
const imgHeaderEl  = ref<HTMLElement | null>(null)
const dayPanelEl   = ref<HTMLElement | null>(null)


let starCfg: DayConfig | null = null
let rafId: number | null = null
let removeResizeListener: (() => void) | null = null
let todayDow = 0
let artLocked = false
// Set when the user arrives at yesterday via the today→scroll-back special case.
// Enables two follow-on behaviours: forward→today, backward→-7 (same week, no slide).
let yesterdayFromToday = false
// Set when offset 0 (today) was reached by scrolling rather than by a manual click or
// initial load. When true, the today→yesterday special case is suppressed so that
// scrolling back continues with the normal same-DOW rule instead.
let arrivedAtTodayByScroll = false

function applyDay(dow: number, dayOffset: number = 0) {
  const cfg = DAY_CONFIG[dow]!
  const s = document.documentElement.style
  s.setProperty('--header-bg',          cfg.bg)
  s.setProperty('--header-accent',      cfg.btn)
  s.setProperty('--header-accent-text', cfg.btnText)
  s.setProperty('--header-weekday',     cfg.weekday)
  s.setProperty('--header-sep',         cfg.sep)
  activeDow.value = dow
  starCfg = cfg

  isHeaderToday.value  = dayOffset === 0

  const d = new Date()
  d.setDate(d.getDate() + dayOffset)
  hdDay.value        = String(d.getDate()).padStart(2, '0')
  hdWeekday.value    = cfg.name.toUpperCase()
  hdMonth.value      = d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' }).toUpperCase()
  hdMonthIndex.value = d.getMonth()
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
    ctx.clearRect(0, 0, canvas.width, canvas.height)
    t += 0.007
    if (!starCfg) { rafId = requestAnimationFrame(draw); return }
    const { rB, rF, gB, gF, bB, bF } = starCfg.star
    const { px: cpx, py: cpy, r: cr } = starCfg.planet
    const w = canvas.width
    const h = canvas.height
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

// ── Timeline state ────────────────────────────────────────────────────────

const weekOffset = ref(0)
const selectedDayOffset = ref<number | null>(null)
const now = ref(new Date())
let clockInterval: ReturnType<typeof setInterval>

const slideState = ref<'idle' | 'sliding-out' | 'sliding-in' | 'hidden'>('idle')
const slideDir = ref(0)
const displayOffset = ref(0)

function isSameDay(d1: Date, d2: Date) {
  return d1.getFullYear() === d2.getFullYear() &&
      d1.getMonth() === d2.getMonth() &&
      d1.getDate() === d2.getDate()
}

function isTaskOverdue(task: Task) {
  if (!task.deadline || task.completed) return false
  return now.value.getTime() >= new Date(task.deadline).getTime()
}

function isPastDay(d: Date) {
  const today = new Date(now.value)
  today.setHours(0, 0, 0, 0)
  const dayStart = new Date(d)
  dayStart.setHours(0, 0, 0, 0)
  return dayStart < today
}

function recurringForDay(d: Date): Task[] {
  if (isPastDay(d)) return []
  return recurringStore.items
    .filter(r => {
      if (!occursOnDay(r, d)) return false
      // Hide if a real spawned task already exists for this recurring task on this day
      return !tasksStore.tasks.some(t => {
        if (t.recurringTaskId !== r.id) return false
        if (!t.deadline) return false
        const dl = new Date(t.deadline)
        return dl.getFullYear() === d.getFullYear() &&
               dl.getMonth()    === d.getMonth()    &&
               dl.getDate()     === d.getDate()
      })
    })
    .map(r => toTimelineTask(r, d))
}

const weekDays = computed(() => {
  const days = []
  for (let i = 0; i < 7; i++) {
    const dayOffset = displayOffset.value * 7 + i
    const d = new Date(now.value)
    d.setDate(now.value.getDate() + dayOffset)

    const isToday = isSameDay(d, now.value)
    const isPast = isPastDay(d)
    const incompleteTasks = [
      ...tasksStore.tasks.filter(t => !t.completed && t.deadline && isSameDay(new Date(t.deadline), d)),
      ...recurringForDay(d),
    ]
    const completedTasks = (isPast || isToday)
      ? tasksStore.tasks.filter(t => t.completed && isSameDay(new Date(t.completed), d))
      : []

    let tasksForDay = []
    let allCompleted = false

    if (isToday) {
      if (incompleteTasks.length === 0 && completedTasks.length > 0) {
        tasksForDay = completedTasks
        allCompleted = true
      } else {
        tasksForDay = incompleteTasks
      }
    } else {
      tasksForDay = [...incompleteTasks, ...completedTasks]
    }

    const count = tasksForDay.length
    const hasOverdue = incompleteTasks.some(t => isTaskOverdue(t))

    days.push({
      date: d,
      name: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d.getDay()],
      num: d.getDate(),
      isToday,
      allCompleted,
      taskCount: count,
      hasOverdue,
      dayOffset,
      tasks: tasksForDay
    })
  }
  return days
})

const selectedDay = computed(() => {
  if (selectedDayOffset.value === null) return null
  const d = new Date(now.value)
  d.setDate(now.value.getDate() + selectedDayOffset.value)

  const isToday = isSameDay(d, now.value)
  const dayName = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d.getDay()]
  const monthName = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'][d.getMonth()]
  const label = `${isToday ? 'Today' : dayName} · ${monthName} ${d.getDate()}`

  const isPast = isPastDay(d)
  const incompleteTasks = [
    ...tasksStore.tasks.filter(t => !t.completed && t.deadline && isSameDay(new Date(t.deadline), d)),
    ...recurringForDay(d),
  ]
  const completedTasks = (isPast || isToday)
    ? tasksStore.tasks.filter(t => t.completed && isSameDay(new Date(t.completed), d))
    : []

  const sortByDeadline = (a: Task, b: Task) => {
    const timeA = a.deadline ? new Date(a.deadline).getTime() : 0
    const timeB = b.deadline ? new Date(b.deadline).getTime() : 0
    if (timeA !== timeB) return timeA - timeB
    if (a.pinned !== b.pinned) return a.pinned ? -1 : 1
    if (b.priority !== a.priority) return b.priority - a.priority
    return b.rating - a.rating
  }

  const tasks = [
    ...incompleteTasks.sort(sortByDeadline),
    ...completedTasks.sort((a, b) => new Date(a.completed!).getTime() - new Date(b.completed!).getTime())
  ]

  return { label, tasks }
})

const frozenSelectedDay = ref<{ label: string; tasks: Task[] } | null>(null)
watch(selectedDay, (val) => {
  if (val !== null) frozenSelectedDay.value = val
})

// When a day is selected/deselected, update header art.
// artLocked suppresses this during week transitions (art is applied directly instead).
watch(selectedDayOffset, (val) => {
  if (artLocked) return
  if (val === null) {
    applyDay(todayDow, 0)
  } else {
    const d = new Date()
    d.setDate(d.getDate() + val)
    applyDay(d.getDay(), val)
  }
})

// Relative day badge label
// Progressive desaturation: each week back reduces saturation and brightness.
// Future days get a fixed mild desaturation.
const planetFilter = computed(() => {
  const offset = selectedDayOffset.value
  if (offset === null || offset === 0) return ''
  if (offset > 0) return 'saturate(0.70) brightness(0.96)'
  const weeksBack = Math.ceil(Math.abs(offset) / 7)
  const sat    = Math.max(0.05, 0.85 - weeksBack * 0.20)
  const bright = Math.max(0.72, 0.96 - weeksBack * 0.06)
  return `saturate(${sat.toFixed(2)}) brightness(${bright.toFixed(2)})`
})

const dayBadgeText = computed(() => {
  const offset = selectedDayOffset.value
  if (offset === null) return ''
  if (offset === 0) return 'Today'
  const d = new Date()
  d.setDate(d.getDate() + offset)
  const dow = d.getDay()
  if (offset === 1)  return 'Tomorrow'
  if (offset === -1) return 'Yesterday'
  if (offset >= 2  && offset <= 6)  return 'This ' + WEEKDAYS[dow]
  if (offset >= -7 && offset <= -2) return 'Last ' + WEEKDAYS[dow]
  if (offset >= 7  && offset <= 13) return 'Next Week'
  return offset > 0 ? `+${offset} days` : `${-offset} days ago`
})

function toggleDay(offset: number) {
  yesterdayFromToday = false
  arrivedAtTodayByScroll = false
  if (selectedDayOffset.value === offset) {
    selectedDayOffset.value = null
  } else {
    selectedDayOffset.value = offset
  }
}

function closeDayPanel() {
  yesterdayFromToday = false
  arrivedAtTodayByScroll = false
  selectedDayOffset.value = null
}

function formatTime(deadline: string | null | undefined) {
  if (!deadline) return ''
  return new Date(deadline).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

function getDotClass(task: Task) {
  if (task.isRecurring) return 'recurring'
  if (task.pinned) return 'pinned'
  if (task.priority === 3) return 'high'
  if (task.priority === 2) return 'med'
  if (task.priority === 1) return 'low'
  return ''
}

function jumpToTask(task: Task) {
  emit('jump-to-task', task)
}

let clickTimer: ReturnType<typeof setTimeout> | null = null

function handleDayClick(dayOffset: number) {
  if (clickTimer) clearTimeout(clickTimer)
  clickTimer = setTimeout(() => {
    clickTimer = null
    toggleDay(dayOffset)
  }, 220)
}

function handleDayDblClick(day: { date: Date; dayOffset: number }) {
  if (clickTimer) {
    clearTimeout(clickTimer)
    clickTimer = null
  }
  if (isPastDay(day.date)) return
  const yyyy = day.date.getFullYear()
  const mm = String(day.date.getMonth() + 1).padStart(2, '0')
  const dd = String(day.date.getDate()).padStart(2, '0')
  emit('create-task-for-date', `${yyyy}-${mm}-${dd}`)
}

// ── Navigation ────────────────────────────────────────────────────────────

let wheelCooldown = false
async function handleWheel(e: WheelEvent) {
  if (Math.abs(e.deltaX) < Math.abs(e.deltaY)) {
    if (wheelCooldown || slideState.value !== 'idle') return
    const dir = e.deltaY > 0 ? 1 : -1
    if (e.shiftKey && selectedDayOffset.value !== null) {
      await stepDay(dir)
    } else {
      await changeWeek(dir)
    }
    wheelCooldown = true
    setTimeout(() => wheelCooldown = false, 350)
  }
}

async function stepDay(dir: number) {
  if (selectedDayOffset.value === null) return
  const newOffset = selectedDayOffset.value + dir
  yesterdayFromToday = false
  arrivedAtTodayByScroll = (newOffset === 0)

  const weekStart = weekOffset.value * 7
  const weekEnd   = weekOffset.value * 7 + 6

  if (newOffset >= weekStart && newOffset <= weekEnd) {
    // Within the same visible week — instant selection change
    selectedDayOffset.value = newOffset
  } else {
    // Crosses into an adjacent week — animate, landing on the specific day
    const newWeek = weekOffset.value + dir
    if (newWeek < -4 || newWeek > 4) return

    const d = new Date()
    d.setDate(d.getDate() + newOffset)
    applyDay(d.getDay(), newOffset)
    artLocked = true

    slideDir.value = dir
    slideState.value = 'sliding-out'

    await new Promise(r => setTimeout(r, 160))

    weekOffset.value = newWeek
    displayOffset.value = newWeek
    slideState.value = 'hidden'
    selectedDayOffset.value = newOffset

    await new Promise(r => setTimeout(r, 10))

    artLocked = false
    slideState.value = 'sliding-in'

    await new Promise(r => setTimeout(r, 180))
    slideState.value = 'idle'
  }
}

let touchStartX: number | null = null
function handleTouchStart(e: TouchEvent) {
  touchStartX = e.touches[0].clientX
}

async function handleTouchEnd(e: TouchEvent) {
  if (touchStartX === null || slideState.value !== 'idle') return
  const dx = e.changedTouches[0].clientX - touchStartX
  touchStartX = null
  if (Math.abs(dx) < 40) return
  const dir = dx < 0 ? 1 : -1
  await changeWeek(dir)
}

let headerTouchStartX: number | null = null
function handleHeaderTouchStart(e: TouchEvent) {
  headerTouchStartX = e.touches[0].clientX
}

async function handleHeaderTouchEnd(e: TouchEvent) {
  if (headerTouchStartX === null || slideState.value !== 'idle') return
  const dx = e.changedTouches[0].clientX - headerTouchStartX
  headerTouchStartX = null
  if (Math.abs(dx) < 40) return
  const dir = dx < 0 ? 1 : -1
  if (selectedDayOffset.value !== null) {
    await stepDay(dir)
  } else {
    await changeWeek(dir)
  }
}

async function animateToOffset(next: number, dir: number) {
  // Carry the selection to the same day-of-week in the new week, with two special cases:
  //   today → scroll back          → yesterday (-1),     set yesterdayFromToday flag
  //   yesterday (from today) → fwd → today (0),          clear flag
  //   all other cases              → same DOW, clear flag
  const sel = selectedDayOffset.value
  let targetOffset: number | null
  if (sel === null) {
    targetOffset = null
    arrivedAtTodayByScroll = false
  } else if (sel === 0 && dir < 0 && !arrivedAtTodayByScroll) {
    // today → scroll back → yesterday (only when today wasn't reached by scrolling)
    targetOffset = -1
    yesterdayFromToday = true
    arrivedAtTodayByScroll = false
  } else if (yesterdayFromToday && sel === -1 && dir > 0) {
    // yesterday (from today) → scroll forward → today
    targetOffset = 0
    yesterdayFromToday = false
    arrivedAtTodayByScroll = true
  } else {
    yesterdayFromToday = false
    targetOffset = next * 7 + ((sel % 7) + 7) % 7
    arrivedAtTodayByScroll = (targetOffset === 0)
  }

  // Pre-apply the destination art so it never flickers through today's art.
  // Lock the watch so it doesn't override while selectedDayOffset transiently becomes null.
  if (targetOffset !== null) {
    const d = new Date()
    d.setDate(d.getDate() + targetOffset)
    applyDay(d.getDay(), targetOffset)
  } else {
    applyDay(todayDow, 0)
  }
  artLocked = true

  slideDir.value = dir
  slideState.value = 'sliding-out'
  if (targetOffset === null) closeDayPanel()

  await new Promise(r => setTimeout(r, 160))

  weekOffset.value = next
  displayOffset.value = next
  slideState.value = 'hidden'
  // Swap the selected day while the grid is invisible — panel stays open, no flicker
  if (targetOffset !== null) selectedDayOffset.value = targetOffset

  await new Promise(r => setTimeout(r, 10))

  artLocked = false
  slideState.value = 'sliding-in'

  await new Promise(r => setTimeout(r, 180))
  slideState.value = 'idle'
}

async function changeWeek(dir: number) {
  // Special case: if yesterday was reached from today, scrolling back moves to the
  // start of this week (-7) rather than sliding to the previous week.
  if (yesterdayFromToday && selectedDayOffset.value === -1 && dir < 0) {
    yesterdayFromToday = false
    selectedDayOffset.value = -7
    return
  }
  const next = weekOffset.value + dir
  if (next < -4 || next > 4) return
  await animateToOffset(next, dir)
}

async function resetTimeline() {
  if (weekOffset.value === 0) {
    closeDayPanel()
    return
  }
  const dir = weekOffset.value > 0 ? -1 : 1
  await animateToOffset(0, dir)
}

async function focusOffset(offset: number) {
  yesterdayFromToday = false
  arrivedAtTodayByScroll = false
  const targetWeek = Math.floor(offset / 7)
  const boundedWeek = Math.max(-4, Math.min(4, targetWeek))

  if (boundedWeek !== weekOffset.value) {
    const dir = boundedWeek > weekOffset.value ? 1 : -1
    await animateToOffset(boundedWeek, dir)
  }
  selectedDayOffset.value = offset
}

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    if (document.querySelector('dialog[open]')) return
    closeDayPanel()
    return
  }
  if (e.code === 'Space' && !['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement).tagName)) {
    e.preventDefault()
    resetTimeline()
  }
}

function handleDocClick(e: MouseEvent) {
  const target = e.target as Node
  if (document.querySelector('dialog[open]')?.contains(target)) return
  if (
    !imgHeaderEl.value?.contains(target) &&
    !dayPanelEl.value?.contains(target)
  ) {
    closeDayPanel()
  }
}

onMounted(() => {
  todayDow = new Date().getDay()
  applyDay(todayDow, 0)
  initCanvas()
  window.addEventListener('keydown', handleKeyDown)
  document.addEventListener('mousedown', handleDocClick)
  clockInterval = setInterval(() => { now.value = new Date() }, 10000)
})

onUnmounted(() => {
  if (rafId !== null) cancelAnimationFrame(rafId)
  removeResizeListener?.()
  window.removeEventListener('keydown', handleKeyDown)
  document.removeEventListener('mousedown', handleDocClick)
  if (clockInterval) clearInterval(clockInterval)
})

defineExpose({ resetTimeline, focusOffset })
</script>

<template>
  <div class="header-section">
    <!-- ── Image header ── -->
    <div
      ref="imgHeaderEl"
      class="img-header"
      @touchstart="handleHeaderTouchStart"
      @touchend="handleHeaderTouchEnd"
    >
      <canvas ref="canvasEl" class="star-canvas" :style="{ filter: planetFilter, transition: 'filter 0.4s ease' }"></canvas>
      <img
        v-for="(src, i) in PLANET_SRCS"
        :key="i"
        v-show="i === activeDow"
        :src="src"
        :style="{ ...PLANET_POS[i], filter: planetFilter, transition: 'filter 0.4s ease', transform: isAprilFools ? 'scaleX(-1)' : undefined }"
        class="header-planet"
        alt=""
      />

      <!-- Comet overlay (first of the month) -->
      <img
        v-if="isYearBoundary"
        :src="cometSvg"
        class="header-comet"
        :style="{ filter: planetFilter, transition: 'filter 0.4s ease' }"
        alt=""
      />

      <!-- Sidebar toggle (mobile only, top-left) -->
      <label for="sidebar" class="header-menu-btn" aria-label="Open sidebar">
        <Menu :size="16" :stroke-width="2" />
      </label>

      <!-- Add button (top-left on desktop, shifted right on mobile) -->
      <button
        class="header-add-btn"
        :class="{ 'btn-hidden': dayBadgeText }"
        @click="emit('open-modal')"
        aria-label="New task"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
      </button>

      <!-- Relative day badge (top-left, shown when non-today day is selected) -->
      <div class="header-day-badge" :class="{ visible: dayBadgeText }">{{ dayBadgeText }}</div>

      <!-- Date display (top-right, shows selected day or today) -->
      <div class="header-date" :class="{ 'is-today': isHeaderToday }" @click="resetTimeline">
        <span class="header-date-day">{{ hdDay }}</span>
        <span class="header-date-sep"></span>
        <div class="header-date-stack">
          <span class="header-date-weekday">{{ hdWeekday }}</span>
          <span class="header-date-month">{{ hdMonth }}</span>
        </div>
      </div>

      <!-- Timeline strip (inside header, at bottom) -->
      <div class="header-timeline-strip">
        <!-- Return-to-today arrow -->
        <button
          v-if="weekOffset !== 0"
          class="timeline-return-arrow"
          :class="weekOffset < 0 ? 'right' : 'left'"
          @click.stop="resetTimeline"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <polyline v-if="weekOffset < 0" points="9 18 15 12 9 6"/>
            <polyline v-else points="15 18 9 12 15 6"/>
          </svg>
        </button>

        <div
          class="timeline-grid"
          :class="{
            'slide-out-left':  slideState === 'sliding-out' && slideDir > 0,
            'slide-out-right': slideState === 'sliding-out' && slideDir < 0,
            'slide-hidden-right': slideState === 'hidden' && slideDir > 0,
            'slide-hidden-left':  slideState === 'hidden' && slideDir < 0,
            'slide-in': slideState === 'sliding-in',
          }"
          @wheel.prevent="handleWheel"
          @touchstart.stop="handleTouchStart"
          @touchend.stop="handleTouchEnd"
        >
          <div
            v-for="day in weekDays"
            :key="day.dayOffset"
            class="day-cell"
            :class="{
              today:    day.isToday,
              past:     !day.isToday && isPastDay(day.date),
              future:   !day.isToday && !isPastDay(day.date),
              selected: selectedDayOffset === day.dayOffset,
            }"
            @click="handleDayClick(day.dayOffset)"
            @dblclick.stop="handleDayDblClick(day)"
          >
            <span class="day-name">{{ day.name }}</span>
            <span class="day-num">{{ day.num }}</span>

            <div v-if="loading" class="day-count-skeleton"></div>
            <template v-else>
              <div
                v-if="day.taskCount > 0"
                class="day-count has-tasks"
                :class="{
                  'overdue-count':  day.hasOverdue,
                  'today-count':    day.isToday && !day.hasOverdue && !day.allCompleted,
                  'completed':      day.allCompleted
                }"
                v-animate-sync="day.hasOverdue ? { group: 'overdue', type: 'count' } : (day.isToday && !day.allCompleted ? { group: 'today', type: 'border' } : null)"
              >{{ day.taskCount }}</div>
              <span v-else class="day-count no-tasks">·</span>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Day panel (outside the header) ── -->
    <div ref="dayPanelEl" class="day-panel-wrap" :class="{ open: selectedDayOffset !== null }">
      <div class="day-panel">
        <div class="day-panel-header">
          <span class="day-panel-title">{{ frozenSelectedDay?.label }}</span>
          <button class="day-panel-close" @click="closeDayPanel">
            <X :size="11" stroke-width="2.5" />
          </button>
        </div>
        <div class="day-panel-tasks">
          <template v-if="frozenSelectedDay?.tasks.length">
            <div
              v-for="task in frozenSelectedDay.tasks"
              :key="task.id"
              class="day-panel-task linkable"
              :class="{ done: task.completed, overdue: !task.completed && isTaskOverdue(task) }"
              @click="jumpToTask(task)"
            >
              <svg v-if="task.completed" class="day-panel-check" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
              <div v-else class="day-panel-dot" :class="getDotClass(task)"></div>
              <span class="day-panel-name">{{ task.title }}</span>
              <span class="day-panel-time">{{ task.completed ? formatTime(task.completed) : formatTime(task.deadline) }}</span>
            </div>
          </template>
          <div v-else class="day-panel-empty">No tasks due this day</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Outer wrapper ── */
.header-section {
  margin-bottom: 0;
}

/* ── Image header ── */
.img-header {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  border-radius: 10px;
  margin-bottom: 6px;
  background: var(--header-bg, #010408);
  transition: background 0.6s ease;
}

@media (max-width: 640px) {
  .img-header {
    margin-left: -20px;
    margin-right: -20px;
    margin-top: -24px;
    width: calc(100% + 40px);
    height: 220px;
    border-radius: 0;
    margin-bottom: 6px;
  }
}

/* Deeper vignette to accommodate timeline strip */
.img-header::after {
  content: '';
  position: absolute; left: 0; right: 0; bottom: 0;
  height: 70%;
  background: linear-gradient(to bottom,
    transparent 0%,
    rgba(0,0,0,0.35) 40%,
    rgba(0,0,0,0.82) 72%,
    rgba(0,0,0,0.92) 100%
  );
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

.header-comet {
  pointer-events: none;
  position: absolute;
  right: 170px;
  top: 14px;
  width: 69px;
  height: 35px;
  opacity: 0.7;
}

/* ── Sidebar hamburger (mobile only, top-left) ── */
.header-menu-btn {
  position: absolute; top: 14px; left: 16px; z-index: 3;
  width: 34px; height: 34px;
  border-radius: 999px;
  border: 1.5px solid rgba(255,255,255,0.40);
  background: rgba(0,0,0,0.60);
  color: rgba(255,255,255,0.85);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: border-color .15s, background .15s, color .15s, transform .1s;
}
.header-menu-btn:hover {
  border-color: var(--header-accent, rgba(255,255,255,0.45));
  background: rgba(255,255,255,0.10);
  color: var(--header-accent-text, rgba(255,255,255,0.9));
  transform: translateY(-1px);
}
.header-menu-btn:active { transform: translateY(1px); }

@media (min-width: 1024px) {
  .header-menu-btn { display: none; }
}

/* ── Add button (top-left) ── */
.header-add-btn {
  position: absolute; top: 14px; left: 16px; z-index: 3;
  width: 34px; height: 34px;
  border-radius: 999px;
  border: 1.5px solid rgba(255,255,255,0.40);
  background: rgba(0,0,0,0.60);
  color: rgba(255,255,255,0.85);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: border-color .15s, background .15s, color .15s, transform .1s;
}
.header-add-btn:hover {
  border-color: var(--header-accent, rgba(255,255,255,0.45));
  background: rgba(255,255,255,0.10);
  color: var(--header-accent-text, rgba(255,255,255,0.9));
  transform: translateY(-1px);
}
.header-add-btn:active { transform: translateY(1px); }
.header-add-btn svg { width: 16px; height: 16px; stroke-width: 2px; }
.header-add-btn.btn-hidden {
  opacity: 0;
  pointer-events: none;
}

/* On mobile the hamburger occupies the top-left slot; push add button and badge right */
@media (max-width: 1023px) {
  .header-add-btn,
  .header-day-badge { left: 60px; }
}

/* ── Relative day badge (top-left) ── */
.header-day-badge {
  position: absolute; top: 14px; left: 16px; z-index: 3;
  font-family: var(--font-mono); font-size: 9px; font-weight: 700;
  letter-spacing: 0.12em; text-transform: uppercase;
  color: rgba(255,255,255,0.55);
  background: rgba(255,255,255,0.08);
  border: 1px solid rgba(255,255,255,0.14);
  border-radius: 999px;
  padding: 4px 10px;
  opacity: 0;
  pointer-events: none;
}
.header-day-badge.visible { opacity: 1; }

/* ── Date display (top-right) ── */
.header-date {
  position: absolute; top: 14px; right: 16px; z-index: 3;
  display: flex; align-items: center; gap: 10px;
  cursor: pointer; user-select: none;
  transition: opacity .15s;
}
.header-date:hover { opacity: 0.7; }

.header-date-day {
  font-family: var(--font-mono);
  font-size: 36px; font-weight: 700;
  letter-spacing: -0.02em;
  color: rgba(255,255,255,0.40);
  line-height: 1;
  transition: color .3s ease;
}
.header-date.is-today .header-date-day { color: rgba(255,255,255,0.72); }

.header-date-sep {
  width: 1px; height: 26px; flex-shrink: 0;
  background: var(--header-sep, rgba(255,255,255,0.18));
  transition: background 0.6s ease;
}
.header-date.is-today .header-date-sep { background: var(--header-sep, rgba(255,255,255,0.38)); }

.header-date-stack {
  display: flex; flex-direction: column;
  gap: 3px;
}
.header-date-weekday {
  font-family: var(--font-mono);
  font-size: 11px; font-weight: 700;
  letter-spacing: 0.18em; text-transform: uppercase;
  color: var(--header-weekday, rgba(255,255,255,0.38));
  transition: color 0.6s ease;
}
.header-date-month {
  font-family: var(--font-mono);
  font-size: 9px; font-weight: 700;
  letter-spacing: 0.14em; text-transform: uppercase;
  color: rgba(255,255,255,0.18);
}

/* ── Timeline strip (inside header, at bottom) ── */
.header-timeline-strip {
  position: absolute; bottom: 0; left: 0; right: 0;
  z-index: 2; padding: 8px 8px 10px;
  transition: background .2s ease;
  isolation: isolate;
}
.header-timeline-strip:hover {
  background: rgba(0,0,0,0.16);
}

/* Return-to-today arrow */
.timeline-return-arrow {
  position: absolute;
  top: 50%; transform: translateY(-50%);
  display: flex; align-items: center;
  font-family: var(--font-mono); font-size: 8px; font-weight: 700;
  letter-spacing: 0.10em;
  color: rgba(255,255,255,0.35);
  cursor: pointer;
  transition: color .15s;
  user-select: none;
  background: none; border: none; padding: 4px;
  z-index: 3;
}
.timeline-return-arrow:hover { color: rgba(255,255,255,0.65); }
.timeline-return-arrow svg { width: 12px; height: 12px; }
.timeline-return-arrow.right { right: 10px; }
.timeline-return-arrow.left  { left: 10px; }

/* ── Timeline 7-day grid ── */
.timeline-grid {
  display: grid; grid-template-columns: repeat(7, 1fr);
  gap: 2px;
  user-select: none;
  transition: opacity .15s ease, transform .15s ease;
}
.timeline-grid.slide-out-left  { opacity: 0; transform: translateX(-60px); }
.timeline-grid.slide-out-right { opacity: 0; transform: translateX(60px); }
.timeline-grid.slide-hidden-right { opacity: 0; transform: translateX(60px);  transition: none; }
.timeline-grid.slide-hidden-left  { opacity: 0; transform: translateX(-60px); transition: none; }
.timeline-grid.slide-in { opacity: 1; transform: translateX(0); transition: opacity .18s ease, transform .18s ease; }

/* ── Day cells ── */
.day-cell {
  display: flex; flex-direction: column; align-items: center;
  gap: 3px; padding: 7px 4px 6px;
  border-radius: 6px;
  cursor: pointer;
  border: 1px solid transparent;
  transition: background-color 120ms;
}
.day-cell:hover { background: rgba(255,255,255,0.06); }

.day-name {
  font-family: var(--font-mono); font-size: 8.5px;
  letter-spacing: 0.10em; text-transform: uppercase;
  color: rgba(255,255,255,0.28);
}
.day-num {
  font-size: 18px; font-weight: 700;
  color: rgba(255,255,255,0.30); line-height: 1;
  font-family: var(--font-mono);
}

/* Past zone */
.day-cell.past .day-name  { color: rgba(255,255,255,0.22); }
.day-cell.past .day-num   { color: rgba(255,255,255,0.25); font-weight: 400; }

/* Today zone */
.day-cell.today { background: rgba(255,255,255,0.07); border-color: rgba(255,255,255,0.14); }
.day-cell.today .day-name { color: rgba(255,255,255,0.55); }
.day-cell.today .day-num  { color: rgba(255,255,255,0.90); font-weight: 700; }

/* Future zone */
.day-cell.future .day-num { color: rgba(255,255,255,0.32); }

/* Selected */
.day-cell.selected {
  background: rgba(255,255,255,0.09);
  border-color: rgba(255,255,255,0.20) !important;
}
.day-cell.today.selected { border-color: rgba(100,140,255,0.65) !important; }

/* Count pills */
.day-count {
  min-width: 18px; height: 15px;
  border-radius: 999px;
  display: flex; align-items: center; justify-content: center;
  font-size: 8px; font-weight: 700;
  font-family: var(--font-mono); padding: 0 5px;
}
.day-count.has-tasks {
  color: rgba(255,255,255,0.38);
  border: 1px solid rgba(255,255,255,0.18);
  background: rgba(255,255,255,0.05);
}
.day-count.today-count {
  color: var(--color-swoosh-today);
  border-color: color-mix(in srgb, var(--color-swoosh-today) 35%, transparent);
  background: color-mix(in srgb, var(--color-swoosh-today) 10%, transparent);
}
.day-count.overdue-count {
  color: var(--color-error);
  border-color: color-mix(in srgb, var(--color-error) 38%, transparent);
  background: color-mix(in srgb, var(--color-error) 12%, transparent);
}
.day-count.no-tasks { color: rgba(255,255,255,0.18); font-size: 8px; }
.day-cell.past .day-count.has-tasks,
.day-count.completed {
  background: rgba(255,255,255,0.12);
  border-color: transparent;
  color: rgba(255,255,255,0.30);
}

.day-count-skeleton {
  min-width: 18px; height: 15px; border-radius: 999px;
  background: rgba(255,255,255,0.10);
  animation: pulse 1.5s ease-in-out infinite;
}
@keyframes pulse {
  0%, 100% { opacity: 0.6; }
  50%       { opacity: 1; }
}

/* ── Day panel ── */
.day-panel-wrap {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows .22s ease, opacity .18s ease;
  opacity: 0;
  overflow: hidden;
  pointer-events: none;
  margin-bottom: -8px;
}
.day-panel-wrap.open {
  grid-template-rows: 1fr;
  opacity: 1;
  pointer-events: all;
  margin-bottom: 14px;
}

.day-panel {
  min-height: 0;
  border: 1px solid var(--color-swoosh-border-hover);
  border-radius: 10px;
  background: var(--color-base-200);
  overflow: hidden;
  margin: 10px 0 0;
  box-shadow: 0 0 0 3px rgba(255,255,255,0.03), 0 8px 24px rgba(0,0,0,0.4);
}

.day-panel-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 16px 9px;
  border-bottom: 1px solid var(--color-swoosh-border);
  background: var(--color-base-300);
}
.day-panel-title {
  font-family: var(--font-mono); font-size: 9.5px; font-weight: 700;
  letter-spacing: 0.14em; text-transform: uppercase;
  color: var(--color-swoosh-text-muted);
}
.day-panel-close {
  width: 22px; height: 22px; border-radius: 999px; border: none;
  background: transparent; color: var(--color-swoosh-text-faint);
  cursor: pointer; display: flex; align-items: center; justify-content: center;
  transition: color .1s, background .1s;
}
.day-panel-close:hover { color: var(--color-swoosh-text-muted); background: var(--color-base-200); }

.day-panel-task {
  display: flex; align-items: center; gap: 12px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--color-swoosh-border);
  animation: fadeUp .15s ease both;
}
.day-panel-task:last-child { border-bottom: none; }
.day-panel-task.linkable { cursor: pointer; }
.day-panel-task.linkable:hover { background: var(--color-base-300); }
.day-panel-task.linkable:hover .day-panel-name { color: var(--color-base-content); }

.day-panel-dot {
  width: 7px; height: 7px; border-radius: 999px; flex-shrink: 0;
  background: var(--color-swoosh-text-faint);
}
.day-panel-dot.high   { background: var(--color-warning); }
.day-panel-dot.med    { background: var(--color-info); }
.day-panel-dot.low    { background: var(--color-success); }
.day-panel-dot.pinned    { background: var(--color-secondary); }
.day-panel-dot.recurring { background: var(--color-primary); border-radius: 2px; }

.day-panel-task.overdue .day-panel-name { color: var(--color-error); }
.day-panel-task.overdue .day-panel-time { color: var(--color-error); opacity: 0.7; }

.day-panel-check {
  width: 13px; height: 13px; flex-shrink: 0;
  color: var(--color-success); opacity: 0.7;
}

.day-panel-task.done .day-panel-name {
  color: var(--color-swoosh-text-faint);
  text-decoration: line-through;
  text-decoration-color: rgba(255,255,255,0.18);
}
.day-panel-task.done .day-panel-time { opacity: 0.45; }

.day-panel-name {
  flex: 1; font-size: 13px; font-weight: 500;
  color: var(--color-base-content);
}
.day-panel-time {
  font-family: var(--font-mono); font-size: 10px;
  color: var(--color-swoosh-text-faint); flex-shrink: 0; letter-spacing: 0.06em;
}
.day-panel-empty {
  padding: 16px;
  font-size: 12px; color: var(--color-swoosh-text-faint);
  font-family: var(--font-mono); letter-spacing: 0.08em;
}

:global(.highlight-pulse) {
  animation: highlightPulse 2s ease-out;
}
@keyframes highlightPulse {
  0% { background-color: transparent; }
  10% { background-color: rgba(232, 232, 234, 0.1); }
  100% { background-color: transparent; }
}
</style>
