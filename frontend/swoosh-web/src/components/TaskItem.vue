<!-- 
  TaskItem.vue
  Represents a single task in the list.
  Handles inline editing, completion toggling, pinning, priority management, and drag-and-drop.
-->
<script setup lang="ts">
import type { Task } from '../types/task'
import { PRIORITIES } from '../types/priority'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref, computed, watch } from 'vue'
import TaskEdit from './TaskEdit.vue'
import TaskRating from './TaskRating.vue'
import { 
  Trash2, 
  GripVertical, 
  EllipsisVertical, 
  ListStart, 
  Pin, 
  PinOff, 
  CalendarClock,
  ClockAlert
} from 'lucide-vue-next'

const props = defineProps<{
  task: Task
}>()

const editing = ref(false)

const now = ref(Date.now())

// Update current time every second for deadline relative formatting
setInterval(() => {
  now.value = Date.now()
}, 1000)

const EXPIRED = {
  true: {icon: ClockAlert},
  false: {icon: CalendarClock}
}

const priorityIndex = computed(() =>
    PRIORITIES.findIndex(p => p.value === props.task.priority)
)

// Computes if the task deadline has passed
const deadlineExpired = computed(() =>
    now.value >= new Date(props.task.deadline).getTime()
)

// Computes if the task is due on the current day
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

const tasksStore = useTasksStore()

const emit = defineEmits<{
  (e: 'drag-start', task: Task): void
  (e: 'drop', task: Task): void
}>()

// Formats the deadline date into a human-readable relative string
function formattedDeadline() {
  if (!props.task.deadline) return null

  const deadline = new Date(props.task.deadline)
  const now = new Date()

  const diffMs = deadline.getTime() - now.getTime()
  const diffSec = Math.floor(diffMs / 1000)
  const diffDays = Math.floor(diffSec / 86400)
  const isToday = now.getDate() === deadline.getDate()
  
  if (Math.abs(diffDays) > 730) {
    return deadline.toLocaleDateString()
  }
  
  if (diffSec < 0) {
    if (diffDays >= -1) {
      return 'Overdue - ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
    }
    if (diffDays >= -2) {
      return 'Overdue - Yesterday'
    }
    if (diffDays >= -7) {
      return 'Overdue - ' + deadline.toLocaleDateString('en-US', { weekday: 'long' })
    }
    return 'Overdue - ' + deadline.toLocaleDateString()
  }
  if (diffDays === 0) {
    if(isToday) return 'Today - ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
    return 'Tomorrow - ' + deadline.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
  }
  if (diffDays === 1) {
    return 'Tomorrow'
  }
  if (diffDays < 7) {
    return deadline.toLocaleDateString('en-US', { weekday: 'long' })
  }
  if (diffDays < 30) {
    return deadline.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    })
  }
  if (diffDays < 365) {
    return deadline.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    })
  }
  
  return deadline.toLocaleDateString()
}

// Formats the completion date for display
function formattedCompletionDate() {
  if (!props.task.completed) return null
  return new Date(props.task.completed).toLocaleDateString()
}

// Switches the component to editing mode and initializes edited values
function startEditing() {
  editing.value = true
}

const completing = ref(false)

// Initiates the task completion animation and updates the store
async function onCompleteClick() {
  if( props.task.completed || completing.value ) return
  
  completing.value = true
  
  setTimeout(async () => {
    await tasksStore.toggleComplete(props.task)
    completing.value = false
  }, 500)
}

// Toggles completion status for already completed tasks (with confirmation)
async function toggleComplete() {
  if(props.task.completed) {
    if(!confirm('Mark task as incomplete?')) {
      return
    }
    await tasksStore.toggleComplete(props.task)
  }
}

// Toggles the pinned status of the task
async function togglePinned() {
  await tasksStore.togglePinned(props.task)
}

// Resets the task priority to default (0)
async function resetPriority() {
  if(priorityIndex.value === 0 ) return
  
  if(confirm('Reset priority?')) {
    await tasksStore.updatePriority(props.task, 0)
  }
}

