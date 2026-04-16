<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useNoteCardsStore } from '../stores/notecards'
import { useTasksStore } from '../stores/tasks'
import { useUiStore } from '../stores/ui'
import { Plus, Trash2, X, Check, ArrowUpRight, Lightbulb, Menu } from 'lucide-vue-next'
import type { NoteCard } from '../types/notecard'

const store = useNoteCardsStore()
const tasksStore = useTasksStore()
const ui = useUiStore()

// Drag state
const dragging = ref<string | null>(null)
const dragOffset = ref({ x: 0, y: 0 })

// Editing state
const editingId = ref<string | null>(null)
const editTitle = ref('')
const editBody = ref('')


function startDrag(e: MouseEvent | TouchEvent, card: NoteCard) {
    if (editingId.value === card.id) return
    dragging.value = card.id
    const touch = 'touches' in e ? e.touches[0] : null
    const clientX = touch ? touch.clientX : (e as MouseEvent).clientX
    const clientY = touch ? touch.clientY : (e as MouseEvent).clientY
    dragOffset.value = { x: clientX - card.positionX, y: clientY - card.positionY }
}

function onMove(e: MouseEvent | TouchEvent) {
    if (!dragging.value) return
    const touch = 'touches' in e ? e.touches[0] : null
    const clientX = touch ? touch.clientX : (e as MouseEvent).clientX
    const clientY = touch ? touch.clientY : (e as MouseEvent).clientY
    const card = store.items.find(c => c.id === dragging.value)
    if (!card) return
    card.positionX = Math.max(0, clientX - dragOffset.value.x)
    card.positionY = Math.max(0, clientY - dragOffset.value.y)
}

function endDrag() {
    if (!dragging.value) return
    const card = store.items.find(c => c.id === dragging.value)
    if (card) {
        store.update(card.id, { positionX: card.positionX, positionY: card.positionY })
    }
    dragging.value = null
}

function openEdit(card: NoteCard) {
    editingId.value = card.id
    editTitle.value = card.title
    editBody.value = card.body ?? ''
}

async function saveEdit(id: string) {
    if (!editTitle.value.trim()) return
    await store.update(id, { title: editTitle.value, body: editBody.value || null })
    editingId.value = null
}

function cancelEdit() {
    editingId.value = null
}

async function deleteCard(id: string) {
    await store.remove(id)
}

async function convertToTask(card: NoteCard) {
    await tasksStore.createTask({
        title: card.title,
        notes: card.body ?? undefined,
        priority: 0,
        pinned: false,
        rating: 0,
    })
    await store.remove(card.id)
}


onMounted(() => store.fetchAll())
</script>

<template>
    <div
        class="noteboard-root"
        @mousemove="onMove"
        @mouseup="endDrag"
        @touchmove.prevent="onMove"
        @touchend="endDrag"
    >
        <!-- Toolbar -->
        <div class="noteboard-toolbar">
            <div class="view-header-left">
                <label for="sidebar" class="page-menu-btn" aria-label="Open sidebar">
                    <Menu :size="15" :stroke-width="2" />
                </label>
                <Lightbulb class="w-5 h-5 text-swoosh-text-muted" style="color: var(--color-swoosh-text-muted)" />
                <span class="view-title">Noteboard</span>
            </div>
            <button class="view-add-btn" @click="ui.triggerCreateModal('notecard')">
                <Plus class="w-4 h-4" /> New card
            </button>
        </div>

        <!-- Loading -->
        <div v-if="store.loading" class="noteboard-empty">Loading...</div>

        <!-- Empty state -->
        <div v-else-if="!store.items.length" class="noteboard-empty">
            <Lightbulb class="w-8 h-8 opacity-30" />
            <p>No note cards yet.</p>
            <button class="btn btn-sm btn-ghost mt-2" @click="ui.triggerCreateModal('notecard')">Create one</button>
        </div>

        <!-- Canvas -->
        <div v-else class="noteboard-canvas">
            <div
                v-for="card in store.items"
                :key="card.id"
                class="note-card"
                :class="{ 'is-dragging': dragging === card.id }"
                :style="{ left: card.positionX + 'px', top: card.positionY + 'px' }"
                @mousedown="startDrag($event, card)"
                @touchstart.prevent="startDrag($event, card)"
            >
                <template v-if="editingId === card.id">
                    <input
                        v-model="editTitle"
                        class="note-card-title-input"
                        maxlength="200"
                        @keydown.escape="cancelEdit"
                        @click.stop
                    />
                    <textarea
                        v-model="editBody"
                        class="note-card-body-input"
                        rows="5"
                        maxlength="4000"
                        @click.stop
                    />
                    <div class="note-card-edit-actions">
                        <button class="icon-action-btn" @click.stop="cancelEdit" title="Cancel">
                            <X class="w-3.5 h-3.5" />
                        </button>
                        <button class="icon-action-btn success" @click.stop="saveEdit(card.id)" title="Save">
                            <Check class="w-3.5 h-3.5" />
                        </button>
                    </div>
                </template>
                <template v-else>
                    <div class="note-card-header">
                        <span class="note-card-title" @dblclick.stop="openEdit(card)">{{ card.title }}</span>
                        <div class="note-card-actions">
                            <button class="icon-action-btn" @click.stop="convertToTask(card)" title="Convert to task">
                                <ArrowUpRight class="w-3.5 h-3.5" />
                            </button>
                            <button class="icon-action-btn danger" @click.stop="deleteCard(card.id)" title="Delete">
                                <Trash2 class="w-3.5 h-3.5" />
                            </button>
                        </div>
                    </div>
                    <p v-if="card.body" class="note-card-body" @dblclick.stop="openEdit(card)">{{ card.body }}</p>
                    <p v-else class="note-card-empty-body" @dblclick.stop="openEdit(card)">Double-click to add a note...</p>
                </template>
            </div>
        </div>
    </div>
