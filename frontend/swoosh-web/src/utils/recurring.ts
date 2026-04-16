import type { RecurringTask } from '../types/recurring'
import type { Task } from '../types/task'

function startOfDay(d: Date): Date {
    const r = new Date(d)
    r.setHours(0, 0, 0, 0)
    return r
}

function isSameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() &&
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate()
}

/** Returns true if the recurring task has an occurrence on the given day. */
export function occursOnDay(task: RecurringTask, day: Date): boolean {
    if (!task.isActive) return false

    const dayStart = startOfDay(day)
    const start = task.recurrenceDate
        ? startOfDay(new Date(task.recurrenceDate + 'T00:00:00'))
        : startOfDay(new Date())

    if (dayStart < start) return false
    if (isSameDay(start, dayStart)) return true

    const n = task.recurrenceInterval
    const type = task.recurrenceType
    const diffMs = dayStart.getTime() - start.getTime()
    const diffDays = Math.round(diffMs / 86400000)

    if (type === 'day')  return diffDays % n === 0
    if (type === 'week') return diffDays % (7 * n) === 0

    if (type === 'month') {
        if (dayStart.getDate() !== start.getDate()) return false
        const months = (dayStart.getFullYear() - start.getFullYear()) * 12 +
            (dayStart.getMonth() - start.getMonth())
        return months % n === 0
    }

    if (type === 'year') {
        if (dayStart.getMonth() !== start.getMonth() || dayStart.getDate() !== start.getDate()) return false
        return (dayStart.getFullYear() - start.getFullYear()) % n === 0
    }

    return false
}

/** Returns a human-readable recurrence label, e.g. "Every 3 weeks". */
export function recurrenceLabel(task: RecurringTask): string {
    const n = task.recurrenceInterval
    const t = task.recurrenceType
    return n === 1 ? `Every ${t}` : `Every ${n} ${t}s`
}

/** Converts a recurring task occurrence into a Task-compatible object for the timeline. */
export function toTimelineTask(task: RecurringTask, day: Date): Task & { isRecurring: true } {
    const yyyy = day.getFullYear()
    const mm   = String(day.getMonth() + 1).padStart(2, '0')
    const dd   = String(day.getDate()).padStart(2, '0')
    const time = task.recurrenceTime ? `T${task.recurrenceTime}:00` : 'T23:59:00'
    return {
        id:           task.id,
        title:        task.title,
        notes:        task.notes,
        completed:    null,
        deadline:     `${yyyy}-${mm}-${dd}${time}`,
        pinned:       task.pinned,
        priority:     task.priority,
        rating:       task.rating,
        icon:         task.icon,
        createdAt:    task.createdAt,
        modified:     task.modified,
        isRecurring:  true,
    }
}
