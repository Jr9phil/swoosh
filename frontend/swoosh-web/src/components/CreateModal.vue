<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useUiStore } from '../stores/ui'
import { useRecurringStore } from '../stores/recurring'
import { useNoteCardsStore } from '../stores/notecards'
import { useRemindersStore } from '../stores/reminders'
import TaskEdit from './TaskEdit.vue'
import { X } from 'lucide-vue-next'
import type { CreateModalTab } from '../stores/ui'
import type { RecurrenceType } from '../types/recurring'

const ui = useUiStore()
const recurringStore = useRecurringStore()
const noteCardsStore = useNoteCardsStore()
const remindersStore = useRemindersStore()

const dialogEl = ref<HTMLDialogElement | null>(null)
const taskEditRef = ref<InstanceType<typeof TaskEdit> | null>(null)

// ── Recurring form ─────────────────────────────────────
const rTitle = ref('')
const rNotes = ref('')
const rType = ref<RecurrenceType>('daily')
const rInterval = ref<number | null>(null)
const rActive = ref(true)

// ── NoteCard form ──────────────────────────────────────
const nTitle = ref('')
const nBody = ref('')

// ── Reminder form ──────────────────────────────────────
const remTitle = ref('')
const remNotes = ref('')
const remAt = ref('')

function defaultRemindAt() {
    const d = new Date()
    d.setMinutes(0, 0, 0)
    d.setHours(d.getHours() + 1)
    return d.toISOString().slice(0, 16)
}

// ── Open / close ───────────────────────────────────────
watch(() => ui.openCreateModal, (val) => {
    if (!val) return
    resetForms()
    dialogEl.value?.showModal()
    ui.consumeCreateModal()

    nextTick(() => {
        if (ui.activeTab === 'task') {
            if (ui.taskPrefill) {
                taskEditRef.value?.prefill(
                    ui.taskPrefill.title ?? '',
                    ui.taskPrefill.notes ?? null,
                    ui.taskPrefill.deadline ?? null,
                    ui.taskPrefill.priority
                )
            } else if (ui.taskDate) {
                taskEditRef.value?.setDate(ui.taskDate)
            }
        }
    })
})

function close() {
    dialogEl.value?.close()
}

function handleDialogClose() {
    taskEditRef.value?.resetForm()
    ui.resetModal()
}

function resetForms() {
    taskEditRef.value?.resetForm()
    rTitle.value = ''
    rNotes.value = ''
    rType.value = 'daily'
    rInterval.value = null
    rActive.value = true
    nTitle.value = ''
    nBody.value = ''
    remTitle.value = ''
    remNotes.value = ''
    remAt.value = defaultRemindAt()
}

function switchTab(tab: CreateModalTab) {
    ui.activeTab = tab
}

// ── Submit handlers ────────────────────────────────────
function handleTaskCreated() {
    ui.onTaskCreated?.()
    close()
}

async function submitRecurring() {
    if (!rTitle.value.trim()) return
    await recurringStore.create({
        title: rTitle.value,
        notes: rNotes.value || null,
        recurrenceType: rType.value,
        recurrenceInterval: rType.value === 'interval' ? rInterval.value : null,
        isActive: rActive.value,
    })
    close()
}

async function submitNoteCard() {
    if (!nTitle.value.trim()) return
    await noteCardsStore.create({
        title: nTitle.value,
        body: nBody.value || null,
        positionX: Math.round(80 + Math.random() * 200),
        positionY: Math.round(80 + Math.random() * 120),
    })
    close()
}

async function submitReminder() {
    if (!remTitle.value.trim() || !remAt.value) return
    await remindersStore.create({
        title: remTitle.value,
        notes: remNotes.value || null,
        remindAt: new Date(remAt.value).toISOString(),
        isCompleted: false,
    })
    close()
}

const tabs: { id: CreateModalTab; label: string }[] = [
    { id: 'task',      label: 'Task' },
    { id: 'recurring', label: 'Recurring' },
    { id: 'notecard',  label: 'Note' },
    { id: 'reminder',  label: 'Reminder' },
]
</script>

