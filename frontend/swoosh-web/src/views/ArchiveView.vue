<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Archive, Menu, ChevronLeft, ChevronRight } from 'lucide-vue-next'
import api from '../api/client'
import type { Task } from '../types/task'

interface PagedResult {
    items: Task[]
    totalCount: number
    page: number
    pageSize: number
    totalPages: number
}

const tasks = ref<Task[]>([])
const loading = ref(false)
const currentPage = ref(1)
const totalCount = ref(0)
const totalPages = ref(1)
const PAGE_SIZE = 25

async function fetchPage(page: number) {
    loading.value = true
    try {
        const res = await api.get<PagedResult>('/tasks/archive', { params: { page, pageSize: PAGE_SIZE } })
        tasks.value = res.data.items
        totalCount.value = res.data.totalCount
        totalPages.value = res.data.totalPages
        currentPage.value = res.data.page
    } finally {
        loading.value = false
    }
}

function prevPage() {
    if (currentPage.value > 1) fetchPage(currentPage.value - 1)
}

function nextPage() {
    if (currentPage.value < totalPages.value) fetchPage(currentPage.value + 1)
}

function formatCompleted(iso: string | undefined): string {
    if (!iso) return ''
    const d = new Date(iso)
    return d.toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })
}

onMounted(() => fetchPage(1))
</script>

<template>
    <div class="archive-view">
        <div class="view-header">
            <div class="view-header-left">
                <label for="sidebar" class="page-menu-btn" aria-label="Open sidebar">
                    <Menu :size="15" :stroke-width="2" />
                </label>
                <Archive class="view-header-icon" />
                <h1 class="view-title">Archive</h1>
            </div>
            <span class="archive-count" v-if="totalCount">{{ totalCount }} task{{ totalCount !== 1 ? 's' : '' }}</span>
        </div>

        <p class="archive-hint">Completed tasks older than 30 days.</p>

        <div v-if="loading" class="view-empty">Loading...</div>

        <div v-else-if="!totalCount" class="view-empty">
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

            <div v-if="totalPages > 1" class="pagination">
                <button class="page-btn" :disabled="currentPage === 1" @click="prevPage" aria-label="Previous page">
                    <ChevronLeft :size="14" />
                </button>
                <span class="page-info">{{ currentPage }} / {{ totalPages }}</span>
                <button class="page-btn" :disabled="currentPage === totalPages" @click="nextPage" aria-label="Next page">
                    <ChevronRight :size="14" />
                </button>
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

.pagination {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    margin-top: 16px;
}

.page-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: var(--radius-r-sm);
    border: 1px solid var(--color-swoosh-border);
    background: var(--color-base-200);
    color: var(--color-swoosh-text-muted);
    cursor: pointer;
    transition: opacity 0.15s;
}

.page-btn:disabled {
    opacity: 0.3;
    cursor: default;
}

.page-btn:not(:disabled):hover {
    background: var(--color-base-300);
}

.page-info {
    font-family: var(--font-mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--color-swoosh-text-faint);
}
</style>
