<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Archive } from 'lucide-vue-next'
import api from '../api/client'
import type { Task } from '../types/task'

const tasks = ref<Task[]>([])
const loading = ref(false)

function formatCompleted(iso: string | undefined): string {
    if (!iso) return ''
    const d = new Date(iso)
    return d.toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })
}

onMounted(async () => {
    loading.value = true
    try {
        const res = await api.get<Task[]>('/tasks/archive')
        tasks.value = res.data
    } finally {
        loading.value = false
    }
})
</script>

<template>
    <div class="archive-view">
        <div class="view-header">
            <div class="view-header-left">
                <Archive class="view-header-icon" />
                <h1 class="view-title">Archive</h1>
            </div>
            <span class="archive-count" v-if="tasks.length">{{ tasks.length }} task{{ tasks.length !== 1 ? 's' : '' }}</span>
        </div>

        <p class="archive-hint">Completed tasks older than 30 days.</p>

        <div v-if="loading" class="view-empty">Loading...</div>

        <div v-else-if="!tasks.length" class="view-empty">
            <Archive class="view-empty-icon" />
            <p>Nothing here yet.</p>
        </div>

        <div v-else class="archive-list">
            <div v-for="task in tasks" :key="task.id" class="archive-item">
                <div class="archive-item-main">
                    <span class="archive-item-title">{{ task.title }}</span>
                    <p v-if="task.notes" class="archive-item-notes">{{ task.notes }}</p>
                </div>
                <span class="archive-item-date">{{ formatCompleted(task.completed?.toString()) }}</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.archive-view {
    max-width: 680px;
    margin: 0 auto;
    padding: 32px 20px;
}

.view-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
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

.archive-count {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--color-swoosh-text-faint);
}

.archive-hint {
    font-size: 0.8125rem;
    color: var(--color-swoosh-text-faint);
    margin-bottom: 24px;
}

.archive-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.archive-item {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    background: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r-sm);
    padding: 12px 14px;
    opacity: 0.7;
}

.archive-item-main {
    flex: 1;
    min-width: 0;
}

.archive-item-title {
    font-size: 0.875rem;
    color: var(--color-swoosh-text-muted);
    text-decoration: line-through;
    font-weight: 500;
}

.archive-item-notes {
    font-size: 0.75rem;
    color: var(--color-swoosh-text-faint);
    margin-top: 2px;
}

.archive-item-date {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--color-swoosh-text-faint);
    white-space: nowrap;
    flex-shrink: 0;
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
