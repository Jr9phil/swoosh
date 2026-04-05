<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useUiStore } from '../stores/ui'
import { useNoteCardsStore } from '../stores/notecards'
import { useRemindersStore } from '../stores/reminders'
import TaskEdit from './TaskEdit.vue'
import RecurringEdit from './RecurringEdit.vue'
import { X } from 'lucide-vue-next'
import type { CreateModalTab } from '../stores/ui'

const ui = useUiStore()
const noteCardsStore = useNoteCardsStore()
const remindersStore = useRemindersStore()

const dialogEl = ref<HTMLDialogElement | null>(null)
const taskEditRef = ref<InstanceType<typeof TaskEdit> | null>(null)
const recurringEditRef = ref<InstanceType<typeof RecurringEdit> | null>(null)

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
    recurringEditRef.value?.resetForm()
    ui.resetModal()
}

function resetForms() {
    taskEditRef.value?.resetForm()
    recurringEditRef.value?.resetForm()
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
                <RecurringEdit ref="recurringEditRef" @close="close" @created="close" />
            </template>

            <!-- NoteCard tab -->
            <template v-else-if="ui.activeTab === 'notecard'">
                <div class="px-5 pt-[18px] pb-0 flex flex-col gap-[13px] min-h-[260px]">
                    <input
                        v-model="nTitle"
                        class="task-edit-input rounded-sm w-full text-base-content font-bold py-[10px] px-[13px] text-[18px]"
                        placeholder="Note title"
                        maxlength="200"
                        autofocus
                        @keydown.escape="close"
                    />
                    <textarea
                        v-model="nBody"
                        class="task-edit-input rounded-sm w-full text-swoosh-text-muted resize-none leading-relaxed py-[10px] px-[13px] text-[14.5px] flex-1 min-h-[140px]"
                        placeholder="What's on your mind?"
                        maxlength="4000"
                    />
                </div>
                <div class="flex items-center justify-end gap-2 border-t border-swoosh mt-[13px] px-5 pt-3 pb-[18px]">
                    <button class="rounded-sm border border-swoosh text-swoosh-text-faint text-[14px] transition-colors hover:text-swoosh-text-muted hover:border-swoosh-border-hover px-[14px] py-[7px]" @click="close">Cancel</button>
                    <button class="rounded-sm border border-swoosh-text-muted bg-transparent text-base-content text-[14px] font-medium transition-colors hover:bg-base-300 px-[18px] py-[7px] disabled:opacity-40" :disabled="!nTitle.trim()" @click="submitNoteCard">
                        Create
                    </button>
                </div>
            </template>

            <!-- Reminder tab -->
            <template v-else-if="ui.activeTab === 'reminder'">
                <div class="px-5 pt-[18px] pb-0 flex flex-col gap-[13px] min-h-[260px]">
                    <input
                        v-model="remTitle"
                        class="task-edit-input rounded-sm w-full text-base-content font-bold py-[10px] px-[13px] text-[18px]"
                        placeholder="Remind me to..."
                        maxlength="200"
                        autofocus
                        @keydown.escape="close"
                    />
                    <div>
                        <div class="font-bold font-mono uppercase text-swoosh-text-faint text-[11px] tracking-[0.10em] mb-1.5">When</div>
                        <div class="flex rounded-sm overflow-hidden border border-swoosh bg-base-100 transition-colors focus-within:border-swoosh-border-hover focus-within:bg-base-200">
                            <input
                                v-model="remAt"
                                type="datetime-local"
                                class="flex-1 bg-transparent text-base-content font-mono outline-none py-[10px] px-[13px] text-[14px]"
                            />
                        </div>
                    </div>
                    <textarea
                        v-model="remNotes"
                        class="task-edit-input rounded-sm w-full text-swoosh-text-muted resize-none leading-relaxed py-[10px] px-[13px] text-[14.5px] min-h-[80px]"
                        placeholder="Notes (optional)"
                        maxlength="1000"
                    />
                </div>
                <div class="flex items-center justify-end gap-2 border-t border-swoosh mt-[13px] px-5 pt-3 pb-[18px]">
                    <button class="rounded-sm border border-swoosh text-swoosh-text-faint text-[14px] transition-colors hover:text-swoosh-text-muted hover:border-swoosh-border-hover px-[14px] py-[7px]" @click="close">Cancel</button>
                    <button class="rounded-sm border border-swoosh-text-muted bg-transparent text-base-content text-[14px] font-medium transition-colors hover:bg-base-300 px-[18px] py-[7px] disabled:opacity-40" :disabled="!remTitle.trim() || !remAt" @click="submitReminder">
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

</style>
