<script setup lang="ts">
import type { Task } from '../types/task'
import { PRIORITIES } from '../types/priority'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref, computed } from 'vue'
import { GripVertical, Pin, PinOff, Plus } from 'lucide-vue-next'

const props = defineProps<{
  task?: Task
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'created'): void
}>()

const isEdit = computed(() => !!props.task)
const tasksStore = useTasksStore()
const loading = ref(false)
const showValidation = ref(false)

// Store original values to allow canceling edits
const originalTitle = ref(props.task?.title ?? '')
const originalNotes = ref(props.task?.notes ?? '')
const originalPinned = ref(props.task?.pinned ?? false)
const originalDeadline = ref(props.task?.deadline ?? '')
const originalPriority = ref(props.task?.priority ?? 0)

// Splits ISO deadline string into date and time components for editing
function splitDeadline(deadline: string | null) {
  if (!deadline) return { date: '', time: '' }
  const [date, fullTime] = deadline.split('T')
  const time = fullTime ? fullTime.substring(0, 5) : '' // HH:mm
  return { date, time }
}

const initialDeadline = splitDeadline(props.task?.deadline ?? null)
const editedDate = ref(initialDeadline.date)
const editedTime = ref(initialDeadline.time)

const editedTitle = ref(props.task?.title ?? '')
const editedNotes = ref(props.task?.notes ?? '')
const editedPinned = ref(props.task?.pinned ?? false)
const editedRating = ref(props.task?.rating ?? 0)

const priorityIndex = ref(
    PRIORITIES.findIndex(p => p.value === (props.task?.priority ?? 0))
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
  if (!isEdit.value) {
    emit('close')
    return
  }
  editedTitle.value = originalTitle.value
  editedNotes.value = originalNotes.value
  editedPinned.value = originalPinned.value
  editedRating.value = props.task?.rating ?? 0
  const { date, time } = splitDeadline(originalDeadline.value)
  editedDate.value = date
  editedTime.value = time
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === originalPriority.value)
  emit('close')
}

// Resets the component state to default values for creation mode
function resetForm() {
  if (isEdit.value) return

  editedTitle.value = ''
  editedNotes.value = ''
  editedDate.value = ''
  editedTime.value = ''
  editedPinned.value = false
  editedRating.value = 0
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === 0)
  showValidation.value = false
}

const isFormBlank = computed(() => {
  if (isEdit.value) return false

  return !editedTitle.value.trim() &&
      !editedNotes.value.trim() &&
      !editedDate.value &&
      !editedTime.value &&
      !editedPinned.value &&
      editedRating.value === 0 &&
      priorityIndex.value === PRIORITIES.findIndex(p => p.value === 0)
})

defineExpose({
  resetForm,
  isFormBlank
})

// Saves changes made in the inline editor to the store
async function finishEditing() {
  if (loading.value) return

  // If the task is already completed, nothing happens
  if (props.task?.completed) {
    emit('close')
    return
  }

  const currentDeadline = combinedDeadline.value
  if (
      isEdit.value &&
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value &&
      editedPinned.value === originalPinned.value &&
      editedRating.value === (props.task?.rating ?? 0) &&
      currentDeadline === (originalDeadline.value || null) &&
      editedPriority.value === originalPriority.value
  ) {
    emit('close')
    return
  }

  if (!editedTitle.value.trim()) {
    showValidation.value = true
    if (!isEdit.value) return // Stay in modal if creation fails
    cancelEditing()
    return
  }

  loading.value = true
  try {
    if (isEdit.value && props.task) {
      await tasksStore.editTask(props.task.id, {
        title: editedTitle.value.trim(),
        notes: editedNotes.value || null,
        pinned: editedPinned.value,
        deadline: currentDeadline,
        priority: editedPriority.value,
        rating: editedRating.value
      })
    } else {
      await tasksStore.createTask({
        title: editedTitle.value.trim(),
        notes: editedNotes.value || null,
        deadline: currentDeadline,
        pinned: editedPinned.value,
        priority: editedPriority.value,
        rating: editedRating.value
      })
      emit('created')
      resetForm()
    }

    emit('close')
  } finally {
    loading.value = false
  }
}

// Deletes the task after confirmation
async function remove() {
  if (isEdit.value && props.task) {
    if (confirm('Delete this task?')) {
      await tasksStore.deleteTask(props.task.id)
    }
  }
}

