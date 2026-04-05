import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../api/client'
import type { NoteCard, CreateNoteCard } from '../types/notecard'

export const useNoteCardsStore = defineStore('notecards', () => {
    const items = ref<NoteCard[]>([])
    const loading = ref(false)

    async function fetchAll() {
        loading.value = true
        try {
            const res = await api.get<NoteCard[]>('/notecards')
            items.value = res.data
        } finally {
            loading.value = false
        }
    }

    async function create(payload: CreateNoteCard) {
        const res = await api.post<NoteCard>('/notecards', payload)
        items.value.unshift(res.data)
        return res.data
    }

    async function update(id: string, payload: Partial<NoteCard>) {
        const card = items.value.find(c => c.id === id)
        if (!card) return
        const merged = { ...card, ...payload }
        await api.put(`/notecards/${id}`, merged)
        const idx = items.value.findIndex(c => c.id === id)
        if (idx !== -1) items.value[idx] = { ...items.value[idx], ...payload } as NoteCard
    }

    async function remove(id: string) {
        await api.delete(`/notecards/${id}`)
        items.value = items.value.filter(c => c.id !== id)
    }

    return { items, loading, fetchAll, create, update, remove }
})
