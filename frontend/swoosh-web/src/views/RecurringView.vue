<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRecurringStore } from '../stores/recurring'
import { useUiStore } from '../stores/ui'
import { Plus, Pencil, Trash2, CalendarSync, ToggleLeft, ToggleRight } from 'lucide-vue-next'
import type { RecurrenceType, CreateRecurringTask } from '../types/recurring'

const store = useRecurringStore()
const ui = useUiStore()

const editingId = ref<string | null>(null)
const editForm = ref<CreateRecurringTask>({
    title: '',
    notes: null,
    recurrenceType: 'daily',
    recurrenceInterval: null,
    isActive: true,
})

const recurrenceLabels: Record<RecurrenceType, string> = {
    daily: 'Every day',
    interval: 'Every X days',
    weekly: 'Every week',
    monthly: 'Every month',
    custom: 'Custom',
}

function recurrenceDescription(type: RecurrenceType, interval: number | null): string {
    if (type === 'interval' && interval) return `Every ${interval} day${interval !== 1 ? 's' : ''}`
    return recurrenceLabels[type]
}

function openEdit(id: string) {
    const item = store.items.find(r => r.id === id)
    if (!item) return
    editingId.value = id
    editForm.value = {
        title: item.title,
        notes: item.notes,
        recurrenceType: item.recurrenceType,
        recurrenceInterval: item.recurrenceInterval,
        isActive: item.isActive,
    }
}

function cancelEdit() {
    editingId.value = null
}

async function submitEdit() {
    if (!editForm.value.title.trim() || !editingId.value) return
    await store.update(editingId.value, { ...editForm.value })
    editingId.value = null
}

async function toggleActive(id: string) {
    const item = store.items.find(r => r.id === id)
    if (!item) return
    await store.update(id, {
        title: item.title,
        notes: item.notes,
        recurrenceType: item.recurrenceType,
        recurrenceInterval: item.recurrenceInterval,
        isActive: !item.isActive,
    })
}

async function confirmDelete(id: string) {
    if (confirm('Delete this recurring task?')) {
        await store.remove(id)
    }
}

const active = computed(() => store.items.filter(r => r.isActive))
const inactive = computed(() => store.items.filter(r => !r.isActive))

onMounted(() => store.fetchAll())
</script>

<template>
    <div class="recurring-view">
        <div class="view-header">
            <div class="view-header-left">
                <CalendarSync class="view-header-icon" />
                <h1 class="view-title">Recurring</h1>
            </div>
            <button class="btn btn-sm btn-ghost view-add-btn" @click="ui.triggerCreateModal('recurring')">
                <Plus class="w-4 h-4" /> New
            </button>
        </div>

        <!-- Loading -->
        <div v-if="store.loading" class="view-empty">Loading...</div>

        <!-- Empty state -->
        <div v-else-if="!store.items.length" class="view-empty">
            <CalendarSync class="view-empty-icon" />
            <p>No recurring tasks yet.</p>
            <button class="btn btn-sm btn-ghost mt-2" @click="ui.triggerCreateModal('recurring')">Create one</button>
        </div>

        <!-- Active -->
        <template v-if="active.length">
            <p class="recurring-section-label">Active</p>
            <div class="recurring-list">
                <template v-for="item in active" :key="item.id">
                    <!-- Inline edit form -->
                    <div v-if="editingId === item.id" class="recurring-edit-card">
                        <div class="form-field">
                            <input v-model="editForm.title" class="input input-sm w-full" maxlength="200" @keydown.escape="cancelEdit" autofocus />
                        </div>
                        <div class="form-field">
                            <select v-model="editForm.recurrenceType" class="select select-sm w-full">
                                <option value="daily">Every day</option>
                                <option value="interval">Every X days</option>
                                <option value="weekly">Every week</option>
                                <option value="monthly">Every month</option>
                                <option value="custom">Custom</option>
                            </select>
                        </div>
                        <div v-if="editForm.recurrenceType === 'interval'" class="form-field">
                            <input v-model.number="editForm.recurrenceInterval" type="number" min="1" max="365" class="input input-sm w-full" placeholder="Days" />
                        </div>
                        <div class="form-actions">
                            <button class="btn btn-xs btn-ghost" @click="cancelEdit">Cancel</button>
                            <button class="btn btn-xs btn-primary" @click="submitEdit" :disabled="!editForm.title.trim()">Save</button>
                        </div>
                    </div>
                    <!-- Normal row -->
                    <div v-else class="recurring-item">
                        <div class="recurring-item-main">
                            <span class="recurring-item-title">{{ item.title }}</span>
                            <span class="recurring-item-rate">{{ recurrenceDescription(item.recurrenceType, item.recurrenceInterval) }}</span>
                            <p v-if="item.notes" class="recurring-item-notes">{{ item.notes }}</p>
                        </div>
                        <div class="recurring-item-actions">
                            <button class="icon-action-btn" @click="toggleActive(item.id)" title="Pause">
                                <ToggleRight class="w-4 h-4 text-success" />
                            </button>
                            <button class="icon-action-btn" @click="openEdit(item.id)" title="Edit">
                                <Pencil class="w-3.5 h-3.5" />
                            </button>
                            <button class="icon-action-btn danger" @click="confirmDelete(item.id)" title="Delete">
                                <Trash2 class="w-3.5 h-3.5" />
                            </button>
                        </div>
                    </div>
                </template>
            </div>
        </template>

        <!-- Inactive -->
        <template v-if="inactive.length">
            <p class="recurring-section-label">Paused</p>
            <div class="recurring-list">
                <div v-for="item in inactive" :key="item.id" class="recurring-item inactive">
                    <div class="recurring-item-main">
                        <span class="recurring-item-title">{{ item.title }}</span>
                        <span class="recurring-item-rate">{{ recurrenceDescription(item.recurrenceType, item.recurrenceInterval) }}</span>
                    </div>
                    <div class="recurring-item-actions">
                        <button class="icon-action-btn" @click="toggleActive(item.id)" title="Resume">
                            <ToggleLeft class="w-4 h-4" />
                        </button>
                        <button class="icon-action-btn" @click="openEdit(item.id)" title="Edit">
                            <Pencil class="w-3.5 h-3.5" />
                        </button>
                        <button class="icon-action-btn danger" @click="confirmDelete(item.id)" title="Delete">
                            <Trash2 class="w-3.5 h-3.5" />
                        </button>
                    </div>
                </div>

            </div>
        </template>
    </div>
</template>

<style scoped>
.recurring-view {
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

.recurring-edit-card {
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

.recurring-section-label {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--color-swoosh-text-faint);
    margin-bottom: 8px;
    margin-top: 20px;
}

.recurring-section-label:first-of-type {
    margin-top: 0;
}

.recurring-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-bottom: 8px;
}

.recurring-item {
    display: flex;
    align-items: center;
    gap: 12px;
    background: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r-sm);
    padding: 12px 14px;
    transition: background-color 0.15s;
}

.recurring-item:hover {
    background: var(--color-base-300);
}

.recurring-item.inactive {
    opacity: 0.5;
}

.recurring-item-main {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.recurring-item-title {
    font-size: 0.875rem;
    color: var(--color-base-content);
    font-weight: 500;
}

.recurring-item-rate {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--color-secondary);
}

.recurring-item-notes {
    font-size: 0.75rem;
    color: var(--color-swoosh-text-muted);
    margin-top: 2px;
}

.recurring-item-actions {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-shrink: 0;
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