<template>
    <dialog
        ref="dialogEl"
        id="create_modal"
        class="modal bg-black/60"
        @close="handleDialogClose"
    >
        <div class="modal-box bg-base-200 border border-swoosh-border-hover p-0 max-w-[520px] xl:max-w-[640px] rounded-[10px] overflow-hidden shadow-[0_0_0_1px_rgba(255,255,255,0.06),0_24px_64px_rgba(0,0,0,0.8)]">

            <!-- Modal header with tabs -->
            <div class="create-modal-header">
                <div class="create-modal-tabs">
                    <button
                        v-for="tab in tabs"
                        :key="tab.id"
                        class="create-modal-tab"
                        :class="{ 'is-active': ui.activeTab === tab.id }"
                        @click="switchTab(tab.id)"
                    >
                        {{ tab.label }}
                    </button>
                </div>
                <button
                    class="create-modal-close"
                    @click="close"
                >
                    <X :size="15" stroke-width="2" />
                </button>
            </div>

            <!-- Task tab -->
            <template v-if="ui.activeTab === 'task'">
                <TaskEdit ref="taskEditRef" @close="close" @created="handleTaskCreated" />
            </template>

            <!-- Recurring tab -->
            <template v-else-if="ui.activeTab === 'recurring'">
                <div class="create-modal-body">
                    <div class="create-form-field">
                        <label class="create-form-label">Title</label>
                        <input
                            v-model="rTitle"
                            class="input input-sm w-full"
                            placeholder="Task title"
                            maxlength="200"
                            autofocus
                            @keydown.escape="close"
                        />
                    </div>
                    <div class="create-form-field">
                        <label class="create-form-label">Recurrence</label>
                        <select v-model="rType" class="select select-sm w-full">
                            <option value="daily">Every day</option>
                            <option value="interval">Every X days</option>
                            <option value="weekly">Every week</option>
                            <option value="monthly">Every month</option>
                            <option value="custom">Custom</option>
                        </select>
                    </div>
                    <div v-if="rType === 'interval'" class="create-form-field">
                        <label class="create-form-label">Interval (days)</label>
                        <input
                            v-model.number="rInterval"
                            type="number"
                            min="1"
                            max="365"
                            class="input input-sm w-full"
                            placeholder="e.g. 3"
                        />
                    </div>
                    <div class="create-form-field">
                        <label class="create-form-label">Notes</label>
                        <textarea
                            v-model="rNotes"
                            class="textarea textarea-sm w-full resize-none"
                            placeholder="Optional notes"
                            rows="2"
                            maxlength="1000"
                        />
                    </div>
                </div>
                <div class="create-modal-footer">
                    <button class="btn btn-sm btn-ghost" @click="close">Cancel</button>
                    <button class="btn btn-sm btn-primary" :disabled="!rTitle.trim()" @click="submitRecurring">
                        Create
                    </button>
                </div>
            </template>

            <!-- NoteCard tab -->
            <template v-else-if="ui.activeTab === 'notecard'">
                <div class="create-modal-body">
                    <div class="create-form-field">
                        <label class="create-form-label">Title</label>
                        <input
                            v-model="nTitle"
                            class="input input-sm w-full"
                            placeholder="Note title"
                            maxlength="200"
                            autofocus
                            @keydown.escape="close"
                        />
                    </div>
                    <div class="create-form-field">
                        <label class="create-form-label">Note</label>
                        <textarea
                            v-model="nBody"
                            class="textarea textarea-sm w-full resize-none"
                            placeholder="What's on your mind?"
                            rows="5"
                            maxlength="4000"
                        />
                    </div>
                </div>
                <div class="create-modal-footer">
                    <button class="btn btn-sm btn-ghost" @click="close">Cancel</button>
                    <button class="btn btn-sm btn-primary" :disabled="!nTitle.trim()" @click="submitNoteCard">
                        Create
                    </button>
                </div>
            </template>

            <!-- Reminder tab -->
            <template v-else-if="ui.activeTab === 'reminder'">
                <div class="create-modal-body">
                    <div class="create-form-field">
                        <label class="create-form-label">Title</label>
                        <input
                            v-model="remTitle"
                            class="input input-sm w-full"
                            placeholder="Remind me to..."
                            maxlength="200"
                            autofocus
                            @keydown.escape="close"
                        />
                    </div>
                    <div class="create-form-field">
                        <label class="create-form-label">When</label>
                        <input
                            v-model="remAt"
                            type="datetime-local"
                            class="input input-sm w-full"
                        />
                    </div>
                    <div class="create-form-field">
                        <label class="create-form-label">Notes</label>
                        <textarea
                            v-model="remNotes"
                            class="textarea textarea-sm w-full resize-none"
                            placeholder="Optional notes"
                            rows="2"
                            maxlength="1000"
                        />
                    </div>
                </div>
                <div class="create-modal-footer">
                    <button class="btn btn-sm btn-ghost" @click="close">Cancel</button>
                    <button class="btn btn-sm btn-primary" :disabled="!remTitle.trim() || !remAt" @click="submitReminder">
                        Create
                    </button>
                </div>
            </template>

        </div>
        <form method="dialog" class="modal-backdrop">
            <button>close</button>
        </form>
    </dialog>
</template>

<style scoped>
.create-modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 16px 0;
    background: var(--color-base-300);
    border-bottom: 1px solid var(--color-swoosh-border);
    border-radius: 10px 10px 0 0;
}

.create-modal-tabs {
    display: flex;
    gap: 2px;
}

.create-modal-tab {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--color-swoosh-text-faint);
    padding: 8px 12px;
    border-radius: 6px 6px 0 0;
    border: none;
    background: none;
    cursor: pointer;
    transition: color 0.15s, background-color 0.15s;
    position: relative;
    bottom: -1px;
}

.create-modal-tab:hover {
    color: var(--color-swoosh-text-muted);
    background-color: color-mix(in srgb, var(--color-base-content) 5%, transparent);
}

.create-modal-tab.is-active {
    color: var(--color-base-content);
    background-color: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-bottom-color: var(--color-base-200);
}

.create-modal-close {
    width: 28px;
    height: 28px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 6px;
    color: var(--color-swoosh-text-faint);
    background: none;
    border: none;
    cursor: pointer;
    transition: color 0.15s;
    margin-bottom: 4px;
    flex-shrink: 0;
}

.create-modal-close:hover {
    color: var(--color-swoosh-text-muted);
}

.create-modal-body {
    padding: 20px 20px 8px;
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.create-modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding: 12px 20px 18px;
}

.create-form-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.create-form-label {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--color-swoosh-text-faint);
}
</style>
