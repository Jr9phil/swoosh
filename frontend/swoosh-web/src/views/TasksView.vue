<!-- 
  TasksView.vue
  Main view for displaying and managing tasks.
  Includes sections for pinned, incomplete, and completed tasks.
-->
<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch, nextTick } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { PRIORITIES } from '../types/priority'
import { useRouter } from 'vue-router'
import TaskEdit from '../components/TaskEdit.vue'
import TaskItem from '../components/TaskItem.vue'
import TaskSkeleton from '../components/TaskSkeleton.vue'
import TaskTimeline from '../components/TaskTimeline.vue'
import { Plus, Pin, X, ListPlus, CheckCircle, ChevronRight, CheckSquare } from 'lucide-vue-next'

const tasksStore = useTasksStore()
const auth = useAuthStore()
const router = useRouter()

// Reactive current time — updated every second for live deadline/overdue updates
const now = ref(Date.now())
let clockInterval: ReturnType<typeof setInterval>

// ── Computed task groups ────────────────────────────────────────────────────
const incompleteTasks = computed(() =>
    tasksStore.tasks
        .filter(t => !t.completed && !t.pinned)
        .slice()
        .sort((a, b) => {
          const overdueA = isOverdue(a.deadline), overdueB = isOverdue(b.deadline)
          if (overdueA !== overdueB) return overdueA ? -1 : 1
          const todayA = isDueToday(a.deadline),  todayB = isDueToday(b.deadline)
          if (todayA !== todayB) return todayA ? -1 : 1
          if (b.priority !== a.priority) return b.priority - a.priority
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        })
)

const tasksByPriority = computed(() =>
    PRIORITIES
        .map(p => ({ priority: p, tasks: incompleteTasks.value.filter(t => t.priority === p.value) }))
        .filter(g => g.tasks.length > 0)
        .reverse()
)

const pinnedTasks = computed(() =>
    tasksStore.tasks
        .filter(t => !t.completed && t.pinned)
        .slice()
        .sort((a, b) => {
          if (b.priority !== a.priority) return b.priority - a.priority
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        })
)

const completedTasks = computed(() =>
    tasksStore.tasks
        .filter(t => t.completed)
        .slice()
        .sort((a, b) => new Date(b.completed!).getTime() - new Date(a.completed!).getTime())
)

const overdueCount = computed(() =>
    tasksStore.tasks.filter(t => !t.completed && isOverdue(t.deadline)).length
)

const isEverythingEmpty = computed(() => tasksStore.tasks.length === 0)
const isEverythingCompleted = computed(() => tasksStore.tasks.length > 0 && incompleteTasks.value.length === 0 && pinnedTasks.value.length === 0)

// ── Date helpers ────────────────────────────────────────────────────────────
function isSameDay(d1: Date, d2: Date) {
  return d1.getFullYear() === d2.getFullYear() &&
      d1.getMonth()    === d2.getMonth()    &&
      d1.getDate()     === d2.getDate()
}
function isDueToday(deadline?: string | null) {
  if (!deadline) return false
  const d = new Date(deadline)
  return isSameDay(new Date(now.value), d) && now.value <= d.getTime()
}
function isOverdue(deadline?: string | null) {
  if (!deadline) return false
  const d = new Date(deadline)
  return now.value > d.getTime()
}

function hasOverdueInGroup(tasks: any[]) { return tasks.some(t => !t.completed && isOverdue(t.deadline)) }
function hasTodayInGroup(tasks: any[])   { return tasks.some(t => !t.completed && isDueToday(t.deadline)) }

// ── Header date ─────────────────────────────────────────────────────────────
const formattedToday = computed(() => {
  const d = new Date()
  const days   = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat']
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
  return `${days[d.getDay()]} ${months[d.getMonth()]} ${d.getDate()}`.toUpperCase()
})

// ── Section collapse ─────────────────────────────────────────────────────────
const taskTimeline = ref<InstanceType<typeof TaskTimeline> | null>(null)
const completedExpanded = ref(false)
const priorityExpanded = ref<Record<string, boolean>>({
  '0': true, '1': true, '2': true, '3': true, 'pinned': true
})
// Tracks which sections are mid-animation so overflow:hidden can be applied
// only during the transition, preventing permanent clipping of dropdown menus.
const animatingSections = ref<Set<string>>(new Set())

