import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../api/client'
import type { Reminder, CreateReminder } from '../types/reminder'

export const useRemindersStore = defineStore('reminders', () => {
    const items = ref<Reminder[]>([])
    const loading = ref(false)

    async function fetchAll() {
        loading.value = true
        try {
            const res = await api.get<Reminder[]>('/api/reminders')
            items.value = res.data
        } finally {
            loading.value = false
        }
    }

    async function create(payload: CreateReminder) {
        const res = await api.post<Reminder>('/api/reminders', payload)
        items.value.unshift(res.data)
        return res.data
    }

    async function update(id: string, payload: Partial<Reminder>) {
        const reminder = items.value.find(r => r.id === id)
        if (!reminder) return
        const merged = { ...reminder, ...payload }
        await api.put(`/api/reminders/${id}`, merged)
        const idx = items.value.findIndex(r => r.id === id)
        if (idx !== -1) items.value[idx] = { ...items.value[idx], ...payload } as Reminder
    }

    async function remove(id: string) {
        await api.delete(`/api/reminders/${id}`)
        items.value = items.value.filter(r => r.id !== id)
    }

    return { items, loading, fetchAll, create, update, remove }
})
