<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRecurringStore } from '../stores/recurring'
import { useUiStore } from '../stores/ui'
import { Plus, CalendarSync, Trash2, RefreshCw, Clock, Menu } from 'lucide-vue-next'
import RecurringEdit from '../components/RecurringEdit.vue'
import TaskRating from '../components/TaskRating.vue'
import TaskIcon from '../components/TaskIcon.vue'
import type { RecurringTask } from '../types/recurring'

const store = useRecurringStore()
const ui = useUiStore()

const editingId = ref<string | null>(null)

function openEdit(id: string) {
    editingId.value = id
}

function cancelEdit() {
    editingId.value = null
}

async function toggleActive(id: string) {
    const item = store.items.find(r => r.id === id)
    if (!item) return
    await store.update(id, { ...item, isActive: !item.isActive })
}

async function confirmDelete(id: string) {
    if (confirm('Delete this recurring task?')) {
        await store.remove(id)
    }
}

function frequencyLabel(item: RecurringTask): string {
    const base = item.recurrenceInterval === 1
        ? `Every ${item.recurrenceType}`
        : `Every ${item.recurrenceInterval} ${item.recurrenceType}s`

    const parts: string[] = [base]

    // Weekly intervals (any multiple of weeks, or days divisible by 7) always
    // land on the same weekday — show it if we have a start date to derive it from.
    const alwaysSameWeekday =
        item.recurrenceType === 'week' ||
        (item.recurrenceType === 'day' && item.recurrenceInterval % 7 === 0)
    if (alwaysSameWeekday && item.recurrenceDate) {
        const [y, mo, d] = item.recurrenceDate.split('-').map(Number)
        const weekday = new Date(y, mo - 1, d).toLocaleDateString([], { weekday: 'long' })
        parts.push(weekday)
    }

    return parts.join(' · ')
}

function formattedTime(time: string): string {
    const [h, m] = time.split(':')
    const d = new Date()
    d.setHours(Number(h), Number(m))
    return d.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

const FREQUENCY_GROUPS = [
    { type: 'day',   label: 'Daily' },
    { type: 'week',  label: 'Weekly' },
    { type: 'month', label: 'Monthly' },
    { type: 'year',  label: 'Yearly' },
] as const

const groups = computed(() =>
    FREQUENCY_GROUPS
        .map(g => ({ ...g, items: store.items.filter(r => r.recurrenceType === g.type) }))
        .filter(g => g.items.length > 0)
)

onMounted(() => store.fetchAll())
</script>

<template>
    <div class="recurring-view">
        <div class="view-header">
            <div class="view-header-left">
                <label for="sidebar" class="page-menu-btn" aria-label="Open sidebar">
                    <Menu :size="15" :stroke-width="2" />
                </label>
                <CalendarSync class="view-header-icon" />
                <h1 class="view-title">Recurring</h1>
            </div>
            <button class="view-add-btn" @click="ui.triggerCreateModal('recurring')">
                <Plus class="w-4 h-4" /> New
            </button>
        </div>

        <!-- Skeleton loader -->
        <template v-if="store.loading">
            <p class="recurring-section-label skeleton-label"></p>
            <div class="recurring-list">
                <div v-for="i in 3" :key="i" class="recurring-skeleton" :style="{ '--delay': `-${(i - 1) * 150}ms` }">
                    <div class="flex-1 min-w-0 flex flex-col gap-2">
                        <div class="h-[14px] rounded-sm shimmer" :style="{ width: i === 1 ? '55%' : i === 2 ? '70%' : '45%' }"></div>
                        <div class="h-[12px] rounded-full shimmer w-[88px]"></div>
                    </div>
                    <div class="shimmer w-[32px] h-[32px] rounded-[6px] flex-shrink-0"></div>
                </div>
            </div>
        </template>

        <!-- Empty state -->
        <div v-else-if="!store.items.length" class="view-empty">
            <CalendarSync class="view-empty-icon" />
            <p>No recurring tasks yet.</p>
            <button class="btn btn-sm btn-ghost mt-2" @click="ui.triggerCreateModal('recurring')">Create one</button>
        </div>

        <!-- Task list grouped by frequency -->
        <template v-if="!store.loading && store.items.length">
            <template v-for="group in groups" :key="group.type">
                <p class="recurring-section-label">{{ group.label }}</p>
                <ul class="recurring-list">
                    <template v-for="item in group.items" :key="item.id">
                        <!-- Inline edit form -->
                        <li v-if="editingId === item.id" class="task-item recurring-edit-card">
                            <RecurringEdit
                                :task="item"
                                @close="cancelEdit"
                                @updated="cancelEdit"
                            />
                        </li>
                        <!-- Normal row -->
                        <li v-else class="task-item" :class="{ 'recurring-inactive': !item.isActive }">
                            <div class="task-main-row">
                                <div class="shrink-0 mt-0.5">
                                    <input
                                        type="checkbox"
                                        class="recurring-toggle"
                                        :checked="item.isActive"
                                        @change="toggleActive(item.id)"
                                        title="Toggle active"
                                    />
                                </div>
                                <div class="flex-1 min-w-0 cursor-text" @click="openEdit(item.id)">
                                    <div class="flex items-center justify-between gap-3">
                                        <span class="flex items-center gap-1.5 min-w-0">
                                            <span class="text-[15.5px] font-bold text-base-content leading-[1.45] break-words">{{ item.title }}</span>
                                            <TaskIcon v-if="item.icon != null" :value="item.icon" />
                                        </span>
                                        <TaskRating :rating="item.rating" :priority="item.priority" :pinned="item.pinned" />
                                    </div>
                                    <p v-if="item.notes" class="task-notes text-[13.5px] text-swoosh-text-muted mt-1 leading-[1.5] break-words">{{ item.notes }}</p>
                                    <div class="badges">
                                        <span class="badge">
                                            <RefreshCw :size="11" />
                                            {{ frequencyLabel(item) }}
                                        </span>
                                        <span v-if="item.recurrenceTime" class="badge">
                                            <Clock :size="11" />
                                            {{ formattedTime(item.recurrenceTime) }}
                                        </span>
                                    </div>
                                </div>
                                <div class="task-actions shrink-0">
                                    <button class="icon-action-btn danger" @click.stop="confirmDelete(item.id)" title="Delete">
                                        <Trash2 class="w-3.5 h-3.5" />
                                    </button>
                                </div>
                            </div>
                        </li>
                    </template>
                </ul>
            </template>
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

/* ── Section labels ── */
.recurring-section-label {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--color-swoosh-text-faint);
    margin-bottom: 6px;
    margin-top: 20px;
}

