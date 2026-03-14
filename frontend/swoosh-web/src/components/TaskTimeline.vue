<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useTasksStore } from '../stores/tasks'
import type { Task } from '../types/task'
import { X } from 'lucide-vue-next'

const props = defineProps<{
  loading?: boolean
}>()

const tasksStore = useTasksStore()
const weekOffset = ref(0)
const selectedDayOffset = ref<number | null>(null)
const now = ref(new Date())
let clockInterval: any

onMounted(() => {
  window.addEventListener('keydown', handleKeyDown)
  clockInterval = setInterval(() => { now.value = new Date() }, 10000)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown)
  if (clockInterval) clearInterval(clockInterval)
})

const slideState = ref<'idle' | 'sliding-out' | 'sliding-in' | 'hidden'>('idle')
const slideDir = ref(0)
const displayOffset = ref(0)

function isSameDay(d1: Date, d2: Date) {
  return d1.getFullYear() === d2.getFullYear() &&
      d1.getMonth() === d2.getMonth() &&
      d1.getDate() === d2.getDate()
}

function isTaskOverdue(task: Task) {
  if (!task.deadline) return false
  return now.value.getTime() >= new Date(task.deadline).getTime()
}

const weekDays = computed(() => {
  const days = []
  for (let i = 0; i < 7; i++) {
    const dayOffset = displayOffset.value * 7 + i
    const d = new Date(now.value)
    d.setDate(now.value.getDate() + dayOffset)

    const isToday = isSameDay(d, now.value)
    const tasksForDay = tasksStore.tasks.filter(t => !t.completed && t.deadline && isSameDay(new Date(t.deadline), d))
    const count = tasksForDay.length
    const hasOverdue = tasksForDay.some(t => isTaskOverdue(t))

    days.push({
      date: d,
      name: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d.getDay()],
      num: d.getDate(),
      isToday,
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

  const tasks = tasksStore.tasks.filter(t => !t.completed && t.deadline && isSameDay(new Date(t.deadline), d))
      .sort((a, b) => {
        const timeA = new Date(a.deadline!).getTime()
        const timeB = new Date(b.deadline!).getTime()
        if (timeA !== timeB) return timeA - timeB

        if (b.priority !== a.priority) return b.priority - a.priority
        return b.rating - a.rating
      })

  return {
    label,
    tasks
  }
})

function toggleDay(offset: number) {
  if (selectedDayOffset.value === offset) {
    selectedDayOffset.value = null
  } else {
    selectedDayOffset.value = offset
  }
}

function closeDayPanel() {
  selectedDayOffset.value = null
}

function formatTime(deadline: string | null | undefined) {
  if (!deadline) return ''
  return new Date(deadline).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

function getPriorityClass(task: Task) {
  if (task.pinned) return 'bg-swoosh-pin'
  if (task.priority === 3) return 'bg-swoosh-high'
  if (task.priority === 2) return 'bg-swoosh-med'
  if (task.priority === 1) return 'bg-swoosh-low'
  return 'bg-swoosh-text-faint'
}

function jumpToTask(task: Task) {
  emit('jump-to-task', task)
  // Do NOT close the day panel — user may want to jump to multiple tasks
}

// Navigation logic
let wheelCooldown = false
async function handleWheel(e: WheelEvent) {
  if (Math.abs(e.deltaX) < Math.abs(e.deltaY)) {
    // vertical scroll
    if (wheelCooldown || slideState.value !== 'idle') return
    const dir = e.deltaY > 0 ? 1 : -1
    await changeWeek(dir)
    wheelCooldown = true
    setTimeout(() => wheelCooldown = false, 350)
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

const emit = defineEmits<{
  (e: 'week-change'): void
  (e: 'jump-to-task', task: Task): void
}>()

async function animateToOffset(next: number, dir: number) {
  slideDir.value = dir
  slideState.value = 'sliding-out'
  closeDayPanel()

  await new Promise(r => setTimeout(r, 160))

  weekOffset.value = next
  displayOffset.value = next
  slideState.value = 'hidden'

  await new Promise(r => setTimeout(r, 10))

  slideState.value = 'sliding-in'
  emit('week-change')

  await new Promise(r => setTimeout(r, 180))
  slideState.value = 'idle'
}

async function changeWeek(dir: number) {
  const next = weekOffset.value + dir
  if (next < -4 || next > 4) return

  await animateToOffset(next, dir)
}

async function resetTimeline() {
  if (weekOffset.value === 0) return
  const dir = weekOffset.value > 0 ? -1 : 1
  await animateToOffset(0, dir)
}

async function focusOffset(offset: number) {
  const targetWeek = Math.floor(offset / 7)
  const boundedWeek = Math.max(-4, Math.min(4, targetWeek))

  if (boundedWeek !== weekOffset.value) {
    const dir = boundedWeek > weekOffset.value ? 1 : -1
    await animateToOffset(boundedWeek, dir)
  }
  selectedDayOffset.value = offset
}

defineExpose({ resetTimeline, focusOffset })

function handleKeyDown(e: KeyboardEvent) {
  if (e.code === 'Space' && !['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement).tagName)) {
    e.preventDefault()
    resetTimeline()
  }
}

onMounted(() => {
  // handled above
})

onUnmounted(() => {
  // handled above
})
</script>

<template>
  <div class="timeline-wrapper mb-8">
    <div style="overflow: hidden; border-radius: 4px;">
      <div
          v-if="!loading"
          class="grid grid-cols-7 gap-1 timeline-container transition-all duration-150"
          :class="{ 
          'opacity-0': slideState === 'sliding-out' || slideState === 'hidden',
          'translate-x-[-60px]': slideState === 'sliding-out' && slideDir > 0,
          'translate-x-[60px]': slideState === 'sliding-out' && slideDir < 0,
          'translate-x-[60px] transition-none opacity-0': slideState === 'hidden' && slideDir > 0,
          'translate-x-[-60px] transition-none opacity-0': slideState === 'hidden' && slideDir < 0,
        }"
          @wheel.prevent="handleWheel"
          @touchstart="handleTouchStart"
          @touchend="handleTouchEnd"
      >
        <div
            v-for="day in weekDays"
            :key="day.date.getTime()"
            class="day-cell flex flex-col items-center gap-[5px] pt-[11px] pb-[11px] px-[5px] rounded-sm cursor-pointer"
            :class="{ 
            'bg-swoosh-surface': day.isToday,
            'selected': selectedDayOffset === day.dayOffset,
            'today-selected': day.isToday && selectedDayOffset === day.dayOffset
          }"
            @click="toggleDay(day.dayOffset)"
        >
          <span class="font-mono text-[10px] tracking-widest uppercase text-swoosh-text-muted">{{ day.name }}</span>
          <span class="text-[23px] font-semibold leading-none" :class="day.isToday ? 'text-swoosh-text' : 'text-swoosh-text-muted'">{{ day.num }}</span>

          <div v-if="day.taskCount > 0"
               class="day-count min-w-[20px] h-5 rounded-full flex items-center justify-center text-[10px] font-bold font-mono px-1 border border-swoosh-border-hover bg-swoosh-surface-raised"
               :class="{ 
               'overdue-timeline-count': day.hasOverdue,
               'text-[#7090f0] border-[#648cff]/35 bg-[#648cff]/10 today-border-pulse': day.isToday && !day.hasOverdue,
               'text-swoosh-text-muted': !day.isToday && !day.hasOverdue && day.taskCount < 4,
               'text-swoosh-high border-swoosh-high/30 bg-swoosh-high/10': !day.isToday && !day.hasOverdue && day.taskCount >= 4
             }"
               v-animate-sync="day.hasOverdue ? { group: 'overdue', type: 'count' } : (day.isToday ? { group: 'today', type: 'border' } : null)"
          >
            {{ day.taskCount }}
          </div>
          <div v-else class="text-swoosh-text-faint text-[10px] h-5 flex items-center justify-center">·</div>
        </div>
      </div>
      <div v-else class="grid grid-cols-7 gap-1">
        <div v-for="i in 7" :key="i" class="flex flex-col items-center gap-[5px] pt-[11px] pb-[11px] px-[5px] rounded-sm bg-white/5 animate-pulse">
          <div class="h-2 w-8 bg-white/10 rounded-full mb-1"></div>
          <div class="h-6 w-6 bg-white/10 rounded-full"></div>
          <div class="h-3 w-3 bg-white/10 rounded-full mt-1"></div>
        </div>
      </div>
    </div>

    <!-- Day Panel -->
    <div class="day-panel-wrap" :class="{ 'open': selectedDayOffset !== null }">
      <div class="day-panel mt-3 border border-swoosh-border-hover rounded-sm bg-swoosh-surface overflow-hidden">
        <div class="day-panel-header flex items-center justify-between py-2.5 px-3.5 border-b border-swoosh-border">
          <span class="font-mono text-[10px] font-bold tracking-[0.1em] uppercase text-swoosh-text-muted">{{ selectedDay?.label }}</span>
          <button class="w-5 h-5 flex items-center justify-center text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors" @click="closeDayPanel">
            <X :size="12" stroke-width="2.5" />
          </button>
        </div>
        <div class="day-panel-tasks">
          <template v-if="selectedDay?.tasks.length">
            <div
                v-for="task in selectedDay.tasks"
                :key="task.id"
                class="day-panel-task flex items-center gap-3 py-2.5 px-3.5 border-b border-swoosh-border last:border-b-0 cursor-pointer hover:bg-swoosh-surface-raised transition-colors group"
                @click="jumpToTask(task)"
            >
              <div class="w-[7px] h-[7px] rotate-45 rounded-[1px] shrink-0" :class="getPriorityClass(task)"></div>
              <span class="flex-1 text-[13.5px] font-medium" :class="isTaskOverdue(task) ? 'text-swoosh-danger' : 'text-swoosh-text group-hover:text-swoosh-text'">{{ task.title }}</span>
              <span class="font-mono text-[11px]" :class="isTaskOverdue(task) ? 'text-swoosh-danger' : 'text-swoosh-text-faint'">{{ formatTime(task.deadline) }}</span>
            </div>
          </template>
          <div v-else class="py-4 px-3.5 text-[13px] text-swoosh-text-faint font-mono">No tasks due this day</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timeline-container {
  user-select: none;
}

.day-cell {
  /* Override Tailwind's transition-colors — it includes outline-color, which
     causes a flash when .selected adds an outline: the width snaps to 1.5px
     instantly while the color fades in from transparent. Only background-color
     needs to transition here. */
  transition: background-color 150ms;
}

.day-cell:hover {
  background: var(--color-swoosh-surface);
}

.day-cell.selected {
  background: var(--color-swoosh-surface-raised);
  outline: 1.5px solid var(--color-swoosh-border-hover);
  outline-offset: -1.5px;
}

.day-cell.today-selected {
  outline-color: rgba(100, 140, 255, 0.5);
}

.overdue-timeline-count {
  color: #ee4444;
  background-color: rgba(210, 50, 50, 0.12);
  border-color: rgba(210, 50, 50, 0.38);
}

.day-panel-wrap {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 0.22s ease, opacity 0.18s ease;
  opacity: 0;
  overflow: hidden;
  pointer-events: none;
}

.day-panel-wrap.open {
  grid-template-rows: 1fr;
  opacity: 1;
  pointer-events: all;
}

.day-panel {
  min-height: 0;
}

@keyframes highlightPulse {
  0% { background-color: transparent; }
  10% { background-color: rgba(232, 232, 234, 0.1); }
  100% { background-color: transparent; }
}

:global(.highlight-pulse) {
  animation: highlightPulse 2s ease-out;
}
</style>