<!--
  SubtaskEdit.vue
  Compact inline editor for subtasks.
  Handles both creating (parentTaskId prop) and editing (task prop).
  Only exposes title, notes, and deadline — no priority, pin, icon, or rating.
-->
<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { X, Check, Calendar, Clock } from 'lucide-vue-next'

const props = defineProps<{
  task?: Task
  parentTaskId?: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'created'): void
}>()

const isEdit = computed(() => !!props.task)
const tasksStore = useTasksStore()
const loading = ref(false)
const showValidation = ref(false)
const deadlineError = ref(false)

const parentTask = computed(() => {
  const parentId = props.parentTaskId ?? props.task?.parentId
  if (!parentId) return null
  return tasksStore.tasks.find(t => t.id === parentId) ?? null
})

const originalTitle    = ref(props.task?.title    ?? '')
const originalNotes    = ref(props.task?.notes    ?? '')
const originalDeadline = ref(props.task?.deadline ?? '')

function splitDeadline(deadline: string | null) {
  if (!deadline) return { date: '', time: '' }
  const [date, fullTime] = deadline.split('T')
  const time = fullTime ? fullTime.substring(0, 5) : ''
  return { date, time }
}

const initialDeadline = splitDeadline(props.task?.deadline ?? null)
const editedDate  = ref(initialDeadline.date)
const editedTime  = ref(initialDeadline.time)
const editedTitle = ref(props.task?.title ?? '')
const editedNotes = ref(props.task?.notes ?? '')

const editContainer   = ref<HTMLElement | null>(null)
const hiddenDateInput = ref<HTMLInputElement | null>(null)
const hiddenTimeInput = ref<HTMLInputElement | null>(null)

const combinedDeadline = computed(() => {
  if (!editedDate.value && !editedTime.value) return null
  let date = editedDate.value
  let time = editedTime.value
  if (date && !time) time = '23:59'
  else if (!date && time) date = localDateString(new Date())
  return `${date}T${time}:00`
})

function localDateString(d: Date) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
function setToday() {
  editedDate.value = localDateString(new Date())
  deadlineError.value = false
}
function setTomorrow() {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  editedDate.value = localDateString(d)
  deadlineError.value = false
}
function clearDate() {
  editedDate.value = ''
  editedTime.value = ''
  deadlineError.value = false
}
function openDatePicker() {
  const input = hiddenDateInput.value
  if (!input) return
  try { input.showPicker() } catch { input.click() }
}
function openTimePicker() {
  const input = hiddenTimeInput.value
  if (!input) return
  try { input.showPicker() } catch { input.click() }
}

function handleGlobalKeydown(e: KeyboardEvent) {
  if (!editContainer.value) return
  const dialog = editContainer.value.closest('dialog')
  if (dialog && !dialog.open) return
  const isInside = editContainer.value.contains(e.target as Node)
  const isInput  = e.target instanceof HTMLInputElement ||
      e.target instanceof HTMLTextAreaElement

  if (e.key === 'Enter') {
    const isModifierPressed = e.ctrlKey || e.metaKey
    if (e.target instanceof HTMLTextAreaElement && !isModifierPressed) return
    if (isInside || !isInput) { e.preventDefault(); finishEditing() }
  }
  if (e.key === 'Escape') {
    if (isInside || !isInput) { e.preventDefault(); cancelEditing() }
  }
}

function handleDocumentMousedown(e: MouseEvent) {
  if (!editContainer.value) return
  if (!editContainer.value.contains(e.target as Node)) {
    if (isEdit.value) finishEditing()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalKeydown)
  if (isEdit.value) document.addEventListener('mousedown', handleDocumentMousedown)
})
onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
  document.removeEventListener('mousedown', handleDocumentMousedown)
})

function cancelEditing() {
  if (!isEdit.value) { emit('close'); return }
  editedTitle.value = originalTitle.value
  editedNotes.value = originalNotes.value
  const { date, time } = splitDeadline(originalDeadline.value)
  editedDate.value = date
  editedTime.value = time
  emit('close')
}

