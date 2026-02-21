/**
 * tasks.ts
 * Pinia store for managing task state and operations.
 * Handles fetching, creating, updating, and deleting tasks, as well as reordering and priority management.
 */
import { defineStore } from 'pinia'
import api from '../api/client'
import type { Task, CreateTask } from '../types/task'

export const useTasksStore = defineStore('tasks', {
    // Store state: maintains the list of tasks and a loading indicator
    state: () => ({
        tasks: [] as Task[],
        loading: false
    }),

    actions: {
        // Fetches all tasks from the server and populates the store
        async fetchTasks() {
            this.loading = true
            try {
                const res = await api.get<Task[]>('/tasks')
                this.tasks = res.data
            } finally {
                this.loading = false
            }
        },
        
        // Creates a new task and adds it to the top of the local list
        async createTask(payload: CreateTask) {
            const res = await api.post('/tasks', payload)
            this.tasks.unshift(res.data)
        },
        
        // Toggles the completion status of a task and updates the backend
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
        
        // Toggles whether a task is pinned (sticky) in the list
        async togglePinned(task: Task) {
            const updated = {
                ...task,
                pinned : !task.pinned
            }
            
            await api.put(`/tasks/${task.id}`, updated)
            
            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].pinned = updated.pinned
            }
        },
        
        // Resets the creation date to now, effectively moving it to the top of the creation-sorted list
        async resetCreationDate(task: Task) {
          const updated = {
              ...task,
              createdAt: new Date().toISOString()
          }
          await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].createdAt = updated.createdAt
            }
        },
        
        // Removes the deadline from a task
        async resetDeadline(task: Task) {
            const updated = {
                ...task,
                deadline : null
            }
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].deadline = updated.deadline
            }
        },

        // Updates the priority level of a task
        async updatePriority(task: Task, priority: number) {
            const updated = {
                ...task,
                priority
            }

            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].priority = priority
            }
        },

        // Moves a task relative to another task by adjusting its creation timestamp
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
            if (index !== -1) this.tasks[index].createdAt = newCreatedAt
        },

        // Edits multiple task fields at once (title, notes, pinned, deadline, priority)
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
        
        // Removes a task from the backend and local store
        async deleteTask(taskId: string) {
            await api.delete(`/tasks/${taskId}`)
            this.tasks = this.tasks.filter(t => t.id !== taskId)
        }
    }
})
