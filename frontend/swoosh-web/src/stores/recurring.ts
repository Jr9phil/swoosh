import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../api/client'
import type { RecurringTask, CreateRecurringTask } from '../types/recurring'

export const useRecurringStore = defineStore('recurring', () => {
    const items = ref<RecurringTask[]>([])
    const loading = ref(false)

    async function fetchAll() {
        loading.value = true
        try {
            const res = await api.get<RecurringTask[]>('/recurringtasks')
            items.value = res.data
        } finally {
            loading.value = false
        }
    }

    async function create(payload: CreateRecurringTask) {
        const res = await api.post<RecurringTask>('/recurringtasks', payload)
        items.value.unshift(res.data)
        return res.data
    }

    async function update(id: string, payload: CreateRecurringTask) {
        await api.put(`/recurringtasks/${id}`, payload)
        const idx = items.value.findIndex(r => r.id === id)
        if (idx !== -1) items.value[idx] = { ...items.value[idx], ...payload } as RecurringTask
    }

    async function remove(id: string) {
        await api.delete(`/recurringtasks/${id}`)
        items.value = items.value.filter(r => r.id !== id)
    }

    return { items, loading, fetchAll, create, update, remove }
})
