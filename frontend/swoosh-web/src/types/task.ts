export interface Task {
    id: string
    title: string
    notes?: string | null
    completed?: string | null
    deadline?: string | null
    pinned: boolean
    priority: number
    createdAt: string
}

export interface CreateTask {
    title: string
    notes?: string | null
    deadline?: string | null
}