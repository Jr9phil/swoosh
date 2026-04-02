<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref, computed, watch, onMounted, onUnmounted, inject, nextTick } from 'vue'
import TaskEdit from './TaskEdit.vue'
import SubtaskEdit from './SubtaskEdit.vue'
import TaskRating from './TaskRating.vue'
import TaskIcon from './TaskIcon.vue'
import { Calendar, Clock, Timer } from 'lucide-vue-next'
import { VueDraggable } from 'vue-draggable-plus'

const props = defineProps<{
  task: Task
  isSubtask?: boolean
  highlight?: boolean
}>()

const editing = ref(false)
const creatingSubtask = ref(false)
const openSeparateTask = inject<(task: Task, priority?: number) => void>('openSeparateTask')

const tasksStore = useTasksStore()

const subtasks = computed(() => {
  if (props.isSubtask) return []
  return tasksStore.tasks
    .filter(t => t.parentId === props.task.id)
    .sort((a, b) => new Date(a.modified).getTime() - new Date(b.modified).getTime())
})

const draggableSubtasks = ref<Task[]>([])
let subtaskDragActive = false // plain let, not ref — changing it must not trigger renders

watch(subtasks, (fresh) => {
  if (subtaskDragActive) return
  const storeMap = new Map(fresh.map(t => [t.id, t]))
  const preserved = draggableSubtasks.value
    .filter(t => storeMap.has(t.id))
    .map(t => storeMap.get(t.id)!)
  const preservedIds = new Set(preserved.map(t => t.id))
  const added = fresh.filter(t => !preservedIds.has(t.id))
  draggableSubtasks.value = [...preserved, ...added]
}, { immediate: true })

// v-model for VueDraggable (preserves drag order); v-for renders displaySubtasks.
const displaySubtasks = computed(() => {
  const storeMap = new Map(subtasks.value.map(t => [t.id, t]))
  const inOrder = draggableSubtasks.value
    .filter(t => storeMap.has(t.id))
    .map(t => storeMap.get(t.id)!)
  const inOrderIds = new Set(inOrder.map(t => t.id))
  const added = subtasks.value.filter(t => !inOrderIds.has(t.id))
  return [...added, ...inOrder]
})

// Allow dropping a top-level task (no existing parent, no subtasks of its own) into this list.
function canDropAsSubtask(_to: any, _from: any, dragEl: HTMLElement) {
  const taskId = dragEl.id.replace('task-', '')
  const task = tasksStore.tasks.find(t => t.id === taskId)
  return !!task && !task.parentId && !tasksStore.tasks.some(t => t.parentId === taskId)
}

async function onSubtaskAdded(evt: any) {
  const taskId = (evt.item as HTMLElement).id.replace('task-', '')
  const task = tasksStore.tasks.find(t => t.id === taskId)

  if (!confirm(`Make "${task?.title ?? 'this task'}" a subtask of "${props.task.title}"?`)) {
    draggableSubtasks.value = draggableSubtasks.value.filter(t => t.id !== taskId)
    return
  }

  await tasksStore.attachToParent(taskId, props.task.id)
}

function onSubtaskDragEnd(evt: any) {
  subtaskDragActive = false

  if (evt.from !== evt.to) {
    const taskId = (evt.item as HTMLElement).id.replace('task-', '')
    const task = tasksStore.tasks.find(t => t.id === taskId)
    if (!task) return

    const destPriorityRaw = parseInt((evt.to as HTMLElement).dataset.priority ?? '')
    const destPriority = Number.isNaN(destPriorityRaw) ? undefined : destPriorityRaw

    draggableSubtasks.value = subtasks.value.slice()
    openSeparateTask?.(task, destPriority)

    // SortableJS moved the DOM element into the dest container; remove it after Vue reconciles.
    // Direct children only — querySelector descends into nested subtask lists.
    const taskElId = 'task-' + taskId
    const destContainer = evt.to as HTMLElement
    nextTick(() => {
      const orphan = Array.from(destContainer.children).find(el => el.id === taskElId)
      orphan?.remove()
    })
    return
  }

  const { oldIndex, newIndex } = evt
  if (oldIndex == null || newIndex == null || oldIndex === newIndex) return
  const source = draggableSubtasks.value[newIndex]
  if (source) tasksStore.moveSubtaskRelative(source, draggableSubtasks.value, newIndex)
}

const now = ref(Date.now())
let clockInterval: ReturnType<typeof setInterval>