async function finishEditing() {
  if (loading.value) return
  if (props.task?.completed) { emit('close'); return }

  const currentDeadline = combinedDeadline.value

  if (
    isEdit.value &&
    editedTitle.value === originalTitle.value &&
    editedNotes.value === originalNotes.value &&
    currentDeadline   === (originalDeadline.value || null)
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

  if (currentDeadline && parentTask.value?.deadline) {
    if (new Date(currentDeadline) > new Date(parentTask.value.deadline)) {
      deadlineError.value = true
      return
    }
  }

  loading.value = true
  try {
    if (isEdit.value && props.task) {
      await tasksStore.editTask(props.task.id, {
        title:    editedTitle.value.trim(),
        notes:    editedNotes.value || null,
        deadline: currentDeadline,
        pinned:   false,
        priority: 0,
        rating:   0,
        icon:     null,
      })
    } else if (props.parentTaskId) {
      await tasksStore.createSubtask(props.parentTaskId, {
        title:    editedTitle.value.trim(),
        notes:    editedNotes.value || null,
        deadline: currentDeadline,
      })
      emit('created')
      editedTitle.value    = ''
      editedNotes.value    = ''
      editedDate.value     = ''
      editedTime.value     = ''
      showValidation.value = false
    }
    emit('close')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div ref="editContainer" class="subtask-edit fade-up" @click.stop>

    <!-- Title -->
    <div class="relative mb-1.5">
      <div v-if="showValidation && !editedTitle.trim()"
           class="absolute left-0 -top-3 text-[10px] text-error font-bold uppercase px-0.5">
        Required
      </div>
      <input
          type="text"
          class="task-edit-input rounded-sm w-full text-base-content font-bold py-[6px] px-2.5 text-[14px]"
          maxlength="100"
          placeholder="Subtask title"
          v-model="editedTitle"
          @input="showValidation = true"
          :disabled="task?.completed"
          autofocus
      />
    </div>

    <!-- Notes -->
    <textarea
        class="task-edit-input rounded-sm w-full text-swoosh-text-muted resize-none leading-relaxed py-[5px] px-2.5 text-[12.5px] min-h-[34px] mb-1.5"
        placeholder="Notes (optional)"
        maxlength="500"
        v-model="editedNotes"
        :disabled="task?.completed"
    />

    <!-- Deadline error -->
    <div v-if="deadlineError" class="text-[10px] text-error font-bold uppercase px-0.5 mb-1">
      Cannot be later than parent deadline
    </div>

    <!-- Deadline + actions on one row -->
    <div class="flex items-center gap-1.5 flex-wrap">

      <!-- No date: quick-pick pills -->
      <template v-if="!editedDate">
        <button type="button" @click="setToday" :disabled="task?.completed"
                class="deadline-shortcut rounded-full font-mono py-[3px] px-2.5 text-[11.5px] disabled:opacity-40">
          Today
        </button>
        <button type="button" @click="setTomorrow" :disabled="task?.completed"
                class="deadline-shortcut rounded-full font-mono py-[3px] px-2.5 text-[11.5px] disabled:opacity-40">
          Tomorrow
        </button>
        <button type="button" @click="openDatePicker" :disabled="task?.completed" title="Pick a date"
                class="deadline-shortcut rounded-full flex items-center justify-center py-[5px] px-2 disabled:opacity-40">
          <Calendar :size="12" />
        </button>
        <input type="date" ref="hiddenDateInput" v-model="editedDate" class="sr-only" tabindex="-1" />
      </template>

      <!-- Date selected -->
      <template v-else>
        <div class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover">
          <input
              type="date"
              class="bg-transparent text-base-content font-mono outline-none py-[3px] px-2 text-[11.5px] disabled:opacity-40"
              v-model="editedDate"
              :disabled="task?.completed"
              @change="deadlineError = false"
          />
          <button type="button" @click="clearDate" :disabled="task?.completed" title="Remove deadline"
                  class="border-l border-swoosh px-1.5 text-swoosh-text-faint hover:text-error hover:bg-base-200 transition-colors disabled:opacity-40 flex items-center">
            <X :size="10" />
          </button>
        </div>

        <template v-if="!editedTime || editedTime === '23:59'">
          <button type="button" @click="openTimePicker" :disabled="task?.completed"
                  class="deadline-shortcut rounded-full font-mono flex items-center gap-1 py-[3px] px-2.5 text-[11.5px] disabled:opacity-40">
            <Clock :size="12" />
            Time
          </button>
          <input type="time" ref="hiddenTimeInput" v-model="editedTime" class="sr-only" tabindex="-1" />
        </template>
        <div v-else class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover">
          <input
              type="time"
              class="bg-transparent text-base-content font-mono outline-none py-[3px] px-2 text-[11.5px] disabled:opacity-40"
              v-model="editedTime"
              :disabled="task?.completed"
          />
          <button type="button" @click="editedTime = ''" :disabled="task?.completed" title="Remove time"
                  class="border-l border-swoosh px-1.5 text-swoosh-text-faint hover:text-error hover:bg-base-200 transition-colors disabled:opacity-40 flex items-center">
            <X :size="10" />
          </button>
        </div>
      </template>

      <!-- Action buttons pushed to the right -->
      <div class="ml-auto flex gap-1">
        <button
            @click="cancelEditing"
            class="rounded-sm border border-swoosh text-swoosh-text-faint text-[12px] transition-colors hover:text-swoosh-text-muted hover:border-swoosh-border-hover px-2.5 py-[4px]"
        >
          Cancel
        </button>
        <button
            @click="finishEditing"
            class="rounded-sm border border-swoosh-text-muted bg-transparent text-base-content text-[12px] font-medium transition-colors hover:bg-base-300 px-3 py-[4px] disabled:opacity-40"
            :disabled="loading"
        >
          <span v-if="loading" class="loading loading-spinner loading-xs"></span>
          <span v-else>{{ isEdit ? 'Save' : 'Add' }}</span>
        </button>
      </div>
    </div>

  </div>
</template>

<style scoped>
input[type="date"],
input[type="time"] {
  color-scheme: dark;
}
input[type="date"]::-webkit-calendar-picker-indicator,
input[type="time"]::-webkit-calendar-picker-indicator {
  cursor: pointer; opacity: 0.6; transition: opacity 0.15s;
}
input[type="date"]::-webkit-calendar-picker-indicator:hover,
input[type="time"]::-webkit-calendar-picker-indicator:hover {
  opacity: 1;
}
</style>
