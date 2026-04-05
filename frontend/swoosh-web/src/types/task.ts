export interface Task {
    id: string
    parentId?: string | null
    title: string
    notes?: string | null
    completed?: string | null
    deadline?: string | null
    pinned: boolean
    priority: number
    rating: number
    icon?: number | null
    timerDuration?: number | null
    createdAt: string
    modified: string
    isRecurring?: boolean
}

export interface CreateTask {
    title: string
    notes?: string | null
    deadline?: string | null
    pinned?: boolean
    priority?: number
    rating?: number
    icon?: number | null
    timerDuration?: number | null
}