// Toggles completion status for already completed tasks (with confirmation)
async function toggleComplete() {
  if(isEdit.value && props.task?.completed) {
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
  if (isEdit.value && props.task) {
    if (confirm('Remove deadline?')) {
      await tasksStore.resetDeadline(props.task)
    }
  } else {
    editedDate.value = ''
    editedTime.value = ''
  }
}

// Moves the task to the top of the list by resetting its creation date
async function moveToTop() {
  if (isEdit.value && props.task) {
    await tasksStore.resetCreationDate(props.task)
  }
}
// Handles click outside the component
function handleClickOutside() {
  if (isEdit.value) {
    finishEditing()
  }
}

</script>

<template>
  <li :class="isEdit ? 'list-row' : 'flex flex-col sm:flex-row gap-4 p-4'" @click.stop v-click-outside="handleClickOutside">
    <div v-if="isEdit" class="flex flex-col">
      <!-- Completion checkbox (disabled in edit mode) -->
      <input
          type="checkbox"
          :checked="!!task?.completed"
          class="checkbox"
          :class="task?.completed ? 'checkbox-lg checkbox-primary' : 'checkbox-lg' "
          disabled
      />
      <!-- Drag handle for reordering -->
      <div v-if="!task?.completed" class="flex-1 flex items-center justify-center min-h-8 cursor-grab group" @click="finishEditing">
        <GripVertical class="opacity-10 group-hover:opacity-50 transition-opacity duration-200" />
      </div>
    </div>

    <!-- Input fields for editing task details -->
    <div class="flex flex-col gap-2 w-full">
      <div class="relative">
        <div v-if="(showValidation || isEdit) && !editedTitle.trim()" class="absolute left-0 -top-4 text-[10px] text-error font-bold uppercase px-1">
          Title is required
        </div>
        <input
            ref="titleInput"
            type="text"
            class="input w-full text-base font-semibold"
            :class="{ validator: showValidation || isEdit }"
            maxlength="24"
            placeholder="Title"
            v-model="editedTitle"
            @keydown="onKeydown"
            @input="showValidation = true"
            :disabled="task?.completed"
            required
            autofocus
        />
      </div>

      <textarea
          class="textarea textarea-bordered w-full"
          placeholder="Notes"
          maxlength="250"
          v-model="editedNotes"
          @keydown="onKeydown"
          :disabled="task?.completed"
      />

      <div class="flex flex-wrap gap-2">
        <input
            type="date"
            class="input input-bordered flex-1 min-w-[140px]"
            v-model="editedDate"
            @keydown="onKeydown"
            :disabled="task?.completed"
        />
        <input
            type="time"
            class="input input-bordered flex-1 min-w-[100px]"
            v-model="editedTime"
            @keydown="onKeydown"
            :disabled="task?.completed"
        />
      </div>
    </div>

    <!-- Edit mode action buttons (priority and pin) -->
    <div class="flex flex-col items-end gap-2 shrink-0">
      <div class="flex justify-end">
        <div class="tooltip h-0" :data-tip=PRIORITIES[priorityIndex].label>
          <button
              id="priority"
              type="button"
              @click="cyclePriority"
              class="btn btn-ghost btn-square max-sm:btn-sm opacity-60 hover:opacity-100">
            <component
                :is="PRIORITIES[priorityIndex].icon"
                class="transition-transform duration-150 active:rotate-12"
            />
          </button>
        </div>
        <label class="swap btn btn-ghost btn-square max-sm:btn-sm opacity-60 hover:opacity-100 ml-2">
          <input type="checkbox" v-model="editedPinned" />
          <Pin class="swap-off" />
          <PinOff class="swap-on" />
        </label>
      </div>
      <div class="rating rating-xs">
        <input type="radio" :name="isEdit ? 'rating-edit-' + task?.id : 'rating-create'" class="rating-hidden" :checked="editedRating === 0" @click="editedRating = 0" />
        <input v-for="n in 5" :key="n" type="radio" :name="isEdit ? 'rating-edit-' + task?.id : 'rating-create'" class="mask mask-diamond" :checked="editedRating === n" @click="editedRating = n" />
      </div>
      <button v-if="!isEdit" class="btn btn-primary max-sm:btn-sm mt-auto" @click="finishEditing" :disabled="loading">
        <span v-if="loading" class="loading loading-spinner loading-sm"></span><Plus v-else /> Add
      </button>
    </div>
    <!-- Secondary task menu -->
    <div v-if="isEdit" class="flex justify-end">
      <TaskMenu
          :is-completed="!!task?.completed"
          :has-deadline="!!task?.deadline"
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