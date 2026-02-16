import type { DirectiveBinding } from 'vue'

export const clickOutside = {
    mounted(el: HTMLElement & { __clickOutside__?: (event: MouseEvent) => void }, binding: DirectiveBinding) {
        el.__clickOutside__ = (event: MouseEvent) => {
            if (!el.contains(event.target as Node)) {
                binding.value(event)
            }
        }

        document.addEventListener('mousedown', el.__clickOutside__)
    },
    unmounted(el: HTMLElement & { __clickOutside__?: (event: MouseEvent) => void }) {
        if (el.__clickOutside__) {
            document.removeEventListener('mousedown', el.__clickOutside__)
        }
    }
}