import { defineStore } from 'pinia'
import api from '../api/client'
import type { Task } from '../types/task'

export const useTasksStore = defineStore('tasks', {
    state: () => ({
        tasks: [] as Task[],
        loading: false
    }),

    actions: {
        async fetchTasks() {
            this.loading = true
            try {
                const res = await api.get<Task[]>('/tasks')
                this.tasks = res.data
            } finally {
                this.loading = false
            }
        }
    }
})
