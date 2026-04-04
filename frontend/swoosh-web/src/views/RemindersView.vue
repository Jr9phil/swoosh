<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRemindersStore } from '../stores/reminders'
import { useUiStore } from '../stores/ui'
import { Plus, Trash2, BellRing, Check, Pencil } from 'lucide-vue-next'

const store = useRemindersStore()
const ui = useUiStore()

const editingId = ref<string | null>(null)
const editForm = ref<{ title: string; notes: string; remindAt: string }>({
    title: '',
    notes: '',
    remindAt: '',
})

function openEdit(id: string) {
    const item = store.items.find(r => r.id === id)
    if (!item) return
    editingId.value = id
    editForm.value = {
        title: item.title,
        notes: item.notes ?? '',
        remindAt: new Date(item.remindAt).toISOString().slice(0, 16),
    }
}

function cancelEdit() {
    editingId.value = null
}

async function submitEdit() {
    if (!editForm.value.title.trim() || !editingId.value) return
    await store.update(editingId.value, {
        title: editForm.value.title,
        notes: editForm.value.notes || null,
        remindAt: new Date(editForm.value.remindAt).toISOString(),
    })
    editingId.value = null
}

async function toggleComplete(id: string) {
    const item = store.items.find(r => r.id === id)
    if (!item) return
    await store.update(id, { isCompleted: !item.isCompleted })
}

async function confirmDelete(id: string) {
    if (confirm('Delete this reminder?')) {
        await store.remove(id)
    }
}

function formatRemindAt(iso: string): string {
    const d = new Date(iso)
    const now = new Date()
    const isToday = d.toDateString() === now.toDateString()
    const isTomorrow = d.toDateString() === new Date(now.getTime() + 86400000).toDateString()

    const time = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    if (isToday) return `Today at ${time}`
    if (isTomorrow) return `Tomorrow at ${time}`
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ` at ${time}`
}

function isPast(iso: string): boolean {
    return new Date(iso) < new Date()
}

const upcoming = computed(() =>
    store.items
        .filter(r => !r.isCompleted)
        .sort((a, b) => new Date(a.remindAt).getTime() - new Date(b.remindAt).getTime())
)

const completed = computed(() =>
    store.items
        .filter(r => r.isCompleted)
        .sort((a, b) => new Date(b.remindAt).getTime() - new Date(a.remindAt).getTime())
)

onMounted(() => store.fetchAll())
</script>

<template>
    <div class="reminders-view">
        <div class="view-header">
            <div class="view-header-left">
                <BellRing class="view-header-icon" />
                <h1 class="view-title">Reminders</h1>
            </div>
            <button class="btn btn-sm btn-ghost view-add-btn" @click="ui.triggerCreateModal('reminder')">
                <Plus class="w-4 h-4" /> New
            </button>
        </div>

        <!-- Loading -->
        <div v-if="store.loading" class="view-empty">Loading...</div>

        <!-- Empty state -->
        <div v-else-if="!store.items.length" class="view-empty">
            <BellRing class="view-empty-icon" />
            <p>No reminders yet.</p>
            <button class="btn btn-sm btn-ghost mt-2" @click="ui.triggerCreateModal('reminder')">Create one</button>
        </div>

        <!-- Upcoming -->
        <template v-if="upcoming.length">
            <p class="section-label">Upcoming</p>
            <div class="reminder-list">
                <template v-for="item in upcoming" :key="item.id">
                    <!-- Inline edit -->
                    <div v-if="editingId === item.id" class="reminder-edit-card">
                        <input v-model="editForm.title" class="input input-sm w-full" maxlength="200" @keydown.escape="cancelEdit" autofocus />
                        <input v-model="editForm.remindAt" type="datetime-local" class="input input-sm w-full" />
                        <textarea v-model="editForm.notes" class="textarea textarea-sm w-full resize-none" rows="2" maxlength="1000" />
                        <div class="form-actions">
                            <button class="btn btn-xs btn-ghost" @click="cancelEdit">Cancel</button>
                            <button class="btn btn-xs btn-primary" @click="submitEdit" :disabled="!editForm.title.trim()">Save</button>
                        </div>
                    </div>
                    <!-- Normal row -->
                    <div
                        v-else
                        class="reminder-item"
                        :class="{ 'is-overdue': isPast(item.remindAt) }"
                    >
                        <button class="reminder-check" @click="toggleComplete(item.id)" title="Mark done">
                            <Check class="w-3 h-3" />
                        </button>
                        <div class="reminder-main">
                            <span class="reminder-title">{{ item.title }}</span>
                            <span class="reminder-time" :class="{ 'overdue-text': isPast(item.remindAt) }">
                                {{ formatRemindAt(item.remindAt) }}
                            </span>
                            <p v-if="item.notes" class="reminder-notes">{{ item.notes }}</p>
                        </div>
                        <button class="icon-action-btn" @click="openEdit(item.id)" title="Edit">
                            <Pencil class="w-3.5 h-3.5" />
                        </button>
                        <button class="icon-action-btn danger" @click="confirmDelete(item.id)" title="Delete">
                            <Trash2 class="w-3.5 h-3.5" />
                        </button>
                    </div>
                </template>
            </div>
        </template>

        <!-- Completed -->
        <template v-if="completed.length">
            <p class="section-label">Done</p>
            <div class="reminder-list">
                <div
                    v-for="item in completed"
                    :key="item.id"
                    class="reminder-item is-completed"
                >
                    <button class="reminder-check is-done" @click="toggleComplete(item.id)" title="Unmark">
                        <Check class="w-3 h-3" />
                    </button>
                    <div class="reminder-main">
                        <span class="reminder-title">{{ item.title }}</span>
                        <span class="reminder-time">{{ formatRemindAt(item.remindAt) }}</span>
                    </div>
                    <button class="icon-action-btn danger" @click="confirmDelete(item.id)" title="Delete">
                        <Trash2 class="w-3.5 h-3.5" />
                    </button>
                </div>
            </div>
        </template>
    </div>
