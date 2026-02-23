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

const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')
const editedPinned = ref(props.task.pinned)
const editedDeadline = ref(props.task.deadline ?? '')

const priorityIndex = ref(
    PRIORITIES.findIndex(p => p.value === props.task.priority)
)

const editedPriority = computed(() =>
    PRIORITIES[priorityIndex.value].value
)

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
  editedDeadline.value = originalDeadline.value
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === originalPriority.value)
  emit('close')
}

// Saves changes made in the inline editor to the store
async function finishEditing() {
  if (
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value &&
      editedPinned.value === originalPinned.value &&
      editedDeadline.value === originalDeadline.value &&
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
    deadline: editedDeadline.value || null,
    priority: editedPriority.value
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
          :class="task.completed ? 'checkbox checkbox-primary' : 'checkbox' "
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

    <!-- Edit mode action buttons (priority and pin) -->
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
    <!-- Secondary task menu -->
    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          :has-deadline="!!task.deadline"
          :has-priority="priorityIndex !== 0"
          @delete="remove"
          @reset-deadline="resetDeadline"
          @move-to-top="moveToTop"
          @un-complete="toggleComplete"
      />
    </div>
  </li>
</template>