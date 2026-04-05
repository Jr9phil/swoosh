<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, nextTick, provide } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { useUiStore } from '../stores/ui'
import { useRecurringStore } from '../stores/recurring'
import { PRIORITIES } from '../types/priority'
import type { Task } from '../types/task'
import { useRouter } from 'vue-router'
import TaskItem from '../components/TaskItem.vue'
import TaskSkeleton from '../components/TaskSkeleton.vue'
import ImgHeader from '../components/ImgHeader.vue'
import { VueDraggable } from 'vue-draggable-plus'
import { useTaskDrag } from '../composables/useTaskDrag'
import { Plus, Pin, ListPlus, CheckCircle, ChevronRight } from 'lucide-vue-next'

const tasksStore = useTasksStore()
const recurringStore = useRecurringStore()
const auth = useAuthStore()
const ui = useUiStore()
const router = useRouter()

const now = ref(Date.now())
let clockInterval: ReturnType<typeof setInterval>

const incompleteTasks = computed(() =>
    tasksStore.tasks
        .filter(t => !t.completed && !t.pinned && !t.parentId)
        .slice()
        .sort((a, b) => {
          const overdueA = isOverdue(a.deadline, a.timerDuration), overdueB = isOverdue(b.deadline, b.timerDuration)
          if (overdueA !== overdueB) return overdueA ? -1 : 1
          const graceA = isInGrace(a.deadline, a.timerDuration), graceB = isInGrace(b.deadline, b.timerDuration)
          if (graceA !== graceB) return graceA ? -1 : 1
          const todayA = isDueToday(a.deadline), todayB = isDueToday(b.deadline)
          if (todayA !== todayB) return todayA ? -1 : 1
          if (b.priority !== a.priority) return b.priority - a.priority
          return new Date(b.modified).getTime() - new Date(a.modified).getTime()
        })
)

const tasksByPriority = computed(() =>
    PRIORITIES
        .map(p => ({ priority: p, tasks: incompleteTasks.value.filter(t => t.priority === p.value) }))
        .filter(g => g.tasks.length > 0)
        .reverse()
)

const onlyNoPriority = computed(() =>
    tasksByPriority.value.length === 1 && tasksByPriority.value[0].priority.value === 0
)

const pinnedTasks = computed(() =>
    tasksStore.tasks
        .filter(t => !t.completed && t.pinned && !t.parentId)
        .slice()
        .sort((a, b) => {
          if (b.priority !== a.priority) return b.priority - a.priority
          return new Date(b.modified).getTime() - new Date(a.modified).getTime()
        })
)

const completedTasks = computed(() =>
    tasksStore.tasks
        .filter(t => t.completed && !t.parentId)
        .slice()
        .sort((a, b) => new Date(b.completed!).getTime() - new Date(a.completed!).getTime())
)

const overdueCount = computed(() =>
    tasksStore.tasks.filter(t => !t.completed && isOverdue(t.deadline, t.timerDuration)).length
)

const isEverythingEmpty = computed(() => tasksStore.tasks.length === 0)
const isEverythingCompleted = computed(() => tasksStore.tasks.length > 0 && incompleteTasks.value.length === 0 && pinnedTasks.value.length === 0)

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
function isOverdue(deadline?: string | null, timerDuration?: number | null) {
  if (!deadline) return false
  const d = new Date(deadline)
  const expiry = d.getTime() + (timerDuration ?? 0)
  return now.value > expiry
}
function isInGrace(deadline?: string | null, timerDuration?: number | null) {
  if (!deadline || !timerDuration) return false
  const d = new Date(deadline)
  const expiry = d.getTime() + timerDuration
  return now.value > d.getTime() && now.value <= expiry
}

function hasOverdueInGroup(tasks: any[]) {
  return tasks.some(t => {
    if (!t.completed && (isOverdue(t.deadline, t.timerDuration) || isInGrace(t.deadline, t.timerDuration))) return true
    if (t.parentId) return false // Subtasks don't have nested subtasks in this app
    return tasksStore.tasks.some(st => st.parentId === t.id && !st.completed && (isOverdue(st.deadline, st.timerDuration) || isInGrace(st.deadline, st.timerDuration)))
  })
}
function hasTodayInGroup(tasks: any[]) {
  return tasks.some(t => {
    if (!t.completed && isDueToday(t.deadline)) return true
    if (t.parentId) return false
    return tasksStore.tasks.some(st => st.parentId === t.id && !st.completed && isDueToday(st.deadline))
  })
}


