<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, nextTick } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { PRIORITIES } from '../types/priority'
import type { Task } from '../types/task'
import { useRouter } from 'vue-router'
import TaskEdit from '../components/TaskEdit.vue'
import TaskItem from '../components/TaskItem.vue'
import TaskSkeleton from '../components/TaskSkeleton.vue'
import TaskTimeline from '../components/TaskTimeline.vue'
import ImgHeader from '../components/ImgHeader.vue'
import { VueDraggable } from 'vue-draggable-plus'
import { useTaskDrag } from '../composables/useTaskDrag'
import { Plus, Pin, X, ListPlus, CheckCircle, ChevronRight, CheckSquare } from 'lucide-vue-next'

const tasksStore = useTasksStore()
const auth = useAuthStore()
const router = useRouter()

const now = ref(Date.now())
let clockInterval: ReturnType<typeof setInterval>

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

const onlyNoPriority = computed(() =>
    tasksByPriority.value.length === 1 && tasksByPriority.value[0].priority.value === 0
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


const taskTimeline = ref<InstanceType<typeof TaskTimeline> | null>(null)
const completedExpanded = ref(false)
const priorityExpanded = ref<Record<string, boolean>>({
  '0': true, '1': true, '2': true, '3': true, 'pinned': true
})
// Tracks sections mid-animation so overflow:hidden is applied only during the
// transition, preventing it from permanently clipping dropdown menus.
const animatingSections = ref<Set<string>>(new Set())

function togglePriority(val: number | string) {
  const key = val.toString()
  priorityExpanded.value[key] = !priorityExpanded.value[key]

  animatingSections.value = new Set([...animatingSections.value, key])
  setTimeout(() => {
    animatingSections.value.delete(key)
    animatingSections.value = new Set(animatingSections.value) // trigger reactivity
  }, 240) // slightly longer than the 220ms grid transition
}

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
    const d = new Date(firstOverdue.deadline!)
    const today = new Date()
    today.setHours(0,0,0,0)
    d.setHours(0,0,0,0)
    const offset = Math.round((d.getTime() - today.getTime()) / 86400000)
    taskTimeline.value?.focusOffset(offset)

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

// Expand the task's section if collapsed, then scroll to it.
// Called when a task is clicked in the timeline panel.
function handleJumpToTask(task: any) {
  const key = task.pinned &&
  !isOverdue(task.deadline) &&
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
const createTaskEdit = ref<any>(null)
function handleModalClose() { createTaskEdit.value?.resetForm() }
function openModal() { (document.getElementById('create_modal') as HTMLDialogElement)?.showModal() }
function closeModal() { (document.getElementById('create_modal') as HTMLDialogElement)?.close() }

function handleCreateTaskForDate(date: string) {
  openModal()
  nextTick(() => { createTaskEdit.value?.setDate(date) })
}

const { draggableGroups, handleDragChoose, handleModelUpdate, onGroupDragEnd } = useTaskDrag(tasksByPriority)

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

      <!-- ── Image Header ── -->
      <ImgHeader @open-modal="openModal" @reset-timeline="taskTimeline?.resetTimeline()" />

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
      <TaskTimeline ref="taskTimeline" :loading="tasksStore.loading" @jump-to-task="handleJumpToTask" @create-task-for-date="handleCreateTaskForDate" />

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
          <button class="btn bg-base-300 border-[1.5px] border-swoosh-border-hover rounded-sm text-base-content cursor-pointer mt-4" @click="openModal"><Plus /> Add a task</button>
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
            @click="togglePriority('pinned')"
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
        <!-- When the only group is no-priority, render tasks directly without a header -->
        <template v-if="onlyNoPriority">
          <VueDraggable
            :model-value="draggableGroups[group.priority.value] ?? []"
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
            <TaskItem v-for="task in (draggableGroups[group.priority.value] ?? []).filter(t => group.tasks.some(gt => gt.id === t.id))" :key="task.id" :task="task" />
          </VueDraggable>
        </template>
        <template v-else>
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
              <VueDraggable
                :model-value="draggableGroups[group.priority.value] ?? []"
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
                <TaskItem v-for="task in (draggableGroups[group.priority.value] ?? []).filter(t => group.tasks.some(gt => gt.id === t.id))" :key="task.id" :task="task" />
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
      <div class="modal-box bg-base-200 border border-swoosh-border-hover p-0 max-w-[520px] xl:max-w-[640px] rounded-[10px] overflow-hidden shadow-[0_0_0_1px_rgba(255,255,255,0.06),0_24px_64px_rgba(0,0,0,0.8)]">
        <!-- Modal header -->
        <div class="flex items-center justify-between px-5 pt-4 pb-[14px] border-b border-swoosh bg-base-300 rounded-t-[10px]">
          <div class="flex items-center gap-2 font-mono text-[11px] tracking-[0.14em] uppercase text-swoosh-text-muted">
            <CheckSquare :size="13" stroke-width="2" />
            New Task
          </div>
          <button
              @click="closeModal"
              class="w-7 h-7 flex items-center justify-center rounded-[6px] text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors"
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