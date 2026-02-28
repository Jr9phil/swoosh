<script setup lang="ts">
import type { Task } from '../types/task'
import { PRIORITIES } from '../types/priority'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref, computed } from 'vue'
import {
  GripVertical,
  Pin,
  PinOff,
} from 'lucide-vue-next'

const props = defineProps<{
  task: Task
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const tasksStore = useTasksStore()

// Store original values to allow canceling edits
const originalTitle = ref(props.task.title)
const originalNotes = ref(props.task.notes ?? '')
const originalPinned = ref(props.task.pinned)
const originalDeadline = ref(props.task.deadline ?? '')
const originalPriority = ref(props.task.priority)

// Splits ISO deadline string into date and time components for editing
function splitDeadline(deadline: string | null) {
  if (!deadline) return { date: '', time: '' }
  const [date, fullTime] = deadline.split('T')
  const time = fullTime ? fullTime.substring(0, 5) : '' // HH:mm
  return { date, time }
}

const initialDeadline = splitDeadline(props.task.deadline ?? null)
const editedDate = ref(initialDeadline.date)
const editedTime = ref(initialDeadline.time)

const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')
const editedPinned = ref(props.task.pinned)
const editedRating = ref(props.task.rating)

const priorityIndex = ref(
    PRIORITIES.findIndex(p => p.value === props.task.priority)
)

const editedPriority = computed(() =>
    PRIORITIES[priorityIndex.value].value
)

// Combines date and time inputs into a single deadline string
// Defaults: date set -> 11:59 PM, time set -> today
const combinedDeadline = computed(() => {
  if (!editedDate.value && !editedTime.value) return null

  let date = editedDate.value
  let time = editedTime.value

  if (date && !time) {
    time = '23:59'
  } else if (!date && time) {
    date = new Date().toISOString().split('T')[0]
  }

  return `${date}T${time}:00`
})

// Cycles through available priority levels
function cyclePriority() {
  priorityIndex.value = (priorityIndex.value + 1) % PRIORITIES.length
}

// Handles keyboard shortcuts for the inline editor
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    if (e.target instanceof HTMLTextAreaElement) {
        return
    }
    e.preventDefault()
    finishEditing()
  }

  if (e.key === 'Escape') {
    cancelEditing()
  }
}

function cancelEditing() {
  editedTitle.value = originalTitle.value
  editedNotes.value = originalNotes.value
  editedPinned.value = originalPinned.value
  editedRating.value = props.task.rating
  const { date, time } = splitDeadline(originalDeadline.value)
  editedDate.value = date
  editedTime.value = time
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === originalPriority.value)
  emit('close')
}

// Saves changes made in the inline editor to the store
async function finishEditing() {
  const currentDeadline = combinedDeadline.value
  if (
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value &&
      editedPinned.value === originalPinned.value &&
      editedRating.value === props.task.rating &&
      currentDeadline === (originalDeadline.value || null) &&
      editedPriority.value === originalPriority.value
  ) {
    emit('close')
    return
  }

  if (!editedTitle.value.trim()) {
    cancelEditing()
    return
  }

  await tasksStore.editTask(props.task.id, {
    title: editedTitle.value.trim(),
    notes: editedNotes.value || null,
    pinned: editedPinned.value,
    deadline: currentDeadline,
    priority: editedPriority.value,
    rating: editedRating.value
  })

  emit('close')
}

// Deletes the task after confirmation
async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
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

// Resets the task's rating to 0
function resetRating() {
  editedRating.value = 0
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

</script>

<template>
  <li class="list-row" @click.stop v-click-outside="finishEditing">
    <div class="flex flex-col">
      <!-- Completion checkbox (disabled in edit mode) -->
      <input
          type="checkbox"
          :checked="!!task.completed"
          class="checkbox"
          :class="task.completed ? 'checkbox-lg checkbox-primary' : 'checkbox-lg' "
          disabled
      />
      <!-- Drag handle for reordering -->
      <div v-if="!task.completed" class="flex-1 flex items-center justify-center min-h-8 cursor-grab group" @click="finishEditing">
        <GripVertical class="opacity-10 group-hover:opacity-50 transition-opacity duration-200" />
      </div>
    </div>

    <!-- Input fields for editing task details -->
    <div class="flex flex-col gap-2 w-full">
      <input
          ref="titleInput"
          type="text"
          class="input input-bordered text-base font-semibold"
          maxlength="24"
          v-model="editedTitle"
          @keydown="onKeydown"
          autofocus
      />

      <textarea
          class="textarea textarea-bordered"
          placeholder="Notes"
          maxlength="250"
          v-model="editedNotes"
          @keydown="onKeydown"
      />

      <div class="flex gap-2">
        <input
            type="date"
            class="input input-bordered flex-1"
            v-model="editedDate"
            @keydown="onKeydown"
        />
        <input
            type="time"
            class="input input-bordered flex-1"
            v-model="editedTime"
            @keydown="onKeydown"
        />
      </div>
    </div>

    <!-- Edit mode action buttons (priority and pin) -->
    <div v-if="!task.completed" class="flex flex-col items-end gap-2">
      <div class="flex justify-end">
        <div class="tooltip h-0" :data-tip=PRIORITIES[priorityIndex].label>
          <button
              id="priority"
              @click="cyclePriority"
              class="btn btn-ghost btn-square opacity-60 hover:opacity-100">
            <component
                :is="PRIORITIES[priorityIndex].icon"
                class="transition-transform duration-150 active:rotate-12"
            />
          </button>
        </div>
        <label class="swap btn btn-ghost btn-square opacity-60 hover:opacity-100 ml-2">
          <input type="checkbox" v-model="editedPinned" />
          <Pin class="swap-off" />
          <PinOff class="swap-on" />
        </label>
      </div>
      <div class="rating rating-xs">
        <input type="radio" name="rating-2" class="rating-hidden" :checked="editedRating === 0" @click="editedRating = 0" />
        <input v-for="n in 5" :key="n" type="radio" name="rating-2" class="mask mask-diamond" :checked="editedRating === n" @click="editedRating = n" />
      </div>
    </div>
    <!-- Secondary task menu -->
    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          :has-deadline="!!task.deadline"
          :has-priority="priorityIndex !== 0"
          :has-rating="editedRating > 0"
          @delete="remove"
          @reset-deadline="resetDeadline"
          @move-to-top="moveToTop"
          @un-complete="toggleComplete"
          @reset-rating="resetRating"
      />
    </div>
  </li>
</template>