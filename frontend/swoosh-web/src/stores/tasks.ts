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

        // Creates a new subtask under the given parent task
        async createSubtask(parentTaskId: string, payload: { title: string, notes?: string | null, deadline?: string | null }) {
            const res = await api.post(`/tasks/${parentTaskId}/subtasks`, payload)
            const subtask: Task = {
                id: res.data.id,
                parentId: parentTaskId,
                title: res.data.title,
                notes: res.data.notes ?? null,
                deadline: res.data.deadline ?? null,
                completed: res.data.completed ?? null,
                createdAt: res.data.createdAt,
                modified: res.data.modified,
                pinned: false,
                priority: this.tasks.find(t => t.id === parentTaskId)?.priority ?? 0,
                rating: 0,
                icon: null
            }
            this.tasks.push(subtask)
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

            // If a subtask with a deadline is uncompleted, auto-uncheck the parent
            // to preserve the invariant that it can't be complete while this subtask isn't.
            if (completedAt === null && task.deadline && task.parentId) {
                const parent = this.tasks.find(t => t.id === task.parentId)
                if (parent?.completed) {
                    await api.put(`/tasks/${parent.id}`, { ...parent, completed: null })
                    const parentIndex = this.tasks.findIndex(t => t.id === parent.id)
                    if (parentIndex !== -1) this.tasks[parentIndex].completed = null
                }
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
        
        // Resets the sort position to now, effectively moving it to the top of the sorted list
        async resetCreationDate(task: Task) {
            const newModified = new Date().toISOString()
            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].modified = newModified
            }
            api.patch(`/tasks/${task.id}/order`, { modified: newModified })
        },
        
        // Removes the deadline (and timer) from a task
        async resetDeadline(task: Task) {
            const updated = {
                ...task,
                deadline: null,
                timerDuration: null
            }
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].deadline = null
                this.tasks[index].timerDuration = null
            }
        },

        // Moves a task relative to another task by adjusting its modified timestamp
        async moveTaskRelative(source: Task, target: Task, before: boolean) {
            // Get all tasks of the same priority and not completed/pinned
            const samePriorityTasks = this.tasks.filter(
                t => t.priority === source.priority && !t.completed && !t.pinned
            )

            // Sort them by modified date descending (matches display order)
            samePriorityTasks.sort((a, b) => new Date(b.modified).getTime() - new Date(a.modified).getTime())

            // Find target index in the sorted same-priority list
            const targetIndex = samePriorityTasks.findIndex(t => t.id === target.id)
            if (targetIndex === -1) return

            let newModified: string

            if (before) {
                // Place source **before** target → make modified slightly later than target
                const nextTask = samePriorityTasks[targetIndex - 1] // task above target
                if (nextTask) {
                    // Average timestamps to fit in between
                    const avg = (new Date(nextTask.modified).getTime() + new Date(target.modified).getTime()) / 2
                    newModified = new Date(avg).toISOString()
                } else {
                    // No task above → just slightly newer than target
                    newModified = new Date(new Date(target.modified).getTime() + 1).toISOString()
                }
            } else {
                // Place source **after** target → slightly older than target
                const nextTask = samePriorityTasks[targetIndex + 1] // task below target
                if (nextTask) {
                    const avg = (new Date(target.modified).getTime() + new Date(nextTask.modified).getTime()) / 2
                    newModified = new Date(avg).toISOString()
                } else {
                    // No task below → slightly older than target
                    newModified = new Date(new Date(target.modified).getTime() - 1).toISOString()
                }
            }

            // Update local state immediately for seamless UX
            const index = this.tasks.findIndex(t => t.id === source.id)
            if (index !== -1) this.tasks[index].modified = newModified

            // Persist to backend asynchronously (fire-and-forget)
            api.patch(`/tasks/${source.id}/order`, { modified: newModified })
        },

        // Moves a task to a different priority group, calculating a new modified timestamp to preserve
        // its visual position within the destination group.
        // destGroupTasks is the destination array after VueDraggable inserted the task at newIndex.
        async moveTaskToPriority(taskId: string, targetPriority: number, destGroupTasks: Task[], newIndex: number) {
            const task = this.tasks.find(t => t.id === taskId)
            if (!task) return

            const above = newIndex > 0 ? destGroupTasks[newIndex - 1] : undefined
            const below = newIndex < destGroupTasks.length - 1 ? destGroupTasks[newIndex + 1] : undefined

            let newModified: string
            if (above && below) {
                const avg = (new Date(above.modified).getTime() + new Date(below.modified).getTime()) / 2
                newModified = new Date(avg).toISOString()
            } else if (above) {
                // Bottom of dest list
                newModified = new Date(new Date(above.modified).getTime() - 1).toISOString()
            } else if (below) {
                // Top of dest list
                newModified = new Date(new Date(below.modified).getTime() + 1).toISOString()
            } else {
                // Only item in dest — keep current timestamp
                newModified = task.modified
            }

            // Pass modified so the backend stores exactly this timestamp (not DateTime.UtcNow).
            const updated = { ...task, priority: targetPriority, modified: newModified }

            // Optimistically update before the API call so the tasksByPriority
            // watch sees the new state immediately and doesn't snap the task back.
            const index = this.tasks.findIndex(t => t.id === taskId)
            const oldPriority = task.priority
            const oldModified = task.modified
            if (index !== -1) {
                this.tasks[index].priority = targetPriority
                this.tasks[index].modified = newModified
            }

            try {
                await api.put(`/tasks/${task.id}`, updated)
            } catch (e) {
                if (index !== -1) {
                    this.tasks[index].priority = oldPriority
                    this.tasks[index].modified = oldModified
                }
                throw e
            }
        },

        // Edits multiple task fields at once (title, notes, pinned, deadline, priority, rating, icon, timerDuration)
        async editTask(
            taskId: string,
            payload: {
                title: string,
                notes?: string | null,
                pinned?: boolean,
                deadline?: string | null,
                priority?: number,
                rating: number,
                icon?: number | null,
                timerDuration?: number | null,
            }
            ) {
            await api.put(`/tasks/${taskId}`, payload)

            const task = this.tasks.find(t => t.id === taskId)
            if (task) {
                task.title = payload.title
                task.notes = payload.notes ?? null

                if (payload.pinned !== undefined) task.pinned = payload.pinned
                if (payload.priority !== undefined) task.priority = payload.priority
                task.rating = payload.rating
                task.icon = payload.icon ?? null
                task.deadline = payload.deadline ?? null
                task.timerDuration = payload.timerDuration ?? null
            }
        },
        
        // Converts a top-level task into a subtask of another task
        async attachToParent(childId: string, parentId: string) {
            const task = this.tasks.find(t => t.id === childId)
            if (!task) return

            const prev = { parentId: task.parentId, pinned: task.pinned, rating: task.rating, icon: task.icon }
            task.parentId = parentId
            task.pinned = false
            task.rating = 0
            task.icon = null

            try {
                await api.put(`/tasks/${childId}/parent/${parentId}`)
            } catch (e) {
                task.parentId = prev.parentId
                task.pinned = prev.pinned
                task.rating = prev.rating
                task.icon = prev.icon
                throw e
            }
        },

        // Removes a task and its subtasks from the backend and local store
        async deleteTask(taskId: string) {
            this.tasks = this.tasks.filter(t => t.id !== taskId && t.parentId !== taskId)
            await api.delete(`/tasks/${taskId}`)
        },

        // Resets a task's rating to 0
        async resetRating(task: Task) {
            const updated = {
                ...task,
                rating: 0
            }
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].rating = 0
            }
        },

        // Reorders a subtask within its sibling list by averaging the modified timestamps of its neighbors
        async moveSubtaskRelative(source: Task, siblings: Task[], newIndex: number) {
            const above = newIndex > 0 ? siblings[newIndex - 1] : undefined
            const below = newIndex < siblings.length - 1 ? siblings[newIndex + 1] : undefined

            let newModified: string
            if (above && below) {
                const avg = (new Date(above.modified).getTime() + new Date(below.modified).getTime()) / 2
                newModified = new Date(avg).toISOString()
            } else if (above) {
                newModified = new Date(new Date(above.modified).getTime() + 1).toISOString()
            } else if (below) {
                newModified = new Date(new Date(below.modified).getTime() - 1).toISOString()
            } else {
                newModified = source.modified
            }

            const index = this.tasks.findIndex(t => t.id === source.id)
            if (index !== -1) this.tasks[index].modified = newModified
            api.patch(`/tasks/${source.id}/order`, { modified: newModified })
        },

        // Resets a task's priority to 0 (none)
        async resetPriority(task: Task) {
            const updated = {
                ...task,
                priority: 0
            }
            await api.put(`/tasks/${task.id}`, updated)

            const index = this.tasks.findIndex(t => t.id === task.id)
            if (index !== -1) {
                this.tasks[index].priority = 0
            }
        }
    }
})