const imgHeader = ref<InstanceType<typeof ImgHeader> | null>(null)
const completedExpanded = ref(false)
const priorityExpanded = ref<Record<string, boolean>>({
  '0': true, '1': true, '2': true, '3': true, 'pinned': true
})
// Tracks sections mid-animation so overflow:hidden is applied only during the
// transition, preventing it from permanently clipping dropdown menus.
const animatingSections = ref<Set<string>>(new Set())

function togglePriority(val: number | string, event?: MouseEvent) {
  const key = val.toString()
  completedExpanded.value = false

  if (event?.shiftKey) {
    const isOnlyOneOpen = priorityExpanded.value[key] &&
      Object.keys(priorityExpanded.value).every(k => k === key || !priorityExpanded.value[k])

    Object.keys(priorityExpanded.value).forEach(k => {
      const nextState = isOnlyOneOpen ? true : k === key
      if (priorityExpanded.value[k] !== nextState) {
        priorityExpanded.value[k] = nextState
        animatingSections.value = new Set([...animatingSections.value, k])
        setTimeout(() => {
          animatingSections.value.delete(k)
          animatingSections.value = new Set(animatingSections.value)
        }, 240)
      }
    })
  } else {
    priorityExpanded.value[key] = !priorityExpanded.value[key]

    animatingSections.value = new Set([...animatingSections.value, key])
    setTimeout(() => {
      animatingSections.value.delete(key)
      animatingSections.value = new Set(animatingSections.value) // trigger reactivity
    }, 240) // slightly longer than the 220ms grid transition
  }
}

function toggleCompleted(_?: MouseEvent) {
  completedExpanded.value = !completedExpanded.value
}

const isLongPressActive = ref(false)
let longPressTimer: ReturnType<typeof setTimeout> | null = null

function startLongPress(val: number | string, isCompleted = false) {
  isLongPressActive.value = false
  if (longPressTimer) clearTimeout(longPressTimer)
  longPressTimer = setTimeout(() => {
    isLongPressActive.value = true
    if (isCompleted) {
      toggleCompleted({ shiftKey: true } as MouseEvent)
    } else {
      togglePriority(val, { shiftKey: true } as MouseEvent)
    }
    if (window.navigator.vibrate) window.navigator.vibrate(50)
  }, 1500)
}

function cancelLongPress() {
  if (longPressTimer) {
    clearTimeout(longPressTimer)
    longPressTimer = null
  }
}

function handleTouchEnd() {
  cancelLongPress()
  if (isLongPressActive.value) {
    setTimeout(() => { isLongPressActive.value = false }, 500)
  }
}

function handleHeaderClick(val: number | string, event: MouseEvent, isCompleted = false) {
  if (isLongPressActive.value) {
    isLongPressActive.value = false
    return
  }
  if (isCompleted) {
    toggleCompleted(event)
  } else {
    togglePriority(val, event)
  }
}


onMounted(async () => {
  clockInterval = setInterval(() => { now.value = Date.now() }, 1000)
  try {
    await Promise.all([tasksStore.fetchTasks(), recurringStore.fetchAll()])
  } catch {
    auth.logout()
    router.push('/login')
  }
})

onUnmounted(() => {
  if (clockInterval) clearInterval(clockInterval)
})

function jumpToOverdue() {
  completedExpanded.value = false
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

  const firstOverdue = tasksStore.tasks.find(t => !t.completed && isOverdue(t.deadline, t.timerDuration))
  if (firstOverdue) {
    const targetTask = firstOverdue.parentId
        ? tasksStore.tasks.find(t => t.id === firstOverdue.parentId)
        : firstOverdue

    if (!targetTask) return

    const d = new Date(firstOverdue.deadline!)
    const today = new Date()
    today.setHours(0,0,0,0)
    d.setHours(0,0,0,0)
    const offset = Math.round((d.getTime() - today.getTime()) / 86400000)
    imgHeader.value?.focusOffset(offset)

    nextTick(() => {
      const el = document.getElementById('task-' + targetTask.id)
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' })
        el.classList.add('highlight-pulse')
        setTimeout(() => el.classList.remove('highlight-pulse'), 2000)
      }
    })
  }
}

