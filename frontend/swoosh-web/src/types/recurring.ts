export type RecurrenceType = 'daily' | 'interval' | 'weekly' | 'monthly' | 'custom'

export interface RecurringTask {
    id: string
    title: string
    notes: string | null
    recurrenceType: RecurrenceType
    recurrenceInterval: number | null
    isActive: boolean
    priority: number
    pinned: boolean
    rating: number
    icon: number | null
    createdAt: string
    modified: string
}

export interface CreateRecurringTask {
    title: string
    notes?: string | null
    recurrenceType: RecurrenceType
    recurrenceInterval?: number | null
    isActive: boolean
    priority?: number
    pinned?: boolean
    rating?: number
    icon?: number | null
}
