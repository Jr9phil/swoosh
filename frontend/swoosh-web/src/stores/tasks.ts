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
            if (index !== -1 && this.tasks[index]) {
                this.tasks[index].completed = completedAt
            }
        },
        
        async togglePinned(task: Task) {
            const updated = {
                ...task,
                pinned : !task.pinned
            }
            
            await api.put(`/tasks/${task.id}`, updated)
            
            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1 && this.tasks[index]) {
                this.tasks[index].pinned = updated.pinned
            }
        },
        
        async resetCreationDate(task: Task) {
          const updated = {
              ...task,
              createdAt: new Date().toISOString()
          }
          await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1 && this.tasks[index]) {
                this.tasks[index].createdAt = updated.createdAt
            }
        },
        
        async resetDeadline(task: Task) {
            const updated = {
                ...task,
                deadline : null
            }
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1 && this.tasks[index]) {
                this.tasks[index].deadline = updated.deadline
            }
        },

        async updatePriority(task: Task, priority: number) {
            const updated = {
                ...task,
                priority
            }

            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1 && this.tasks[index]) {
                this.tasks[index].priority = priority
            }
        },

        async moveTaskRelative(source: Task, target: Task, before: boolean) {
            // Copy source and target
            const tasksCopy = [...this.tasks]

            // Get all tasks of the same priority and not completed/pinned
            const samePriorityTasks = tasksCopy.filter(
                t => t.priority === source.priority && !t.completed && !t.pinned
            )

            // Sort them by creation date descending
            samePriorityTasks.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

            // Find target index in the sorted same-priority list
            const targetIndex = samePriorityTasks.findIndex(t => t.id === target.id)
            if (targetIndex === -1) return

            let newCreatedAt: string

            if (before) {
                // Place source **before** target → make createdAt slightly later than target
                const nextTask = samePriorityTasks[targetIndex - 1] // task above target
                if (nextTask) {
                    // Average timestamps to fit in between
                    const avg = (new Date(nextTask.createdAt).getTime() + new Date(target.createdAt).getTime()) / 2
                    newCreatedAt = new Date(avg).toISOString()
                } else {
                    // No task above → just slightly newer than target
                    newCreatedAt = new Date(new Date(target.createdAt).getTime() + 1).toISOString()
                }
            } else {
                // Place source **after** target → slightly older than target
                const nextTask = samePriorityTasks[targetIndex + 1] // task below target
                if (nextTask) {
                    const avg = (new Date(target.createdAt).getTime() + new Date(nextTask.createdAt).getTime()) / 2
                    newCreatedAt = new Date(avg).toISOString()
                } else {
                    // No task below → slightly older than target
                    newCreatedAt = new Date(new Date(target.createdAt).getTime() - 1).toISOString()
                }
            }

            // Update backend
            const updated = { ...source, createdAt: newCreatedAt }
            await api.put(`/tasks/${source.id}`, updated)

            // Update local state
            const index = this.tasks.findIndex(t => t.id === source.id)
            if (index !== -1 && this.tasks[index]) this.tasks[index].createdAt = newCreatedAt
        },

        async editTask(
            taskId: string, 
            payload: { 
                title: string, 
                notes?: string | null, 
                pinned?: boolean, 
                deadline?: string | null,
                priority?: number,
            }
            ) {
            await api.put(`/tasks/${taskId}`, payload)
            
            const task = this.tasks.find(t => t.id === taskId)
            if (task) {
                task.title = payload.title
                task.notes = payload.notes ?? null
                
                if(payload.pinned !== undefined) task.pinned = payload.pinned
                if (payload.priority !== undefined) task.priority = payload.priority
                
                task.deadline = payload.deadline ?? null
            }
        },
        
        async deleteTask(taskId: string) {
            await api.delete(`/tasks/${taskId}`)
            this.tasks = this.tasks.filter(t => t.id !== taskId)
        }
    }
})