function togglePriority(val: number | string) {
  const key = val.toString()
  priorityExpanded.value[key] = !priorityExpanded.value[key]

  // Add overflow clip for the transition duration, then remove it
  animatingSections.value = new Set([...animatingSections.value, key])
  setTimeout(() => {
    animatingSections.value.delete(key)
    animatingSections.value = new Set(animatingSections.value) // trigger reactivity
  }, 240) // slightly longer than the 220ms grid transition
}

// ── Lifecycle ────────────────────────────────────────────────────────────────
onMounted(async () => {
  clockInterval = setInterval(() => { now.value = Date.now() }, 1000)
  try {
    await tasksStore.fetchTasks()
  } catch {
    auth.logout()
    router.push('/login')
  }
})

onUnmounted(() => {
  if (clockInterval) clearInterval(clockInterval)
})

// ── Jump to overdue ──────────────────────────────────────────────────────────
function jumpToOverdue() {
  Object.keys(priorityExpanded.value).forEach(key => {
    const tasks = key === 'pinned'
        ? pinnedTasks.value
        : incompleteTasks.value.filter(t => t.priority === parseInt(key))
    if (!hasOverdueInGroup(tasks)) priorityExpanded.value[key] = false
  })
  if (hasOverdueInGroup(pinnedTasks.value)) priorityExpanded.value['pinned'] = true
  tasksByPriority.value.forEach(group => {
    if (hasOverdueInGroup(group.tasks)) priorityExpanded.value[group.priority.value.toString()] = true
  })

  const firstOverdue = tasksStore.tasks.find(t => !t.completed && isOverdue(t.deadline))
  if (firstOverdue) {
    // 1. Focus the timeline on this task's day
    const d = new Date(firstOverdue.deadline!)
    const today = new Date()
    today.setHours(0,0,0,0)
    d.setHours(0,0,0,0)
    const offset = Math.round((d.getTime() - today.getTime()) / 86400000)
    taskTimeline.value?.focusOffset(offset)

    // 2. Jump to the task in the list
    nextTick(() => {
      const el = document.getElementById('task-' + firstOverdue.id)
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' })
        el.classList.add('highlight-pulse')
        setTimeout(() => el.classList.remove('highlight-pulse'), 2000)
      }
    })
  }
}

// ── Jump to a specific task from the timeline panel ─────────────────────────
// TaskTimeline emits this when a task is clicked. We must expand the task's
// section first (it may be collapsed), wait for Vue to render, then scroll.
function handleJumpToTask(task: any) {
  // Determine which section key this task lives in
  const key = task.pinned &&
  !isOverdue(task.deadline) &&
  !isDueToday(task.deadline)
      ? 'pinned'
      : task.priority.toString()

  const wasCollapsed = !priorityExpanded.value[key]
  priorityExpanded.value[key] = true

  // If the section was collapsed we need to wait for the expand animation
  // to finish before scrollIntoView will find a visible element
  const delay = wasCollapsed ? 250 : 0
  setTimeout(() => {
    nextTick(() => {
      const el = document.getElementById('task-' + task.id)
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' })
        el.classList.add('highlight-pulse')
        setTimeout(() => el.classList.remove('highlight-pulse'), 2000)
      }
    })
  }, delay)
}
const createTaskEdit = ref<any>(null)
function handleModalClose() { createTaskEdit.value?.resetForm() }
function openModal() { (document.getElementById('create_modal') as HTMLDialogElement)?.showModal() }
function closeModal() { (document.getElementById('create_modal') as HTMLDialogElement)?.close() }
</script>

