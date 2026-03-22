import { ref, watch, nextTick } from 'vue'
import type { ComputedRef } from 'vue'
import { useTasksStore } from '../stores/tasks'
import type { Task } from '../types/task'
import type { Priority } from '../types/priority'
import { resyncAnimatedChildren } from '../directives/animateSync'

interface PriorityGroup {
    priority: Priority
    tasks: Task[]
}

export function useTaskDrag(tasksByPriority: ComputedRef<PriorityGroup[]>) {
    const tasksStore = useTasksStore()

    // Per-priority mutable task arrays bound to VueDraggable. Kept in sync with
    // the store via the watch below; user-drag order is preserved via frozenGroups.
    const draggableGroups = ref<Record<number, Task[]>>({})

    // Priorities the user has reordered this session. The watch uses the existing
    // draggableGroups order for these instead of re-applying the computed sort.
    const frozenGroups = new Set<number>()

    // Blocked while a drag is in progress so clock-driven tasksByPriority
    // recomputes don't overwrite draggableGroups mid-gesture.
    let dragActive = false

    watch(tasksByPriority, (groups) => {
        if (dragActive) return
        const next: Record<number, Task[]> = {}
        groups.forEach(g => {
            const p = g.priority.value
            if (frozenGroups.has(p)) {
                const storeMap = new Map(g.tasks.map(t => [t.id, t]))
                const preserved = (draggableGroups.value[p] ?? [])
                    .filter(t => storeMap.has(t.id))
                    .map(t => storeMap.get(t.id)!)
                const preservedIds = new Set(preserved.map(t => t.id))
                const added = g.tasks.filter(t => !preservedIds.has(t.id))
                next[p] = [...added, ...preserved]
            } else {
                next[p] = [...g.tasks]
            }
        })
        draggableGroups.value = next
    }, { immediate: true, flush: 'sync' })

    function handleDragChoose(evt: any) {
        dragActive = true
        // Clear v-animate-sync guard attrs so the directive recalculates animation
        // phase on the next clock tick rather than being blocked by the equality check.
        const el = evt.item as HTMLElement
        el.querySelectorAll<HTMLElement>('[data-sync-type]').forEach(badge => {
            badge.removeAttribute('data-sync-type')
            badge.removeAttribute('data-sync-group')
        })
    }

    function handleModelUpdate(priority: number, val: Task[]) {
        draggableGroups.value[priority] = val
    }

    function onGroupDragEnd(evt: any, sourcePriorityValue: number) {
        // Unblock the watch now — @update:model-value has already fired on both
        // lists, so draggableGroups reflects the drag result. The store action
        // below updates the store optimistically, so the watch will see a
        // consistent state on its next fire.
        dragActive = false

        const { oldIndex, newIndex } = evt

        if (evt.from !== evt.to) {
            const destPriority = parseInt((evt.to as HTMLElement).dataset.priority ?? '')
            if (isNaN(destPriority)) return

            frozenGroups.add(sourcePriorityValue)
            frozenGroups.add(destPriority)

            const taskId = (evt.item as HTMLElement).id.replace('task-', '')
            const destItems = draggableGroups.value[destPriority]
            if (!destItems) return

            tasksStore.moveTaskToPriority(taskId, destPriority, destItems, newIndex ?? 0)
            resyncAnimatedChildren(evt.item as HTMLElement)

            // VueDraggable's onRemove re-inserts the dragged element into the source
            // container (via Tt) so Vue can cleanly remove it during reconciliation.
            // In some cases Vue's patch leaves the element as an orphan in source;
            // this nextTick pass removes any such orphan after the render settles.
            const srcContainer = evt.from as HTMLElement
            const destContainer = evt.to as HTMLElement
            nextTick(() => {
                const taskElId = 'task-' + taskId
                ;(srcContainer.querySelector('#' + taskElId) as HTMLElement | null)?.remove()
                const dupes = Array.from(destContainer.querySelectorAll('#' + taskElId))
                dupes.slice(1).forEach(el => el.remove())
            })
            return
        }

        if (oldIndex == null || newIndex == null || oldIndex === newIndex) return

        frozenGroups.add(sourcePriorityValue)

        const items = draggableGroups.value[sourcePriorityValue]
        if (!items) return
        const source = items[newIndex]
        if (!source) return

        const target = newIndex < items.length - 1 ? items[newIndex + 1] : items[newIndex - 1]
        if (!target) return
        const before = newIndex < items.length - 1

        tasksStore.moveTaskRelative(source, target, before)
        resyncAnimatedChildren(evt.item as HTMLElement)
    }

    return { draggableGroups, handleDragChoose, handleModelUpdate, onGroupDragEnd }
}
