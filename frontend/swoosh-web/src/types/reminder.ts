export interface Reminder {
    id: string
    title: string
    notes: string | null
    remindAt: string
    isCompleted: boolean
    createdAt: string
    modified: string
}

export interface CreateReminder {
    title: string
    notes?: string | null
    remindAt: string
    isCompleted: boolean
}