.recurring-section-label:first-of-type {
    margin-top: 0;
}

.skeleton-label {
    width: 48px;
    height: 12px;
    border-radius: 4px;
    background: linear-gradient(
        90deg,
        rgba(255,255,255,0.04) 25%,
        rgba(255,255,255,0.10) 50%,
        rgba(255,255,255,0.04) 75%
    );
    background-size: 200% 100%;
    animation: shimmer 2s linear infinite;
}

/* ── List container ── */
.recurring-list {
    list-style: none;
    background: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r);
    overflow: visible;
    margin-bottom: 8px;
}

.recurring-inactive {
    opacity: 0.5;
}

/* ── Toggle ── */
.recurring-toggle {
    appearance: none;
    cursor: pointer;
    flex-shrink: 0;
    width: 36px;
    height: 20px;
    border-radius: 9999px;
    border: 2px solid var(--color-swoosh-border-hover);
    background: transparent;
    position: relative;
    transition: background-color 0.2s, border-color 0.2s;
}

.recurring-toggle::before {
    content: '';
    position: absolute;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: var(--color-swoosh-border-hover);
    top: 50%;
    left: 2px;
    transform: translateY(-50%);
    transition: left 0.2s, background-color 0.2s;
}

.recurring-toggle:checked {
    border-color: var(--color-success);
    background: color-mix(in srgb, var(--color-success) 18%, transparent);
}

.recurring-toggle:checked::before {
    background: var(--color-success);
    left: calc(100% - 14px);
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

/* ── Skeleton rows ── */
.recurring-skeleton {
    display: flex;
    align-items: center;
    gap: 13px;
    padding-inline: 14px;
    padding-block: 16px;
    border-bottom: 1px solid var(--color-swoosh-border);
}

.recurring-skeleton:last-child {
    border-bottom: none;
}

/* ── Shimmer animation ── */
.shimmer {
    background: linear-gradient(
        90deg,
        rgba(255,255,255,0.04) 25%,
        rgba(255,255,255,0.10) 50%,
        rgba(255,255,255,0.04) 75%
    );
    background-size: 200% 100%;
    animation: shimmer 2s linear infinite;
    animation-delay: var(--delay, 0ms);
}

@keyframes shimmer {
    0%   { background-position:  200% 0; }
    100% { background-position: -200% 0; }
}

/* ── Empty state ── */
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

/* ── Large screen scaling for skeleton rows ── */
@media (min-width: 1280px) {
    .recurring-skeleton { padding-inline: 18px; padding-block: 18px; }
}

@media (min-width: 1536px) {
    .recurring-skeleton { padding-inline: 20px; padding-block: 20px; }
}
</style>