onMounted(() => {
  clockInterval = setInterval(() => { now.value = Date.now() }, 1000)
})
onUnmounted(() => {
  clearInterval(clockInterval)
  if (completingTimeout) clearTimeout(completingTimeout)
  if (notesHoverTimer) clearTimeout(notesHoverTimer)
})

// Has the original deadline moment passed (regardless of grace period)?
const deadlineExpired = computed(() => {
  if (!props.task.deadline) return false
  return now.value >= new Date(props.task.deadline).getTime()
})

// The moment when the grace period (if any) ends
const gracePeriodExpiry = computed(() => {
  if (!props.task.deadline || !props.task.timerDuration) return null
  return new Date(props.task.deadline).getTime() + props.task.timerDuration
})

// Deadline passed but the grace-period timer is still running
const isInGracePeriod = computed(() => {
  if (!deadlineExpired.value || !gracePeriodExpiry.value) return false
  return now.value < gracePeriodExpiry.value
})

// Truly overdue: past the deadline AND past any grace period
const isOverdue = computed(() => {
  if (!props.task.deadline) return false
  if (gracePeriodExpiry.value) return now.value >= gracePeriodExpiry.value
  return deadlineExpired.value
})

const isDueToday = computed(() => {
  if (!props.task.deadline || deadlineExpired.value) return false

  const today = new Date(now.value)
  const deadline = new Date(props.task.deadline)

  return (
      today.getFullYear() === deadline.getFullYear() &&
      today.getMonth() === deadline.getMonth() &&
      today.getDate() === deadline.getDate()
  )
})

function formattedDeadline() {
  if (!props.task.deadline) return null

  // Grace period: show countdown to when the timer expires
  if (isInGracePeriod.value && gracePeriodExpiry.value) {
    const remaining = Math.max(0, gracePeriodExpiry.value - now.value)
    const totalSec = Math.floor(remaining / 1000)
    const h = Math.floor(totalSec / 3600)
    const m = Math.floor((totalSec % 3600) / 60)
    const s = totalSec % 60
    if (h > 0) {
      return `Now · ${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
    }
    return `Now · ${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
  }

  const deadline = new Date(props.task.deadline)
  const current = new Date(now.value)

  // diffSec: exact elapsed time, used to determine expiry and sub-day labels.
  // calendarDiffDays: midnight-to-midnight difference, used for human-readable day labels.
  const diffMs = deadline.getTime() - current.getTime()
  const diffSec = Math.floor(diffMs / 1000)

  const startOfCurrent = new Date(current.getFullYear(), current.getMonth(), current.getDate())
  const startOfDeadline = new Date(deadline.getFullYear(), deadline.getMonth(), deadline.getDate())
  const calendarDiffDays = Math.round((startOfDeadline.getTime() - startOfCurrent.getTime()) / 86400000)

  if (Math.abs(calendarDiffDays) > 730) {
    return deadline.toLocaleDateString()
  }

  if (diffSec < 0) {
    if (calendarDiffDays === 0) {
      return 'Overdue · ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
    }
    if (calendarDiffDays === -1) {
      return 'Overdue · Yesterday'
    }
    if (calendarDiffDays >= -7) {
      return 'Overdue · ' + deadline.toLocaleDateString('en-US', { weekday: 'long' })
    }
    return 'Overdue · ' + deadline.toLocaleDateString()
  }

  if (calendarDiffDays === 0) {
    return 'Today · ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
  }
  if (calendarDiffDays === 1) {
    if (diffSec < 86400) {
      return 'Tomorrow · ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
    }
    return 'Tomorrow'
  }
  if (calendarDiffDays < 7) {
    return deadline.toLocaleDateString('en-US', { weekday: 'long' })
  }
  if (calendarDiffDays < 30) {
    return deadline.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    })
  }
  if (calendarDiffDays < 365) {
    return deadline.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    })
  }

  return deadline.toLocaleDateString()
}

function formattedCompletionDate() {
  if (!props.task.completed) return null
  return new Date(props.task.completed).toLocaleDateString()
}

function startEditing() {
  editing.value = true
}

// On touch devices a single tap conflicts with long-press-to-drag, so double-tap opens the editor.
let touchStartX = 0
let touchStartY = 0
let lastTapTime = 0

function handleContentClick() {
  // Synthetic click fires after touchend on touch devices — ignore here, handled by touchend.
  if (navigator.maxTouchPoints > 0) return
  startEditing()
}

