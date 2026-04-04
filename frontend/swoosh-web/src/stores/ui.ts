import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useUiStore = defineStore('ui', () => {
    const openCreateModal = ref(false)

    function triggerCreateModal() {
        openCreateModal.value = true
    }

    function consumeCreateModal() {
        openCreateModal.value = false
    }

    return { openCreateModal, triggerCreateModal, consumeCreateModal }
})