</template>

<style scoped>
.noteboard-root {
    position: relative;
    width: 100%;
    height: 100vh;
    overflow: hidden;
    display: flex;
    flex-direction: column;
}

.noteboard-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 24px;
    border-bottom: 1px solid var(--color-swoosh-border);
    background: var(--color-base-100);
    flex-shrink: 0;
    z-index: 10;
}

.view-header-left {
    display: flex;
    align-items: center;
    gap: 10px;
}

.view-title {
    font-family: var(--font-mono);
    font-size: 13px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.12em;
    color: var(--color-swoosh-text-muted);
}


.noteboard-canvas {
    position: relative;
    flex: 1;
    overflow: auto;
    min-height: 1000px;
    min-width: 1200px;
    background-image: radial-gradient(circle, rgba(255,255,255,0.04) 1px, transparent 1px);
    background-size: 28px 28px;
}

.noteboard-empty {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    color: var(--color-swoosh-text-faint);
    font-size: 0.875rem;
}

.form-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
}

/* Note card */
.note-card {
    position: absolute;
    width: 220px;
    background: var(--color-base-200);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r);
    padding: 14px;
    cursor: grab;
    user-select: none;
    box-shadow: 0 4px 16px rgba(0,0,0,0.3);
    transition: box-shadow 0.15s, border-color 0.15s;
}

.note-card:hover {
    border-color: var(--color-swoosh-border-hover);
    box-shadow: 0 6px 24px rgba(0,0,0,0.4);
}

.note-card.is-dragging {
    cursor: grabbing;
    border-color: var(--color-secondary);
    box-shadow: 0 8px 32px rgba(0,0,0,0.5);
    z-index: 100;
}

.note-card-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 8px;
}

.note-card-title {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-base-content);
    line-height: 1.3;
    flex: 1;
    min-width: 0;
}

.note-card-actions {
    display: flex;
    gap: 4px;
    flex-shrink: 0;
    opacity: 0;
    transition: opacity 0.15s;
}

.note-card:hover .note-card-actions {
    opacity: 1;
}

.note-card-body {
    font-size: 0.8125rem;
    color: var(--color-swoosh-text-muted);
    line-height: 1.5;
    white-space: pre-wrap;
    word-break: break-word;
}

.note-card-empty-body {
    font-size: 0.75rem;
    color: var(--color-swoosh-text-faint);
    font-style: italic;
}

.note-card-title-input {
    width: 100%;
    background: var(--color-base-300);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r-sm);
    padding: 4px 8px;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-base-content);
    margin-bottom: 8px;
    outline: none;
}

.note-card-title-input:focus {
    border-color: var(--color-swoosh-border-hover);
}

.note-card-body-input {
    width: 100%;
    background: var(--color-base-300);
    border: 1px solid var(--color-swoosh-border);
    border-radius: var(--radius-r-sm);
    padding: 6px 8px;
    font-size: 0.8125rem;
    color: var(--color-swoosh-text-muted);
    resize: vertical;
    outline: none;
    font-family: var(--font-sans);
}

.note-card-body-input:focus {
    border-color: var(--color-swoosh-border-hover);
}

.note-card-edit-actions {
    display: flex;
    justify-content: flex-end;
    gap: 4px;
    margin-top: 8px;
}

.icon-action-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
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

.icon-action-btn.success:hover {
    background: color-mix(in srgb, var(--color-success) 15%, transparent);
    color: var(--color-success);
}
</style>