// Expand the task's section if collapsed, then scroll to it.
// Called when a task is clicked in the timeline panel.
function handleJumpToTask(task: any) {
  if (!task.completed) completedExpanded.value = false

  const key = task.pinned &&
  !isOverdue(task.deadline, task.timerDuration) &&
  !isDueToday(task.deadline)
      ? 'pinned'
      : task.priority.toString()

  const wasCollapsed = !priorityExpanded.value[key]
  priorityExpanded.value[key] = true

  // Wait for the expand animation before scrollIntoView
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
function handleCreateTaskForDate(date: string) {
  ui.triggerCreateModal('task', { date })
}

function openSeparateTask(task: Task, priority?: number) {
  ui.triggerCreateModal('task', {
    prefill: { title: task.title, notes: task.notes, deadline: task.deadline, priority },
    onCreated: () => tasksStore.deleteTask(task.id),
  })
}

provide('openSeparateTask', openSeparateTask)

const { draggableGroups, displayGroups, handleDragChoose, handleModelUpdate, onGroupDragEnd } = useTaskDrag(tasksByPriority)

provide('draggableGroups', draggableGroups)

// ── Skeleton data — realistic mix of variants and widths ──────────────────────
const skeletonSections = [
  {
    labelWidth: '88px',
    items: [
      { variant: 'badge'   as const, titleWidth: '72%' },
      { variant: 'default' as const, titleWidth: '52%' },
      { variant: 'notes'   as const, titleWidth: '80%' },
    ],
  },
  {
    labelWidth: '72px',
    items: [
      { variant: 'default' as const, titleWidth: '64%' },
      { variant: 'badge'   as const, titleWidth: '76%' },
    ],
  },
  {
    labelWidth: '104px',
    items: [
      { variant: 'default' as const, titleWidth: '48%' },
    ],
  },
]
</script>

<template>
  <main class="flex-1 flex justify-center pt-6 px-5 pb-[60px] xl:pt-8 xl:px-10 xl:pb-[70px]">
    <div class="w-full max-w-[540px] xl:max-w-[700px] 2xl:max-w-[840px]">

      <!-- ── Image Header + Timeline ── -->
      <ImgHeader ref="imgHeader" :loading="tasksStore.loading" @open-modal="ui.triggerCreateModal('task')" @jump-to-task="handleJumpToTask" @create-task-for-date="handleCreateTaskForDate" />

      <!-- ── Overdue banner ── -->
      <div v-if="overdueCount > 0" class="overdue-banner" id="overdue-banner" v-animate-sync:overdue="'banner'">
        <span class="overdue-banner-dot" v-animate-sync:overdue="'dot'"></span>
        <span class="overdue-banner-text">{{ overdueCount }} overdue task{{ overdueCount !== 1 ? 's' : '' }}</span>
        <button class="overdue-banner-action" @click="jumpToOverdue">
          View
          <ChevronRight :size="12"/>
        </button>
      </div>

      <!-- ── Loading State ── -->
      <div v-if="tasksStore.loading" class="space-y-8">
        <div v-for="(section, si) in skeletonSections" :key="si">
          <!-- Section header skeleton — mirrors real section-label structure -->
          <div class="flex items-center justify-between py-[10px]">
            <div class="flex items-center gap-2">
              <div class="w-[9px] h-[9px] rounded-sm sk-block" :style="{ animationDelay: `${-si * 200}ms` }"></div>
              <div class="h-[9px] rounded-sm sk-block" :style="{ width: section.labelWidth, animationDelay: `${-(si * 200 + 80)}ms` }"></div>
            </div>
            <div class="flex items-center gap-2">
              <div class="w-5 h-[16px] rounded-full sk-block" :style="{ animationDelay: `${-(si * 200 + 160)}ms` }"></div>
              <div class="w-4 h-[9px] rounded-sm sk-block" :style="{ animationDelay: `${-(si * 200 + 240)}ms` }"></div>
            </div>
          </div>
          <!-- Task card skeleton -->
          <div class="task-group">
            <TaskSkeleton
              v-for="(item, ii) in section.items"
              :key="ii"
              :variant="item.variant"
              :title-width="item.titleWidth"
              :index="si * 4 + ii"
            />
          </div>
        </div>
      </div>

      <template v-else>
        <!-- ── No tasks yet ── -->
        <div v-if="isEverythingEmpty" class="flex flex-col items-center justify-center py-24 text-center">
          <div class="empty-state-icon bg-white/5">
            <ListPlus :size="32" stroke-width="1.5" class="text-swoosh-text-faint" />
          </div>
          <h3 class="empty-state-title">No tasks yet</h3>
          <p class="empty-state-text">Create a new task to get started</p>
          <button class="btn bg-base-300 border-[1.5px] border-swoosh-border-hover rounded-sm text-base-content cursor-pointer mt-4" @click="ui.triggerCreateModal('task')"><Plus /> Add a task</button>
        </div>

        <!-- ── All tasks completed ── -->
        <div v-else-if="isEverythingCompleted" class="flex flex-col items-center justify-center py-16 text-center">
          <div class="empty-state-icon bg-success/5">
            <CheckCircle :size="32" stroke-width="1.5" class="text-success/40" />
          </div>
          <h3 class="empty-state-title">All tasks completed</h3>
          <p class="empty-state-text">You're all caught up for now</p>
        </div>

        <!-- ── Pinned Section ── -->
        <div v-if="pinnedTasks.length">
        <div
            class="section-label pinned"
            :class="{ open: priorityExpanded.pinned }"
            @click="handleHeaderClick('pinned', $event)"
            @touchstart="startLongPress('pinned')"
            @touchend="handleTouchEnd"
            @touchmove="cancelLongPress"
            @contextmenu.prevent
        >
          <div class="section-label-left">
            <Pin :size="14" fill="currentColor"/>
            <span>Pinned</span>
            <span v-if="hasOverdueInGroup(pinnedTasks) && priorityExpanded.pinned" v-animate-sync:overdue="'dot'" class="overdue-dot left"></span>
          </div>
          <div class="section-label-right">
            <!-- Right-column indicators: shown only when COLLAPSED -->
            <span v-if="hasOverdueInGroup(pinnedTasks) && !priorityExpanded.pinned" v-animate-sync:overdue="'dot'" class="overdue-dot right"></span>
            <span v-else-if="hasTodayInGroup(pinnedTasks) && !priorityExpanded.pinned" v-animate-sync:today="'dot'" class="today-dot"></span>
            <span class="section-count">{{ pinnedTasks.length }}</span>
            <ChevronRight :size="14" stroke-width="2.5" class="section-toggle" />
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
        <!-- When the only group is no-priority, render tasks directly without a header -->
        <template v-if="onlyNoPriority">
          <VueDraggable
            :model-value="displayGroups[group.priority.value] ?? []"
            @update:model-value="(val: Task[]) => handleModelUpdate(group.priority.value, val)"
            class="task-group mt-8"
            :group="{ name: 'tasks' }"
            :data-priority="group.priority.value"
            :animation="150"
            :delay="500"
            :delay-on-touch-only="true"
            ghost-class="drag-ghost"
            @choose="handleDragChoose"
            @end="(evt: any) => onGroupDragEnd(evt, group.priority.value)"
          >
            <TaskItem v-for="task in displayGroups[group.priority.value] ?? []" :key="task.id" :task="task" />
          </VueDraggable>
        </template>
        <template v-else>
          <div
              class="section-label"
              :class="[group.priority.class, { open: priorityExpanded[group.priority.value] }]"
              @click="handleHeaderClick(group.priority.value, $event)"
              @touchstart="startLongPress(group.priority.value)"
              @touchend="handleTouchEnd"
              @touchmove="cancelLongPress"
              @contextmenu.prevent
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
              <ChevronRight :size="14" stroke-width="2.5" class="section-toggle" />
            </div>
          </div>
          <div class="section-body" :class="{ collapsed: !priorityExpanded[group.priority.value], 'is-animating': animatingSections.has(group.priority.value.toString()) }">
            <div>
              <!-- pinned/high/med/low get a coloured left accent — none does not -->
              <VueDraggable
                :model-value="displayGroups[group.priority.value] ?? []"
                @update:model-value="(val: Task[]) => handleModelUpdate(group.priority.value, val)"
                class="task-group"
                :class="{ high: group.priority.value === 3, med: group.priority.value === 2, low: group.priority.value === 1 }"
                :group="{ name: 'tasks' }"
                :data-priority="group.priority.value"
                :animation="150"
                :delay="500"
                :delay-on-touch-only="true"
                ghost-class="drag-ghost"
                @choose="handleDragChoose"
                @end="(evt: any) => onGroupDragEnd(evt, group.priority.value)"
              >
                <TaskItem v-for="task in displayGroups[group.priority.value] ?? []" :key="task.id" :task="task" />
              </VueDraggable>
            </div>
          </div>
        </template>
      </template>

      <!-- ── Completed Section ── -->
      <div v-if="completedTasks.length">
        <div class="spacer"></div>
        <div
            class="collapse-header"
            :class="{ open: completedExpanded }"
            @click="handleHeaderClick('', $event, true)"
            @touchstart="startLongPress('', true)"
            @touchend="handleTouchEnd"
            @touchmove="cancelLongPress"
            @contextmenu.prevent
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

  </main>
</template>

<style scoped>
.sk-block {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.04) 25%,
    rgba(255, 255, 255, 0.10) 50%,
    rgba(255, 255, 255, 0.04) 75%
  );
  background-size: 200% 100%;
  animation: sk-shimmer 2s linear infinite;
}

@keyframes sk-shimmer {
  0%   { background-position:  200% 0; }
  100% { background-position: -200% 0; }
}
</style>