</template>

<style scoped>
.reminders-view {
    max-width: 680px;
    margin: 0 auto;
    padding: 32px 20px;
}

.view-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 28px;
}

.view-header-left {
    display: flex;
    align-items: center;
    gap: 10px;
}

.view-header-icon {
    width: 20px;
    height: 20px;
    color: var(--color-swoosh-text-muted);
}

.view-title {
    font-family: var(--font-mono);
    font-size: 13px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.12em;
    color: var(--color-swoosh-text-muted);
}

.view-add-btn {
    gap: 6px;
    font-family: var(--font-mono);
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.08em;
}

.reminder-edit-card {
    background: var(--color-base-300);
    border: 1px solid var(--color-swoosh-border-hover);
    border-radius: var(--radius-r-sm);
    padding: 12px 14px;
    margin-bottom: 4px;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.form-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.form-label {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--color-swoosh-text-faint);
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding-top: 4px;
}

.section-label {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--color-swoosh-text-faint);
    margin-bottom: 8px;
    margin-top: 20px;
}

.section-label:first-of-type { margin-top: 0; }

.reminder-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-bottom: 8px;
}

.reminder-item {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    background: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r-sm);
    padding: 12px 14px;
    transition: background-color 0.15s, border-color 0.15s;
}

.reminder-item:hover {
    background: var(--color-base-300);
}

.reminder-item.is-overdue {
    border-color: color-mix(in srgb, var(--color-warning) 30%, transparent);
}

.reminder-item.is-completed {
    opacity: 0.45;
}

.reminder-check {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    border: 1.5px solid var(--color-swoosh-border);
    background: none;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    margin-top: 1px;
    color: transparent;
    transition: border-color 0.15s, background-color 0.15s, color 0.15s;
}

.reminder-check:hover {
    border-color: var(--color-success);
    color: var(--color-success);
}

.reminder-check.is-done {
    background: var(--color-success);
    border-color: var(--color-success);
    color: #fff;
}

.reminder-main {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 2px;
    cursor: pointer;
}

.reminder-title {
    font-size: 0.875rem;
    color: var(--color-base-content);
    font-weight: 500;
}

.is-completed .reminder-title {
    text-decoration: line-through;
}

.reminder-time {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--color-swoosh-text-faint);
}

.overdue-text {
    color: var(--color-warning);
}

.reminder-notes {
    font-size: 0.75rem;
    color: var(--color-swoosh-text-muted);
    margin-top: 2px;
}

.icon-action-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 26px;
    border-radius: var(--radius-r-sm);
    color: var(--color-swoosh-text-faint);
    transition: background-color 0.15s, color 0.15s;
    cursor: pointer;
    border: none;
    background: none;
    flex-shrink: 0;
    margin-top: 1px;
}

.icon-action-btn:hover {
    background: var(--color-base-300);
    color: var(--color-base-content);
}

.icon-action-btn.danger:hover {
    background: color-mix(in srgb, var(--color-error) 15%, transparent);
    color: var(--color-error);
}

.view-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 64px 0;
    color: var(--color-swoosh-text-faint);
    font-size: 0.875rem;
    text-align: center;
}

.view-empty-icon {
    width: 32px;
    height: 32px;
    opacity: 0.3;
}
</style>
