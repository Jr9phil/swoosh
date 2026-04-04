import { defineStore } from 'pinia'
import { ref } from 'vue'

export type CreateModalTab = 'task' | 'recurring' | 'notecard' | 'reminder'

export interface TaskPrefill {
    title?: string
    notes?: string | null
    deadline?: string | null
    priority?: number
}

export const useUiStore = defineStore('ui', () => {
    const openCreateModal = ref(false)
    const activeTab = ref<CreateModalTab>('task')
    const taskPrefill = ref<TaskPrefill | null>(null)
    const taskDate = ref<string | null>(null)
    const onTaskCreated = ref<(() => void) | null>(null)

    function triggerCreateModal(tab: CreateModalTab = 'task', opts?: {
        prefill?: TaskPrefill
        date?: string
        onCreated?: () => void
    }) {
        activeTab.value = tab
        taskPrefill.value = opts?.prefill ?? null
        taskDate.value = opts?.date ?? null
        onTaskCreated.value = opts?.onCreated ?? null
        openCreateModal.value = true
    }

    function consumeCreateModal() {
        openCreateModal.value = false
    }

    function resetModal() {
        taskPrefill.value = null
        taskDate.value = null
        onTaskCreated.value = null
    }

    return {
        openCreateModal,
        activeTab,
        taskPrefill,
        taskDate,
        onTaskCreated,
        triggerCreateModal,
        consumeCreateModal,
        resetModal,
    }
})
