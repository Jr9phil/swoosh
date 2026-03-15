import type { DirectiveBinding } from 'vue'

const SYNC_GROUPS = {
    overdue: {
        duration: 1100,
        animations: {
            dot:    'overdueGlow 1.1s ease-in-out',
            badge:  'overdueBadge 1.1s ease-in-out',
            banner: 'overdueBadge 1.1s ease-in-out',
            count:  'overdueBadge 1.1s ease-in-out'
        }
    },
    today: {
        duration: 2400,
        animations: {
            dot:    'todayPulse 2.4s ease-in-out',
            border: 'todayBorderPulse 2.4s ease-in-out',
            badge:  'todayBadgePulse 2.4s ease-in-out'
        }
    }
}

function applyAnimation(el: HTMLElement, binding: DirectiveBinding) {
    let groupName = binding.arg as string || 'overdue'
    let type: string | undefined

    if (binding.value && typeof binding.value === 'object') {
        groupName = binding.value.group || groupName
        type = binding.value.type
    } else {
        type = binding.value
    }

    const currentType = el.getAttribute('data-sync-type')
    const currentGroup = el.getAttribute('data-sync-group')

    if (currentType === type && currentGroup === groupName) {
        return
    }

    if (!type || !groupName) {
        el.style.animation = ''
        el.removeAttribute('data-sync-type')
        el.removeAttribute('data-sync-group')
        return
    }

    const group = SYNC_GROUPS[groupName as keyof typeof SYNC_GROUPS]
    if (!group) return

    const animationTemplate = group.animations[type as keyof typeof group.animations]
    if (!animationTemplate) return

    el.setAttribute('data-sync-type', type)
    el.setAttribute('data-sync-group', groupName)

    // Parse the template (e.g. "overdueGlow 1.1s ease-in-out") to set properties individually
    // ensuring we don't restart the animation if we just want to update the sync phase.
    const [name, duration, ...timing] = animationTemplate.split(' ')
    const phase = -Math.floor(performance.now() % group.duration)

    el.style.animationName = name
    el.style.animationDuration = duration
    el.style.animationTimingFunction = timing.join(' ')
    el.style.animationDelay = `${phase}ms`
    el.style.animationIterationCount = 'infinite'
    el.style.animationFillMode = 'both'
}

export const animateSync = {
    mounted(el: HTMLElement, binding: DirectiveBinding) {
        applyAnimation(el, binding)
    },
    updated(el: HTMLElement, binding: DirectiveBinding) {
        applyAnimation(el, binding)
    }
}