<template>
  <main class="flex-1 flex justify-center pt-6 px-5 pb-[60px]">
    <div class="w-full max-w-[540px]">

      <!-- ── Header ── -->
      <header class="flex items-center gap-3.5 mb-8 pb-[22px] border-b border-swoosh">
        <button
            class="icon-btn rounded-sm"
            @click="openModal"
        >
          <Plus :size="18" stroke-width="2.5" />
        </button>
        <span class="text-[22px] font-extrabold tracking-[-0.01em] text-base-content flex-1">My Tasks</span>
        <!-- Clicking the date label resets the timeline to the current week -->
        <div
            class="font-mono text-xs font-bold text-swoosh-text-muted tracking-[0.04em] whitespace-nowrap cursor-pointer select-none transition-colors hover:text-swoosh-text"
            @click="() => taskTimeline?.resetTimeline()"
        >
          {{ formattedToday }}
        </div>
      </header>

      <!-- ── Overdue banner ── -->
      <div v-if="overdueCount > 0" class="overdue-banner" id="overdue-banner" v-animate-sync:overdue="'banner'">
        <span class="overdue-banner-dot" v-animate-sync:overdue="'dot'"></span>
        <span class="overdue-banner-text">{{ overdueCount }} overdue task{{ overdueCount !== 1 ? 's' : '' }}</span>
        <button class="overdue-banner-action" @click="jumpToOverdue">
          View
          <ChevronRight :size="12"/>
        </button>
      </div>

      <!-- ── Timeline ── -->
      <TaskTimeline ref="taskTimeline" :loading="tasksStore.loading" @jump-to-task="handleJumpToTask" />

      <!-- ── Loading State ── -->
      <div v-if="tasksStore.loading" class="space-y-8">
        <div>
          <div class="section-label skeleton animate-pulse h-[34px] w-32 bg-white/5 rounded-sm mb-4"></div>
          <div class="space-y-px">
            <TaskSkeleton v-for="i in 3" :key="i" />
          </div>
        </div>
        <div>
          <div class="section-label skeleton animate-pulse h-[34px] w-40 bg-white/5 rounded-sm mb-4"></div>
          <div class="space-y-px">
            <TaskSkeleton v-for="i in 2" :key="i" />
          </div>
        </div>
      </div>

      <template v-else>
        <!-- ── No tasks yet ── -->
        <div v-if="isEverythingEmpty" class="flex flex-col items-center justify-center py-24 text-center">
          <div class="w-[72px] h-[72px] rounded-full bg-white/5 flex items-center justify-center mb-6">
            <ListPlus :size="32" stroke-width="1.5" class="text-swoosh-text-faint" />
          </div>
          <h3 class="text-[18px] font-bold text-base-content mb-1">No tasks yet</h3>
          <p class="text-[13px] text-swoosh-text-faint font-mono uppercase tracking-[0.1em]">Create a new task to get started </p>
          <button class="btn bg-base-300 border-[1.5px] border-swoosh-border-hover rounded-sm text-base-content cursor-pointer mt-4" @click="openModal"><Plus /> Add a task</button>
        </div>

        <!-- ── All tasks completed ── -->
        <div v-else-if="isEverythingCompleted" class="flex flex-col items-center justify-center py-16 text-center">
          <div class="w-[72px] h-[72px] rounded-full bg-success/5 flex items-center justify-center mb-6">
            <CheckCircle :size="32" stroke-width="1.5" class="text-success/40" />
          </div>
          <h3 class="text-[18px] font-bold text-base-content mb-1">All tasks completed</h3>
          <p class="text-[13px] text-swoosh-text-faint font-mono uppercase tracking-[0.1em]">You're all caught up for now</p>
        </div>

        <!-- ── Pinned Section ── -->
        <div v-if="pinnedTasks.length">
        <div
            class="section-label pinned"
            :class="{ open: priorityExpanded.pinned }"
            @click="togglePriority('pinned')"
        >
          <div class="section-label-left">
            <Pin :size="14" />
            <span>Pinned</span>
            <!-- Overdue dot: LEFT = visible when EXPANDED, per mockup -->
            <span v-if="hasOverdueInGroup(pinnedTasks) && priorityExpanded.pinned" v-animate-sync:overdue="'dot'" class="overdue-dot left"></span>
          </div>
          <div class="section-label-right">
            <!-- Right-column indicators: shown only when COLLAPSED -->
            <span v-if="hasOverdueInGroup(pinnedTasks) && !priorityExpanded.pinned" v-animate-sync:overdue="'dot'" class="overdue-dot right"></span>
            <span v-else-if="hasTodayInGroup(pinnedTasks) && !priorityExpanded.pinned" v-animate-sync:today="'dot'" class="today-dot"></span>
            <span class="section-count">{{ pinnedTasks.length }}</span>
            <span class="section-toggle"></span>
          </div>
        </div>
        <div class="section-body" :class="{ collapsed: !priorityExpanded.pinned, 'is-animating': animatingSections.has('pinned') }">
          <div>
            <div class="task-group pinned">
              <TaskItem v-for="task in pinnedTasks" :key="task.id" :task="task" />
            </div>
          </div>
        </div>
        <div class="section-divider"></div>
      </div>

      <!-- ── Priority Sections ── -->
      <template v-for="group in tasksByPriority" :key="group.priority.value">
        <div
            class="section-label"
            :class="[group.priority.class, { open: priorityExpanded[group.priority.value] }]"
            @click="togglePriority(group.priority.value)"
        >
          <div class="section-label-left">
            <component :is="group.priority.icon" :size="14" fill="currentColor" />
            <span>{{ group.priority.label }}</span>
            <!-- Overdue dot: LEFT = visible when EXPANDED -->
            <span v-if="hasOverdueInGroup(group.tasks) && priorityExpanded[group.priority.value]" v-animate-sync:overdue="'dot'" class="overdue-dot left"></span>
          </div>
          <div class="section-label-right">
            <!-- Right-column indicators: shown only when COLLAPSED -->
            <span v-if="hasOverdueInGroup(group.tasks) && !priorityExpanded[group.priority.value]" v-animate-sync:overdue="'dot'" class="overdue-dot right"></span>
            <span v-else-if="hasTodayInGroup(group.tasks) && !priorityExpanded[group.priority.value]" v-animate-sync:today="'dot'" class="today-dot"></span>
            <span class="section-count">{{ group.tasks.length }}</span>
            <span class="section-toggle"></span>
          </div>
        </div>
        <div class="section-body" :class="{ collapsed: !priorityExpanded[group.priority.value], 'is-animating': animatingSections.has(group.priority.value.toString()) }">
          <div>
            <!-- pinned/high/med/low get a coloured left accent — none does not -->
            <div class="task-group" :class="{ high: group.priority.value === 3, med: group.priority.value === 2, low: group.priority.value === 1 }">
              <TaskItem v-for="task in group.tasks" :key="task.id" :task="task" />
            </div>
          </div>
        </div>
      </template>

      <!-- ── Completed Section ── -->
      <div v-if="completedTasks.length">
        <div class="spacer"></div>
        <div
            class="collapse-header"
            :class="{ open: completedExpanded }"
            @click="completedExpanded = !completedExpanded"
        >
          <ChevronRight :size="13" stroke-width="2.5" />
          Completed ({{ completedTasks.length }})
        </div>
        <div v-if="completedExpanded" class="task-group mt-1">
          <TaskItem v-for="task in completedTasks" :key="task.id" :task="task" />
        </div>
      </div>
    </template>

    </div>

    <!-- ── Create Task Modal ── -->
    <dialog id="create_modal" class="modal bg-black/60 backdrop-blur-[2px]" @close="handleModalClose">
      <div class="modal-box bg-base-200 border border-swoosh-border-hover p-0 max-w-[520px] rounded-sm overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.7)]">
        <!-- Modal header: 16px top, 20px sides, 14px bottom — matches mockup -->
        <div class="flex items-center justify-between px-5 pt-4 pb-[14px] border-b border-swoosh">
          <div class="flex items-center gap-2 font-mono text-[12px] tracking-[0.10em] uppercase text-swoosh-text-muted">
            <CheckSquare :size="15" stroke-width="2" />
            New Task
          </div>
          <button
              @click="closeModal"
              class="w-7 h-7 flex items-center justify-center rounded-sm text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors"
          >
            <X :size="15" stroke-width="2" />
          </button>
        </div>
        <!-- No padding wrapper — TaskEdit owns body + footer padding in create mode -->
        <TaskEdit ref="createTaskEdit" @close="closeModal" @created="closeModal" />
      </div>
      <form method="dialog" class="modal-backdrop">
        <button>close</button>
      </form>
    </dialog>
  </main>
</template>