<script setup lang="ts">
import { ref, computed } from 'vue'
import { Pin, X, Ban } from 'lucide-vue-next'
import { PRIORITIES } from '../types/priority'
import { useRecurringStore } from '../stores/recurring'
import TaskRating from './TaskRating.vue'
import TaskIcon from './TaskIcon.vue'
import { TASK_ICONS } from '../types/icon'
import type { RecurrenceType } from '../types/recurring'

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'created'): void
}>()

const recurringStore = useRecurringStore()
const loading = ref(false)
const showValidation = ref(false)

const editedTitle    = ref('')
const editedNotes    = ref('')
const rType          = ref<RecurrenceType>('daily')
const rInterval      = ref<number | null>(null)
const editedPinned   = ref(false)
const editedRating   = ref(0)
const selectedIcon   = ref<number | null>(null)
const showIconPicker = ref(false)

const priorityIndex = ref(PRIORITIES.findIndex(p => p.value === 0))
const editedPriority = computed(() => PRIORITIES[priorityIndex.value].value)

function cyclePriority() {
    priorityIndex.value = (priorityIndex.value + 1) % PRIORITIES.length
}

function resetForm() {
    editedTitle.value    = ''
    editedNotes.value    = ''
    rType.value          = 'daily'
    rInterval.value      = null
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
        await recurringStore.create({
            title: editedTitle.value.trim(),
            notes: editedNotes.value || null,
            recurrenceType: rType.value,
            recurrenceInterval: rType.value === 'interval' ? rInterval.value : null,
            isActive: true,
            priority: editedPriority.value,
            pinned: editedPinned.value,
            rating: editedRating.value,
            icon: selectedIcon.value,
        })
        emit('created')
        resetForm()
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

            <!-- Recurrence -->
            <div>
                <div class="font-bold font-mono uppercase text-swoosh-text-faint text-[11px] tracking-[0.10em] mb-1.5">Recurrence</div>
                <div class="flex gap-2 items-center">
                    <!-- Type select -->
                    <div class="flex flex-1 rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200">
                        <select
                            v-model="rType"
                            class="flex-1 bg-transparent text-base-content font-mono outline-none py-[10px] px-[13px] text-[14px] appearance-none cursor-pointer"
                        >
                            <option value="daily">Every day</option>
                            <option value="interval">Every X days</option>
                            <option value="weekly">Every week</option>
                            <option value="monthly">Every month</option>
                            <option value="custom">Custom</option>
                        </select>
                    </div>
                    <!-- Interval input (only for "interval" type) -->
                    <template v-if="rType === 'interval'">
                        <div class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200 w-[110px]">
                            <input
                                v-model.number="rInterval"
                                type="number"
                                min="1"
                                max="365"
                                class="flex-1 bg-transparent text-base-content font-mono outline-none py-[10px] px-[13px] text-[14px] w-full"
                                placeholder="days"
                            />
                        </div>
                    </template>
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
                    <span v-else>Add recurring</span>
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
                        <span v-else class="text-[11px] font-mono">+</span>
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
                    <span v-else>Add recurring</span>
                </button>
            </div>
        </div>
    </div>
</template>
