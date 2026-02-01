export interface Task {
    id: string
    title: string
    notes?: string | null
    deadline?: string | null
    isCompleted: boolean
    createdAt: string
}

export interface CreateTask {
    title: string
    notes?: string | null
    deadline?: string | null
}