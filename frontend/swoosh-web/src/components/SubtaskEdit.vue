<!--
  SubtaskEdit.vue
  Compact inline editor for subtasks.
  Handles both creating (parentTaskId prop) and editing (task prop).
  Only exposes title, notes, and deadline — no priority, pin, icon, or rating.
-->
<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { X, Check, Calendar, Clock, Timer } from 'lucide-vue-next'

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

const originalTitle        = ref(props.task?.title        ?? '')
const originalNotes        = ref(props.task?.notes        ?? '')
const originalDeadline     = ref(props.task?.deadline     ?? '')
const originalTimerDuration = ref(props.task?.timerDuration ?? 0)

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

// ── Timer duration ────────────────────────────────────────────────────────────

const editedTimerMs   = ref<number>(props.task?.timerDuration ?? 0)
const showCustomTimer = ref(false)
const customTimerH    = ref(0)
const customTimerM    = ref(0)

const maxTimerMs = computed(() => {
  if (!editedDate.value || !editedTime.value || editedTime.value === '23:59' || editedTime.value === '') return 0
  const [year, month, day] = editedDate.value.split('-').map(Number)
  const [hour, minute]     = editedTime.value.split(':').map(Number)
  const deadline = new Date(year, month - 1, day, hour, minute, 0, 0)
  const nextDay  = new Date(year, month - 1, day)
  nextDay.setDate(nextDay.getDate() + 1)
  return Math.max(0, Math.min(28_800_000, nextDay.getTime() - deadline.getTime()))
})

const hasDateAndTime = computed(() =>
  !!editedDate.value &&
  !!editedTime.value &&
  editedTime.value !== '23:59' &&
  editedTime.value !== '' &&
  maxTimerMs.value > 0
)

const TIMER_PRESETS = [
  { label: '15m', ms: 15 * 60_000 },
  { label: '30m', ms: 30 * 60_000 },
  { label: '1h',  ms: 60 * 60_000 },
  { label: '2h',  ms: 120 * 60_000 },
  { label: '4h',  ms: 240 * 60_000 },
  { label: '8h',  ms: 480 * 60_000 },
]
const timerPresets = computed(() => TIMER_PRESETS.filter(p => p.ms <= maxTimerMs.value))

function formatTimerMs(ms: number): string {
  const totalMin = Math.floor(ms / 60_000)
  const h = Math.floor(totalMin / 60)
  const m = totalMin % 60
  if (h > 0 && m > 0) return `${h}h ${m}m`
  if (h > 0) return `${h}h`
  return `${m}m`
}

function openCustomTimer() {
  const totalMin = Math.floor(editedTimerMs.value / 60_000)
  customTimerH.value = Math.floor(totalMin / 60)
  customTimerM.value = totalMin % 60
  showCustomTimer.value = true
}

function applyCustomTimer() {
  const ms = (customTimerH.value * 60 + customTimerM.value) * 60_000
  editedTimerMs.value = Math.min(ms, maxTimerMs.value)
  showCustomTimer.value = false
}

// Clear timer whenever date or time changes
watch([editedDate, editedTime], () => {
  editedTimerMs.value = 0
  showCustomTimer.value = false
})

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
  editedTitle.value    = originalTitle.value
  editedNotes.value    = originalNotes.value
  editedTimerMs.value  = originalTimerDuration.value
  showCustomTimer.value = false
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
    editedTitle.value   === originalTitle.value &&
    editedNotes.value   === originalNotes.value &&
    currentDeadline     === (originalDeadline.value || null) &&
    editedTimerMs.value === originalTimerDuration.value
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
        title:         editedTitle.value.trim(),
        notes:         editedNotes.value || null,
        deadline:      currentDeadline,
        pinned:        false,
        priority:      0,
        rating:        0,
        icon:          null,
        timerDuration: editedTimerMs.value || null,
      })
    } else if (props.parentTaskId) {
      await tasksStore.createSubtask(props.parentTaskId, {
        title:         editedTitle.value.trim(),
        notes:         editedNotes.value || null,
        deadline:      currentDeadline,
        timerDuration: editedTimerMs.value || null,
      })
      emit('created')
      editedTitle.value     = ''
      editedNotes.value     = ''
      editedDate.value      = ''
      editedTime.value      = ''
      editedTimerMs.value   = 0
      showCustomTimer.value = false
      showValidation.value  = false
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

    <!-- Deadline row -->
    <div class="flex items-center gap-1.5 flex-wrap mb-1.5">
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
    </div>

    <!-- Timer (grace period) row — only when date + specific time are set -->
    <div v-if="hasDateAndTime && !task?.completed" class="flex items-center gap-1.5 flex-wrap mb-1.5">
      <!-- Custom H:M input -->
      <template v-if="showCustomTimer">
        <div class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 focus-within:border-swoosh-border-hover transition-colors">
          <input type="number" v-model.number="customTimerH" min="0" :max="Math.floor(maxTimerMs / 3_600_000)"
                 class="w-8 bg-transparent text-base-content font-mono outline-none text-center py-[3px] px-1 text-[11.5px]" placeholder="0" />
          <span class="self-center text-swoosh-text-faint font-mono text-[11px] pr-0.5">h</span>
          <input type="number" v-model.number="customTimerM" min="0" max="59"
                 class="w-8 bg-transparent text-base-content font-mono outline-none text-center py-[3px] px-1 text-[11.5px]" placeholder="0" />
          <span class="self-center text-swoosh-text-faint font-mono text-[11px] pl-0.5 pr-1">m</span>
        </div>
        <button type="button" @click="applyCustomTimer"
                class="deadline-shortcut rounded-full font-mono py-[3px] px-2.5 text-[11.5px]">Set</button>
        <button type="button" @click="showCustomTimer = false" class="text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">
          <X :size="12" />
        </button>
      </template>

      <!-- Timer set: show badge -->
      <template v-else-if="editedTimerMs > 0">
        <div class="flex rounded-sm overflow-hidden border border-warning/25 bg-warning/8">
            <span class="flex items-center gap-1 text-warning font-mono py-[3px] px-2 text-[11.5px]">
              <Timer :size="11" />
              {{ formatTimerMs(editedTimerMs) }}
            </span>
          <button type="button" @click="editedTimerMs = 0" title="Remove timer"
                  class="border-l border-warning/25 px-1.5 text-warning/60 hover:text-error hover:bg-base-200 transition-colors flex items-center">
            <X :size="10" />
          </button>
        </div>
        <button type="button" @click="openCustomTimer" class="text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors" title="Edit timer">
          <Timer :size="12" />
        </button>
      </template>

      <!-- No timer: presets + custom button -->
      <template v-else>
        <button v-for="preset in timerPresets" :key="preset.ms" type="button" @click="editedTimerMs = preset.ms"
                class="deadline-shortcut rounded-full font-mono py-[3px] px-2.5 text-[11.5px]">
          {{ preset.label }}
        </button>
        <button type="button" @click="openCustomTimer" title="Custom duration"
                class="deadline-shortcut rounded-full flex items-center justify-center py-[5px] px-2">
          <Timer :size="12" />
        </button>
      </template>
    </div>

    <!-- Actions row -->
    <div class="flex items-center">
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
