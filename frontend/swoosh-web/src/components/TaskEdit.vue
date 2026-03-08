<script setup lang="ts">
import type { Task } from '../types/task'
import { PRIORITIES } from '../types/priority'
import { useTasksStore } from '../stores/tasks'
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Pin } from 'lucide-vue-next'
import TaskRating from './TaskRating.vue'

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
const originalTitle    = ref(props.task?.title    ?? '')
const originalNotes    = ref(props.task?.notes    ?? '')
const originalPinned   = ref(props.task?.pinned   ?? false)
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
const editedDate  = ref(initialDeadline.date)
const editedTime  = ref(initialDeadline.time)

const editedTitle  = ref(props.task?.title  ?? '')
const editedNotes  = ref(props.task?.notes  ?? '')
const editedPinned = ref(props.task?.pinned ?? false)
const editedRating = ref(props.task?.rating ?? 0)

const priorityIndex = ref(
    PRIORITIES.findIndex(p => p.value === (props.task?.priority ?? 0))
)

const editedPriority = computed(() =>
    PRIORITIES[priorityIndex.value].value
)

const editContainer = ref<HTMLElement | null>(null)

// Combines date and time inputs into a single deadline string
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

// Handles keyboard shortcuts (Enter to save, Escape to cancel)
function handleGlobalKeydown(e: KeyboardEvent) {
  if (!editContainer.value) return

  // Skip if the component is inside a closed modal
  const dialog = editContainer.value.closest('dialog')
  if (dialog && !dialog.open) return

  const isInside = editContainer.value.contains(e.target as Node)
  const isInput = e.target instanceof HTMLInputElement ||
      e.target instanceof HTMLTextAreaElement ||
      e.target instanceof HTMLSelectElement

  if (e.key === 'Enter') {
    const isModifierPressed = e.ctrlKey || e.metaKey
    if (e.target instanceof HTMLTextAreaElement && !isModifierPressed) return
    if (isInside || !isInput) {
      e.preventDefault()
      finishEditing()
    }
  }

  if (e.key === 'Escape') {
    if (isInside || !isInput) {
      e.preventDefault()
      cancelEditing()
    }
  }
}

// Clicks outside the inline editor (edit mode only) auto-save and close
function handleClickOutside() {
  if (isEdit.value) {
    finishEditing()
  }
}

function handleDocumentMousedown(e: MouseEvent) {
  if (!editContainer.value) return
  if (!editContainer.value.contains(e.target as Node)) {
    handleClickOutside()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalKeydown)
  // Only wire up click-outside for inline edit mode, not the create modal
  if (isEdit.value) {
    document.addEventListener('mousedown', handleDocumentMousedown)
  }
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
  document.removeEventListener('mousedown', handleDocumentMousedown)
})

function cancelEditing() {
  if (!isEdit.value) {
    emit('close')
    return
  }
  editedTitle.value  = originalTitle.value
  editedNotes.value  = originalNotes.value
  editedPinned.value = originalPinned.value
  editedRating.value = props.task?.rating ?? 0
  const { date, time } = splitDeadline(originalDeadline.value)
  editedDate.value  = date
  editedTime.value  = time
  priorityIndex.value = PRIORITIES.findIndex(p => p.value === originalPriority.value)
  emit('close')
}

