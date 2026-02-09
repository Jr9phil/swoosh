<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref, computed, watch } from 'vue'
import { 
  Trash2, 
  GripVertical, 
  EllipsisVertical, 
  ListStart, 
  Pin, 
  PinOff, 
  CalendarClock,
  ClockAlert,  
  ChessPawn,
  ChessKnight,
  ChessQueen,
  ChessKing  
} from 'lucide-vue-next'

const props = defineProps<{
  task: Task
}>()

const originalTitle = ref(props.task.title)
const originalNotes = ref(props.task.notes ?? '')
const originalPinned = ref(props.task.pinned)
const originalDeadline = ref(props.task.deadline ?? '')
const originalPriority = ref(props.task.priority)

const editing = ref(false)
const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')
const editedPinned = ref(props.task.pinned)
const editedDeadline = ref(props.task.deadline ?? '')

const now = ref(Date.now())

setInterval(() => {
  now.value = Date.now()
}, 1000)

const PRIORITIES = [
  { value: 0, icon: ChessPawn, label: 'Default', style: 'btn-ghost opacity-0 group-hover:opacity-50' },
  { value: 1, icon: ChessKnight, label: 'Medium', style: 'btn-soft text-slate-400 bg-slate-400/10' },
  { value: 2, icon: ChessQueen, label: 'High', style: 'btn-soft text-blue-400 bg-blue-400/10' },
  { value: 3, icon: ChessKing, label: 'Top', style: 'btn-soft text-amber-400 bg-amber-400/10' }
]

const EXPIRED = {
  true: {icon: ClockAlert},
  false: {icon: CalendarClock}
}

const priorityIndex = ref(
    PRIORITIES.findIndex(p => p.value === props.task.priority)
)

const editedPriority = computed(() =>
    PRIORITIES[priorityIndex.value].value
)

watch(
    () => props.task.priority,
    (newVal) => {
      if (!editing.value) {
        priorityIndex.value = PRIORITIES.findIndex(p => p.value === newVal)
      }
    }
)

const deadlineExpired = computed(() =>
    now.value >= new Date(props.task.deadline).getTime()
)

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

function formattedCompletionDate() {
  if (!props.task.completed) return null
  return new Date(props.task.completed).toLocaleDateString()
}
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    e.preventDefault()
    finishEditing()
  }

  if (e.key === 'Escape') {
    editedTitle.value = originalTitle.value
    editedNotes.value = originalNotes.value
    editing.value = false
  }
}
function startEditing() {
  editing.value = true
  editedTitle.value = props.task.title
  editedNotes.value = props.task.notes ?? ''
  editedPinned.value = props.task.pinned
  editedDeadline.value = props.task.deadline ?? ''
  
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === props.task.priority)
  
  originalTitle.value = props.task.title
  originalNotes.value = props.task.notes ?? ''
  originalPinned.value = props.task.pinned
  originalDeadline.value = props.task.deadline ?? ''
  originalPriority.value = props.task.priority
}
function cyclePriority() {
  if(!editing.value) return
  priorityIndex.value = (priorityIndex.value + 1) % PRIORITIES.length
}

const completing = ref(false)

async function onCompleteClick() {
  if( props.task.completed || completing.value ) return
  
  completing.value = true
  
  setTimeout(async () => {
    await tasksStore.toggleComplete(props.task)
    completing.value = false
  }, 500)
}
async function toggleComplete() {
  if(props.task.completed) {
    if(!confirm('Mark task as incomplete?')) {
      return
    }
    await tasksStore.toggleComplete(props.task)
  }
}
async function togglePinned() {
  await tasksStore.togglePinned(props.task)
}
async function resetPriority() {
  if(priorityIndex.value === 0 ) return
  
  if(confirm('Reset priority?')) {
    await tasksStore.updatePriority(props.task, 0)
  }
}
async function resetDeadline() {
  if (confirm('Remove deadline?')) {
    await tasksStore.resetDeadline(props.task)
  }
}
async function moveToTop() {
  await tasksStore.resetCreationDate(props.task)
}
async function finishEditing() {
  if (!editing.value) return

  editing.value = false
  
  if (
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value &&
      editedPinned.value === originalPinned.value &&
      editedDeadline.value === originalDeadline.value &&
      editedPriority.value === originalPriority.value
  ) {
    return
  }
  
  if (!editedTitle.value.trim()) {
    editedTitle.value = originalTitle.value
    editedNotes.value = originalNotes.value
    editedPinned.value = originalPinned.value
    editedDeadline.value = originalDeadline.value
    editedPriority.value = originalPriority.value
    return
  }

  await tasksStore.editTask(props.task.id, {
    title: editedTitle.value.trim(),
    notes: editedNotes.value || null,
    pinned: editedPinned.value,
    deadline: editedDeadline.value || null,
    priority: editedPriority.value
  })
}
async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
}
</script>