function handleContentTouchStart(e: TouchEvent) {
  const t = e.touches[0]
  if (t) { touchStartX = t.clientX; touchStartY = t.clientY }
}

function handleContentTouchEnd(e: TouchEvent) {
  const t = e.changedTouches[0]
  if (!t) return

  if (Math.abs(t.clientX - touchStartX) > 10 || Math.abs(t.clientY - touchStartY) > 10) return

  const now = Date.now()
  const gap = now - lastTapTime
  lastTapTime = now

  if (gap < 300 && gap > 0) {
    e.preventDefault() // suppress the subsequent synthetic click
    startEditing()
  }
}

const notesEl = ref<HTMLElement | null>(null)
const notesExpanded = ref(false)
const notesFullHeight = ref(0)
let notesHoverTimer: ReturnType<typeof setTimeout> | null = null

function startNotesExpand() {
  if (!notesEl.value) return
  if (notesEl.value.scrollHeight <= notesEl.value.clientHeight + 2) return
  notesHoverTimer = setTimeout(() => {
    if (notesEl.value) {
      notesFullHeight.value = notesEl.value.scrollHeight
      notesExpanded.value = true
    }
  }, 500)
}

function cancelNotesExpand() {
  if (notesHoverTimer) { clearTimeout(notesHoverTimer); notesHoverTimer = null }
  notesExpanded.value = false
}

const completing = ref(false)
const completingDone = ref(false)
const blocked = ref(false)
let completingTimeout: ReturnType<typeof setTimeout> | null = null

// Hold-to-complete: checking the box starts a 2.5s countdown; a second click cancels it.
// Blocked (with shake) if the task has incomplete subtasks with deadlines.
async function onCompleteClick() {
  if (props.task.completed || completingDone.value) return

  if (completing.value) {
    completing.value = false
    if (completingTimeout) { clearTimeout(completingTimeout); completingTimeout = null }
    return
  }

  if (!props.isSubtask && subtasks.value.some(s => s.deadline !== null && !s.completed)) {
    blocked.value = true
    setTimeout(() => { blocked.value = false }, 600)
    return
  }

  completing.value = true

  await new Promise<void>(resolve => { completingTimeout = setTimeout(resolve, 2500) })
  completingTimeout = null

  if (!completing.value) return // cancelled by second click

  completing.value = false
  completingDone.value = true
  setTimeout(() => { completingDone.value = false }, 700)

  await tasksStore.toggleComplete(props.task)
}

async function toggleComplete() {
  if (props.task.completed) {
    if (!confirm('Mark task as incomplete?')) return
    await tasksStore.toggleComplete(props.task)
  }
}

// Taskmenu functions
async function togglePinned() {
  await tasksStore.togglePinned(props.task)
}

async function resetRating() {
  if (props.task.rating === 0) return
  await tasksStore.resetRating(props.task)
}

async function resetPriority() {
  if (props.task.priority === 0) return
  await tasksStore.resetPriority(props.task)
}

async function resetDeadline() {
  if (confirm('Remove deadline?')) {
    await tasksStore.resetDeadline(props.task)
  }
}

async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
}
</script>

