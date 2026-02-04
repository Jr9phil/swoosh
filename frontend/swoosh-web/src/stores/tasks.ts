import { defineStore } from 'pinia'
import api from '../api/client'
import type { Task, CreateTask } from '../types/task'

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
        },
        async createTask(payload: CreateTask) {
            const res = await api.post('/tasks', payload)
            this.tasks.unshift(res.data)
        },
        async toggleComplete(task: Task) {
            const completedAt = task.completed 
                ? null
                : new Date().toISOString()

            const updated = {
                ...task,
                completed: completedAt
            }
            
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].completed = completedAt
            }
        },
        async editTask(taskId: string, payload: { title: string, notes?: string | null }) {
            await api.put(`/tasks/${taskId}`, payload)
            
            const task = this.tasks.find(t => t.id === taskId)
            if (task) {
                task.title = payload.title
                task.notes = payload.notes ?? null
            }
        },
        async deleteTask(taskId: string) {
            await api.delete(`/tasks/${taskId}`)
            this.tasks = this.tasks.filter(t => t.id !== taskId)
        }
    }
})
