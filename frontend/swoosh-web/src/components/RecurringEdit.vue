<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Pin, X, Ban, Clock, Calendar } from 'lucide-vue-next'
import { PRIORITIES } from '../types/priority'
import { useRecurringStore } from '../stores/recurring'
import { useTasksStore } from '../stores/tasks'
import TaskRating from './TaskRating.vue'
import TaskIcon from './TaskIcon.vue'
import { TASK_ICONS } from '../types/icon'
import type { RecurrenceType, RecurringTask } from '../types/recurring'

const props = defineProps<{
    /** When provided, the form operates in edit mode for this task. */
    task?: RecurringTask
}>()

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'created'): void
    (e: 'updated'): void
}>()

const isEditMode = computed(() => !!props.task)

const recurringStore = useRecurringStore()
const tasksStore = useTasksStore()
const loading = ref(false)
const showValidation = ref(false)

const editedTitle    = ref('')
const editedNotes    = ref('')
const rInterval      = ref(1)
const rType          = ref<RecurrenceType>('day')
function todayString() {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const rDate          = ref(todayString())
const rTime          = ref('')
const editedPinned   = ref(false)
const editedRating   = ref(0)
const selectedIcon   = ref<number | null>(null)
const showIconPicker = ref(false)
const hiddenDateInput = ref<HTMLInputElement | null>(null)
const hiddenTimeInput = ref<HTMLInputElement | null>(null)

const priorityIndex  = ref(PRIORITIES.findIndex(p => p.value === 0))
const editedPriority = computed(() => PRIORITIES[priorityIndex.value].value)

// Populate fields from task prop when in edit mode
watch(() => props.task, (task) => {
    if (!task) return
    editedTitle.value   = task.title
    editedNotes.value   = task.notes ?? ''
    rInterval.value     = task.recurrenceInterval
    rType.value         = task.recurrenceType
    rDate.value         = task.recurrenceDate ?? ''
    rTime.value         = task.recurrenceTime ?? ''
    editedPinned.value  = task.pinned
    editedRating.value  = task.rating
    selectedIcon.value  = task.icon ?? null
    priorityIndex.value = PRIORITIES.findIndex(p => p.value === task.priority) ?? 0
}, { immediate: true })

function cyclePriority() {
    priorityIndex.value = (priorityIndex.value + 1) % PRIORITIES.length
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

function resetForm() {
    editedTitle.value    = ''
    editedNotes.value    = ''
    rInterval.value      = 1
    rType.value          = 'day'
    rDate.value          = todayString()
    rTime.value          = ''
    editedPinned.value   = false
    editedRating.value   = 0
    selectedIcon.value   = null
    showIconPicker.value = false
    priorityIndex.value  = PRIORITIES.findIndex(p => p.value === 0)
    showValidation.value = false
}

const isFormBlank = computed(() =>
    !editedTitle.value.trim() &&
    !editedNotes.value.trim() &&
    editedPriority.value === 0 &&
    !editedPinned.value &&
    editedRating.value === 0 &&
    selectedIcon.value === null
)

async function submit() {
    if (loading.value) return
    if (!editedTitle.value.trim()) {
        showValidation.value = true
        return
    }
    loading.value = true
    try {
        const payload = {
            title:              editedTitle.value.trim(),
            notes:              editedNotes.value || null,
            recurrenceType:     rType.value,
            recurrenceInterval: Math.max(1, rInterval.value || 1),
            recurrenceDate:     rDate.value || null,
            recurrenceTime:     rTime.value || null,
            isActive:           props.task?.isActive ?? true,
            priority:           editedPriority.value,
            pinned:             editedPinned.value,
            rating:             editedRating.value,
            icon:               selectedIcon.value,
        }
        if (isEditMode.value && props.task) {
            await recurringStore.update(props.task.id, payload)
            emit('updated')
        } else {
            await recurringStore.create(payload)
            await tasksStore.fetchTasks()
            emit('created')
            resetForm()
        }
    } finally {
        loading.value = false
    }
}

defineExpose({ resetForm, isFormBlank })
</script>

<template>
    <div class="flex flex-col">
        <!-- Fields -->
        <div class="px-5 pt-[18px] pb-0 flex flex-col gap-[13px]">

            <!-- Title -->
            <div class="relative">
                <div v-if="showValidation && !editedTitle.trim()" class="absolute left-0 -top-4 text-[10px] text-error font-bold uppercase px-1">
                    Title is required
                </div>
                <input
                    type="text"
                    class="task-edit-input rounded-sm w-full text-base-content font-bold py-[10px] px-[13px] text-[18px]"
                    placeholder="Recurring task title"
                    maxlength="200"
                    v-model="editedTitle"
                    @input="showValidation = true"
                    autofocus
                    @keydown.escape="emit('close')"
                />
            </div>

            <!-- Notes -->
            <textarea
                class="task-edit-input rounded-sm w-full text-swoosh-text-muted resize-none leading-relaxed py-[10px] px-[13px] text-[14.5px] min-h-[80px]"
                placeholder="Notes (optional)"
                maxlength="500"
                v-model="editedNotes"
            />

            <!-- Recurrence + Time -->
            <div>
                <div class="font-bold font-mono uppercase text-swoosh-text-faint text-[11px] tracking-[0.10em] mb-1.5">Repeat every</div>
                <div class="flex gap-1.5 items-center">
                    <!-- Interval number -->
                    <div class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200 w-[58px] flex-shrink-0">
                        <input
                            v-model.number="rInterval"
                            type="number"
                            min="1"
                            max="999"
                            class="flex-1 bg-transparent text-base-content font-mono outline-none py-[10px] px-[8px] text-[14px] w-full text-center"
                        />
                    </div>
                    <!-- Unit dropdown -->
                    <select
                        v-model="rType"
                        class="rounded-sm border border-swoosh bg-base-100 text-base-content font-mono outline-none py-[10px] px-[8px] text-[14px] appearance-none cursor-pointer w-[78px] flex-shrink-0 transition-colors hover:border-swoosh-border-hover focus:border-swoosh-border-hover focus:bg-base-200"
                    >
                        <option value="day">{{ rInterval === 1 ? 'day' : 'days' }}</option>
                        <option value="week">{{ rInterval === 1 ? 'week' : 'weeks' }}</option>
                        <option value="month">{{ rInterval === 1 ? 'month' : 'months' }}</option>
                        <option value="year">{{ rInterval === 1 ? 'year' : 'years' }}</option>
                    </select>
                    <!-- Date: pill when unset, input when set -->
                    <template v-if="!rDate">
                        <button
                            type="button"
                            @click="openDatePicker"
                            class="deadline-shortcut rounded-full font-mono flex items-center gap-1.5 py-[7px] px-3.5 text-[14px] flex-shrink-0"
                        >
                            <Calendar :size="15" />
                            From
                        </button>
                        <input type="date" ref="hiddenDateInput" v-model="rDate" class="sr-only" tabindex="-1" />
                    </template>
                    <div v-else class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200 flex-shrink-0">
                        <input
                            type="date"
                            v-model="rDate"
                            class="bg-transparent text-base-content font-mono outline-none py-[10px] px-[8px] text-[13px]"
                        />
                        <button
                            type="button"
                            @click="rDate = ''"
                            title="Remove date"
                            class="border-l border-swoosh px-1.5 text-swoosh-text-faint hover:text-error hover:bg-base-200 transition-colors flex items-center"
                        >
                            <X :size="10" />
                        </button>
                    </div>

                    <!-- Time: pill when unset, input when set -->
                    <template v-if="!rTime">
                        <button
                            type="button"
                            @click="openTimePicker"
                            class="deadline-shortcut rounded-full font-mono flex items-center gap-1.5 py-[7px] px-3.5 text-[14px] flex-shrink-0"
                        >
                            <Clock :size="15" />
                            Add Time
                        </button>
                        <input type="time" ref="hiddenTimeInput" v-model="rTime" class="sr-only" tabindex="-1" />
                    </template>
                    <div v-else class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200 flex-shrink-0">
                        <input
                            type="time"
                            v-model="rTime"
                            class="bg-transparent text-base-content font-mono outline-none py-[10px] px-[8px] text-[13px]"
                        />
                        <button
                            type="button"
                            @click="rTime = ''"
                            title="Remove time"
                            class="border-l border-swoosh px-1.5 text-swoosh-text-faint hover:text-error hover:bg-base-200 transition-colors flex items-center"
                        >
                            <X :size="10" />
                        </button>
                    </div>
                </div>
            </div>

        </div>

        <!-- Footer: priority, pin, icon, rating, actions -->
        <div class="flex flex-wrap items-center gap-y-2 border-t border-swoosh mt-[13px] px-5 pt-3 pb-[18px]">
            <div class="flex items-center gap-[5px]">

                <!-- Priority Cycle -->
                <button
                    type="button"
                    @click="cyclePriority"
                    title="Priority level"
                    class="flex items-center gap-1 py-[5px] pl-2 pr-[10px] rounded-sm border text-[13px] font-mono transition-colors"
                    :class="PRIORITIES[priorityIndex].value === 0
                        ? 'border-swoosh text-swoosh-text-faint hover:text-swoosh-text-muted hover:border-swoosh-border-hover'
                        : PRIORITIES[priorityIndex].textClass + ' border-current/25'"
                >
                    <component :is="PRIORITIES[priorityIndex].icon" :size="13" fill="currentColor" />
                    <span class="uppercase tracking-wider">{{ PRIORITIES[priorityIndex].shortLabel }}</span>
                </button>

                <!-- Pin Toggle -->
                <button
                    type="button"
                    @click="editedPinned = !editedPinned"
                    :title="editedPinned ? 'Unpin' : 'Pin'"
                    class="w-8 h-8 flex items-center justify-center rounded-sm border transition-colors"
                    :class="editedPinned
                        ? 'text-secondary border-secondary/25'
                        : 'text-swoosh-text-faint border-swoosh hover:text-swoosh-text-muted hover:border-swoosh-border-hover'"
                >
                    <Pin :size="14" :fill="editedPinned ? 'currentColor' : 'none'" />
                </button>

                <!-- Icon Picker -->
                <div class="relative" v-click-outside="() => showIconPicker = false">
                    <button
                        type="button"
                        @click="showIconPicker = !showIconPicker"
                        title="Select icon"
                        class="w-8 h-8 flex items-center justify-center rounded-sm border transition-colors"
                        :class="selectedIcon !== null
                            ? 'border-current/25 ' + TASK_ICONS.find(i => i.value === selectedIcon)?.color
                            : 'text-swoosh-text-faint border-swoosh hover:text-swoosh-text-muted hover:border-swoosh-border-hover'"
                    >
                        <TaskIcon v-if="selectedIcon !== null" :value="selectedIcon" />
                        <Ban v-else :size="14" />
                    </button>
                    <div
                        v-if="showIconPicker"
                        class="absolute bottom-full left-0 mb-2 z-50 bg-base-300 border border-swoosh rounded-[8px] p-2 grid grid-cols-6 gap-1 w-[168px]"
                    >
                        <button
                            type="button"
                            @click="selectedIcon = null; showIconPicker = false"
                            class="col-span-6 flex items-center justify-center gap-1.5 rounded-sm py-1 mb-1 text-[11px] font-mono uppercase tracking-wider transition-colors hover:bg-base-200"
                            :class="selectedIcon === null ? 'bg-base-200 text-base-content' : 'text-swoosh-text-faint'"
                        >
                            <Ban :size="11" />
                            No icon
                        </button>
                        <button
                            v-for="icon in TASK_ICONS"
                            :key="icon.value"
                            type="button"
                            @click="selectedIcon = icon.value; showIconPicker = false"
                            class="w-7 h-7 flex items-center justify-center rounded-sm transition-colors hover:bg-base-200"
                            :class="[icon.color, selectedIcon === icon.value ? 'bg-base-200' : '']"
                        >
                            <component :is="icon.icon" :size="15" />
                        </button>
                    </div>
                </div>

                <!-- Rating -->
                <TaskRating
                    :rating="editedRating"
                    :priority="editedPriority"
                    interactive
                    @update:rating="editedRating = $event"
                />
            </div>

            <!-- Action Buttons -->
            <div class="ml-auto w-full min-[380px]:w-auto flex gap-[7px] items-center">
                <!-- Full-width on very small screens -->
                <button
                    @click="emit('close')"
                    class="flex-1 min-[380px]:hidden rounded-sm border border-swoosh text-swoosh-text-faint text-[14px] transition-colors hover:text-swoosh-text-muted hover:border-swoosh-border-hover px-[14px] py-[7px]"
                >Cancel</button>
                <button
                    @click="submit"
                    class="flex-1 min-[380px]:hidden rounded-sm border border-swoosh-text-muted bg-transparent text-base-content text-[14px] font-medium transition-colors hover:bg-base-300 px-[18px] py-[7px] disabled:opacity-40"
                    :disabled="loading"
                >
                    <span v-if="loading" class="loading loading-spinner loading-xs"></span>
                    <span v-else>{{ isEditMode ? 'Save changes' : 'Add recurring' }}</span>
                </button>

                <!-- Conjoined icon buttons — mobile (380px–sm) -->
                <div class="hidden min-[380px]:flex sm:hidden rounded-sm overflow-hidden border border-swoosh">
                    <button
                        @click="emit('close')"
                        title="Cancel"
                        class="w-8 h-8 flex items-center justify-center border-r border-swoosh text-swoosh-text-faint hover:text-swoosh-text-muted hover:bg-base-200 transition-colors"
                    ><X :size="14" /></button>
                    <button
                        @click="submit"
                        title="Add recurring"
                        class="w-8 h-8 flex items-center justify-center text-base-content hover:bg-base-200 transition-colors disabled:opacity-40"
                        :disabled="loading"
                    >
                        <span v-if="loading" class="loading loading-spinner loading-xs"></span>
                        <span v-else class="text-[11px] font-mono">{{ isEditMode ? '✓' : '+' }}</span>
                    </button>
                </div>

                <!-- Text buttons — sm and up -->
                <button
                    @click="emit('close')"
                    class="hidden sm:block rounded-sm border border-swoosh text-swoosh-text-faint text-[14px] transition-colors hover:text-swoosh-text-muted hover:border-swoosh-border-hover px-[14px] py-[7px]"
                >Cancel</button>
                <button
                    @click="submit"
                    class="hidden sm:block rounded-sm border border-swoosh-text-muted bg-transparent text-base-content text-[14px] font-medium transition-colors hover:bg-base-300 px-[18px] py-[7px] disabled:opacity-40"
                    :disabled="loading"
                >
                    <span v-if="loading" class="loading loading-spinner loading-xs"></span>
                    <span v-else>{{ isEditMode ? 'Save changes' : 'Add recurring' }}</span>
                </button>
            </div>
        </div>
    </div>
</template>