<template>
  <li v-if="editing" class="list-row" v-click-outside="finishEditing">
    <div class="flex flex-col">
      <input
        type="checkbox"
        :checked="!!task.completed"
        :class="task.completed ? 'checkbox checkbox-primary' : 'checkbox' "
        disabled 
      />
      <div v-if="!task.completed" class="flex-1 flex items-center justify-center min-h-8 cursor-grab group" @click="finishEditing">
        <GripVertical class="opacity-10 group-hover:opacity-50 transition-opacity duration-200" />
      </div>
    </div>

    <div class="flex flex-col gap-2 w-full">
      <input
          ref="titleInput"
          type="text"
          class="input input-bordered text-base font-semibold"
          maxlength="200"
          v-model="editedTitle"
          @keydown="onKeydown"
          autofocus
      />

      <textarea
          class="textarea textarea-bordered"
          placeholder="Notes"
          maxlength="1000"
          v-model="editedNotes"
          @keydown="onKeydown"
      />
      
      <input 
          type="datetime-local" 
          class="input input-bordered" 
          v-model="editedDeadline" 
          @keydown="onKeydown"
      />
    </div>
    <div v-if="!task.completed" class="flex justify-end">
      <div class="tooltip h-0" :data-tip=PRIORITIES[priorityIndex].label>
        <button
            id="priority"
            @click="cyclePriority"
            class="btn btn-ghost btn-circle opacity-60 hover:opacity-100">
          <component
            :is="PRIORITIES[priorityIndex].icon"
            class="transition-transform duration-150 active:rotate-12"
          />
        </button>
      </div>
      <label class="swap btn btn-ghost btn-circle opacity-60 hover:opacity-100 ml-1">
        <input type="checkbox" v-model="editedPinned" />
        <Pin class="swap-off" />
        <PinOff class="swap-on" />
      </label>
    </div>
    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          :has-deadline="!!task.deadline"
          @delete="remove"
          @reset-deadline="resetDeadline"
          @move-to-top="moveToTop"
          @un-complete="toggleComplete"
      />
    </div>
  </li>
  <li v-else 
      class="list-row"
      :draggable="!task.completed"
      @dragstart="emit('drag-start', task)"
      @dragover.prevent
      @drop="emit('drop', task)"
  >
      <div class="flex flex-col">
        <div class="inline-grid *:[grid-area:1/1]">
          <input type="checkbox" :checked="!!task.completed" class="checkbox checkbox-primary" :class="completing ? 'animate-ping opacity-100' : 'opacity-0'"/>
          <input
            type="checkbox"
            :checked="!!task.completed"
            :disabled="task.completed"
            @change="onCompleteClick"
            class="checkbox hover:checkbox-primary"
            :class="{ 'checkbox-primary' : task.completed || completing }"
          />
        </div>

        <div v-if="!task.completed" class="flex-1 flex items-center justify-center mt-2 cursor-grab group">
          <GripVertical class="opacity-0 group-hover:opacity-50 transition-opacity duration-200" />
        </div>
      </div>
    
    <div @click="startEditing" class="cursor-text">
      <h1 class="text-base" :class="task.completed ? 'line-through opacity-70' : 'font-semibold'">
        {{ task.title }}
      </h1>
      <p v-if="!task.completed" class="text-sm opacity-70 line-clamp-3 mb-1"> {{ task.notes }}</p>
      <p v-else class="text-xs opacity-50 line-clamp-1">Completed on {{ formattedCompletionDate() }}</p>
      <div v-if="!task.completed && task.deadline" class="badge badge-soft mt-1 cursor-pointer" :class="{ 'badge-error' : deadlineExpired }, { 'badge-info' : isDueToday }">
        <component :is="EXPIRED[deadlineExpired].icon" :size="16" /> {{ formattedDeadline() }}
      </div>
    </div>

    <div v-if="!task.completed" class="flex justify-end group">
      <button 
          id="priority" 
          @click="startEditing"
          class="btn btn-circle"
          :class="PRIORITIES[priorityIndex].style">
        <component
            :is="PRIORITIES[priorityIndex].icon"
        />
      </button>
      <button 
          id="pin" 
          @click="togglePinned"
          class="btn btn-ghost btn-circle ml-1"
          :class="task.pinned ? '' : 'opacity-0 group-hover:opacity-50'">
        <Pin />
      </button>
    </div>

    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          :has-deadline="!!task.deadline"
          :has-priority="priorityIndex !== 0"
          @delete="remove"
          @edit="startEditing"
          @reset-deadline="resetDeadline"
          @move-to-top="moveToTop"
          @un-complete="toggleComplete"
          @priority="resetPriority"
      />
    </div>
  </li>
</template>