// Resets the task's rating to 0
async function resetRating() {
  if (props.task.rating === 0) return
  
  await tasksStore.resetRating(props.task)
}

// Removes the task's deadline
async function resetDeadline() {
  if (confirm('Remove deadline?')) {
    await tasksStore.resetDeadline(props.task)
  }
}

// Moves the task to the top of the list by resetting its creation date
async function moveToTop() {
  await tasksStore.resetCreationDate(props.task)
}

// Deletes the task after confirmation
async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
}
</script>

<!-- Component Template: Renders either the task display or the inline editor -->
<template>
  <!-- Inline Editor Mode -->
  <TaskEdit v-if="editing" :task="task" @close="editing = false" />

  <!-- Display Mode -->
  <li v-else 
      class="list-row"
      :class="{ 'items-center' : !task.notes && !task.deadline && !task.completed }"
      :draggable="!task.completed"
      @dragstart="emit('drag-start', task)"
      @dragover.prevent
      @drop="emit('drop', task)"
  >
      <div class="flex">
        <!-- Interactive completion checkbox with animation support -->
        <div class="inline-grid *:[grid-area:1/1]">
          <input type="checkbox" :checked="!!task.completed" class="checkbox checkbox-primary checkbox-lg" :class="completing ? 'animate-ping opacity-100' : 'opacity-0'"/>
          <input
            type="checkbox"
            :checked="!!task.completed"
            :disabled="task.completed"
            @change="onCompleteClick"
            class="checkbox checkbox-lg hover:checkbox-primary"
            :class="{ 'checkbox-primary' : task.completed || completing }"
          />
        </div>
      </div>
    
    <!-- Task textual content: Title, notes, and deadline badge -->
    <div @click="startEditing" class="cursor-text flex-grow">
      <div class="flex flex-row">
        <h1 class="text-base" :class="task.completed ? 'line-through opacity-70' : 'font-semibold'">
          {{ task.title }}
        </h1>
        <TaskRating v-if="!task.completed" :rating="task.rating" :priority="task.priority" />
      </div>
      <p v-if="!task.completed" class="text-sm opacity-70 line-clamp-3"> {{ task.notes }}</p>
      <p v-else class="text-xs opacity-50 line-clamp-1">Completed on {{ formattedCompletionDate() }}</p>
      <!-- Deadline indicator badge -->
      <div v-if="!task.completed && task.deadline" class="badge mt-1 cursor-pointer" :class="deadlineExpired ? 'badge-error' : 'badge-soft', { 'badge-info' : isDueToday }">
        <component :is="EXPIRED[deadlineExpired].icon" :size="16" /> {{ formattedDeadline() }}
      </div>
    </div>

    <!-- Quick action buttons: Priority and Pin -->
    <div v-if="!task.completed" class="flex justify-end group">
      <button 
          id="priority" 
          @click="startEditing"
          class="btn btn-square max-sm:btn-sm"
          :class="[priorityIndex === 0 ? 'btn-ghost' : 'btn-soft', PRIORITIES[priorityIndex].class]">
        <component
            :is="PRIORITIES[priorityIndex].icon"
        />
      </button>
      <button 
          id="pin" 
          @click="togglePinned"
          class="btn btn-ghost btn-circle ml-2 max-sm:btn-sm"
          :class="task.pinned ? '' : 'opacity-0 group-hover:opacity-50'">
        <Pin />
      </button>
    </div>

    <!-- Task overflow menu -->
    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          :has-deadline="!!task.deadline"
          :has-priority="priorityIndex !== 0"
          :has-rating="task.rating > 0"
          @delete="remove"
          @edit="startEditing"
          @reset-deadline="resetDeadline"
          @move-to-top="moveToTop"
          @un-complete="toggleComplete"
          @priority="resetPriority"
          @reset-rating="resetRating"
      />
    </div>
  </li>
</template>