// Resets the component state to default values for creation mode
function resetForm() {
  if (isEdit.value) return

  editedTitle.value  = ''
  editedNotes.value  = ''
  editedDate.value   = ''
  editedTime.value   = ''
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

defineExpose({ resetForm, isFormBlank })

// Saves changes to the store
async function finishEditing() {
  if (loading.value) return

  if (props.task?.completed) {
    emit('close')
    return
  }

  const currentDeadline = combinedDeadline.value
  if (
      isEdit.value &&
      editedTitle.value    === originalTitle.value &&
      editedNotes.value    === originalNotes.value &&
      editedPinned.value   === originalPinned.value &&
      editedRating.value   === (props.task?.rating ?? 0) &&
      currentDeadline      === (originalDeadline.value || null) &&
      editedPriority.value === originalPriority.value
  ) {
    emit('close')
    return
  }

  if (!editedTitle.value.trim()) {
    showValidation.value = true
    if (!isEdit.value) return
    cancelEditing()
    return
  }

  loading.value = true
  try {
    if (isEdit.value && props.task) {
      await tasksStore.editTask(props.task.id, {
        title:    editedTitle.value.trim(),
        notes:    editedNotes.value || null,
        pinned:   editedPinned.value,
        deadline: currentDeadline,
        priority: editedPriority.value,
        rating:   editedRating.value
      })
    } else {
      await tasksStore.createTask({
        title:    editedTitle.value.trim(),
        notes:    editedNotes.value || null,
        deadline: currentDeadline,
        pinned:   editedPinned.value,
        priority: editedPriority.value,
        rating:   editedRating.value
      })
      emit('created')
      resetForm()
    }

    emit('close')
  } finally {
    loading.value = false
  }
}

async function remove() {
  if (isEdit.value && props.task) {
    if (confirm('Delete this task?')) {
      await tasksStore.deleteTask(props.task.id)
    }
  }
}

async function toggleComplete() {
  if (isEdit.value && props.task?.completed) {
    if (!confirm('Mark task as incomplete?')) return
    await tasksStore.toggleComplete(props.task)
  }
}

function resetRating() {
  editedRating.value = 0
}

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

async function moveToTop() {
  if (isEdit.value && props.task) {
    await tasksStore.resetCreationDate(props.task)
  }
}
</script>

<!-- Component Template -->
<template>
  <div
      ref="editContainer"
      :class="[
      isEdit
        ? 'fade-up bg-surface-raised border-b border-swoosh px-3.5 py-4'
        : 'flex flex-col'
    ]"
      @click.stop
  >
    <div class="flex flex-col gap-3">
      <!-- Title Input -->
      <div class="relative">
        <div v-if="(showValidation || isEdit) && !editedTitle.trim()" class="absolute left-0 -top-4 text-[10px] text-swoosh-danger font-bold uppercase px-1">
          Title is required
        </div>
        <input
            type="text"
            class="w-full bg-swoosh-surface-input border border-swoosh-border rounded-sm py-3 px-3.5 text-swoosh-text text-[15.5px] font-bold focus:outline-none focus:border-swoosh-border-hover focus:bg-surface transition-all placeholder:text-swoosh-text-faint"
            maxlength="100"
            placeholder="Task title"
            v-model="editedTitle"
            @input="showValidation = true"
            :disabled="task?.completed"
            required
            autofocus
        />
      </div>

      <!-- Notes Textarea -->
      <textarea
          class="w-full bg-swoosh-surface-input border border-swoosh-border rounded-sm py-2.5 px-3.5 text-swoosh-text-muted text-[13.5px] focus:outline-none focus:border-swoosh-border-hover focus:bg-surface transition-all placeholder:text-swoosh-text-faint min-h-[72px] resize-none"
          placeholder="Notes (optional)"
          maxlength="500"
          v-model="editedNotes"
          :disabled="task?.completed"
      />

      <!-- Deadline Section -->
      <div>
        <div class="text-[10px] font-bold font-mono tracking-widest uppercase text-swoosh-text-faint mb-1.5 ml-1">Deadline</div>
        <div class="flex gap-2">
          <input
              type="date"
              class="flex-1 bg-swoosh-surface-input border border-swoosh-border rounded-sm py-2 px-3 text-swoosh-text text-[13px] focus:outline-none focus:border-swoosh-border-hover focus:bg-surface transition-all"
              v-model="editedDate"
              :disabled="task?.completed"
          />
          <input
              type="time"
              class="flex-1 bg-swoosh-surface-input border border-swoosh-border rounded-sm py-2 px-3 text-swoosh-text text-[13px] focus:outline-none focus:border-swoosh-border-hover focus:bg-surface transition-all"
              v-model="editedTime"
              :disabled="task?.completed"
          />
        </div>
      </div>
    </div>

    <!-- Footer: Priority, Pin, Rating and action buttons -->
    <div class="flex items-center justify-between mt-4 pt-4 border-t border-swoosh">
      <div class="flex items-center gap-2">
        <!-- Priority Cycle -->
        <button
            type="button"
            @click="cyclePriority"
            class="flex items-center gap-2 px-2.5 py-1.5 rounded-sm bg-surface-raised border border-swoosh-border text-swoosh-text-muted hover:text-swoosh-text hover:border-swoosh-text-muted transition-all active:scale-95"
            :class="PRIORITIES[priorityIndex].textClass"
        >
          <component :is="PRIORITIES[priorityIndex].icon" :size="14" fill="currentColor" />
          <span class="text-[11px] font-bold font-mono uppercase tracking-wider">{{ PRIORITIES[priorityIndex].label }}</span>
        </button>

        <!-- Pin Toggle -->
        <button
            type="button"
            @click="editedPinned = !editedPinned"
            class="w-8 h-8 flex items-center justify-center rounded-sm bg-surface-raised border border-swoosh-border transition-all active:scale-95"
            :class="editedPinned ? 'text-swoosh-pin border-swoosh-pin/40 bg-swoosh-pin/10' : 'text-swoosh-text-faint hover:text-swoosh-text-muted'"
        >
          <Pin :size="16" :fill="editedPinned ? 'currentColor' : 'none'" />
        </button>

        <!-- Rating -->
        <TaskRating
            :rating="editedRating"
            :priority="editedPriority"
            interactive
            @update:rating="editedRating = $event"
        />
      </div>

      <!-- Action Buttons -->
      <div class="flex gap-2">
        <button
            @click="cancelEditing"
            class="px-4 py-1.5 text-[13px] font-bold text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors"
        >
          Cancel
        </button>
        <button
            @click="finishEditing"
            class="px-5 py-1.5 bg-swoosh-text text-swoosh-bg text-[13px] font-extrabold rounded-sm hover:scale-[1.02] active:scale-[0.98] transition-all disabled:opacity-50"
            :disabled="loading"
        >
          <span v-if="loading" class="loading loading-spinner loading-xs"></span>
          <span v-else>{{ isEdit ? 'Save' : 'Add task' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>