<template>
  <SubtaskEdit v-if="editing && isSubtask" :task="task" @close="editing = false" />
  <TaskEdit v-else-if="editing" :task="task" @close="editing = false" />

  <component v-else
      :is="isSubtask ? 'div' : 'li'"
      :id="'task-' + task.id"
      class="task-item"
      :class="{ 'completing-done': completingDone, 'subtask-highlight': highlight }"
  >
    <div v-if="completing" class="task-complete-bar"></div>

    <div class="task-main-row" :class="{
      'title-only': !task.completed && !task.notes && !task.deadline && subtasks.length === 0 && !creatingSubtask,
      'cursor-grab': !task.completed && (isSubtask || !task.pinned),
    }">
      <div :class="['shrink-0 relative', { 'mt-0.5': task.completed || task.notes || task.deadline, 'shake': blocked }]">
        <input
            type="checkbox"
            :checked="!!task.completed || blocked"
            :disabled="!!task.completed"
            @change="onCompleteClick"
            class="swoosh-check"
            :class="{ 'opacity-100' : task.completed || completing || blocked }"
        />
      </div>

      <div
        @click="handleContentClick"
        @touchstart.passive="handleContentTouchStart"
        @touchend="handleContentTouchEnd"
        class="flex-1 min-w-0 cursor-text"
        :class="{ 'opacity-40' : task.completed }"
      >
        <div class="flex items-center justify-between gap-3">
          <span class="flex items-center gap-1.5 min-w-0">
            <span
                :class="[isSubtask ? 'text-[14px]' : 'text-[15.5px]', { 'line-through text-swoosh-text-muted': task.completed }]"
                class="font-bold text-base-content leading-[1.45] break-words"
            >
              {{ task.title }}
            </span>
            <TaskIcon v-if="task.icon != null" :value="task.icon" />
          </span>
          <TaskRating v-if="!task.completed && !isSubtask" :rating="task.rating" :priority="task.priority" :pinned="task.pinned" />
        </div>
        <p
          v-if="!task.completed && task.notes"
          ref="notesEl"
          class="task-notes text-[13.5px] text-swoosh-text-muted mt-1 leading-[1.5] break-words"
          :class="{ 'notes-expanded': notesExpanded }"
          :style="notesExpanded ? { maxHeight: notesFullHeight + 'px' } : {}"
          @mouseenter="startNotesExpand"
          @mouseleave="cancelNotesExpand"
        >{{ task.notes }}</p>
        <p v-else-if="task.completed" class="text-[11px] text-swoosh-text-muted mt-0.5">Completed {{ formattedCompletionDate() }}</p>
        <div v-if="!task.completed && task.deadline" class="badges">
          <span class="badge" :class="{ 'overdue': isOverdue, 'grace': isInGracePeriod, 'due-today': isDueToday }" v-animate-sync:overdue="isOverdue ? 'badge' : isDueToday ? { group: 'today', type: 'border' } : null">
            <Timer v-if="isInGracePeriod" :size="11" />
            <Calendar v-else-if="!deadlineExpired" :size="11" />
            <Clock v-else :size="11" />
            {{ formattedDeadline() }}
          </span>
        </div>
      </div>

      <!-- task-actions is hidden until row hover via CSS -->
      <div class="task-actions shrink-0">
        <TaskMenu
            :is-completed="!!task.completed"
            :pinned="task.pinned"
            :has-deadline="!!task.deadline"
            :has-rating="task.rating > 0"
            :priority="task.priority"
            :is-subtask="isSubtask"
            @delete="remove"
            @edit="startEditing"
            @reset-deadline="resetDeadline"
            @pin="togglePinned"
            @un-complete="toggleComplete"
            @reset-rating="resetRating"
            @reset-priority="resetPriority"
            @add-subtask="creatingSubtask = true"
            @separate-task="openSeparateTask?.(task)"
        />
      </div>
    </div>

    <!-- @pointerdown.stop prevents the outer VueDraggable from seeing events that originate
         here and mistakenly dragging the whole parent task. -->
    <div v-if="!isSubtask && (subtasks.length > 0 || creatingSubtask)" class="subtasks-container" @pointerdown.stop>
      <VueDraggable
        v-model="draggableSubtasks"
        :animation="150"
        :delay="500"
        :delay-on-touch-only="true"
        :group="{ name: 'tasks', put: canDropAsSubtask }"
        ghost-class="drag-ghost"
        @choose="subtaskDragActive = true"
        @end="onSubtaskDragEnd"
        @add="onSubtaskAdded"
      >
        <TaskItem
            v-for="sub in displaySubtasks"
            :key="sub.id"
            :task="sub"
            :is-subtask="true"
            :highlight="blocked && !!sub.deadline && !sub.completed"
        />
      </VueDraggable>
      <SubtaskEdit
          v-if="creatingSubtask"
          :parent-task-id="task.id"
          @close="creatingSubtask = false"
          @created="creatingSubtask = false"
      />
    </div>
  </component>
</template>

<style scoped>
.task-notes {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  overflow: hidden;
  max-height: 3em; /* 2 lines × 1.5 line-height */
  transition: max-height 0.3s ease;
}

.task-notes.notes-expanded {
  -webkit-line-clamp: unset;
  /* max-height driven by inline :style binding */
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  20%       { transform: translateX(-4px); }
  40%       { transform: translateX(4px); }
  60%       { transform: translateX(-3px); }
  80%       { transform: translateX(3px); }
}

.shake {
  animation: shake 0.5s ease;
}

@keyframes subtask-highlight {
  0%, 100% { background-color: transparent; }
  30%       { background-color: oklch(0.85 0.12 80 / 0.35); }
}

.subtask-highlight {
  border-radius: 6px;
  animation: subtask-highlight 0.6s ease;
}
</style>
