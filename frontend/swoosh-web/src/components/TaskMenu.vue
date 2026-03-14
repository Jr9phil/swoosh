<!-- 
  TaskMenu.vue
  A dropdown menu component for task-specific actions.
  Provides options like moving to top, resetting deadline, toggling completion, and deleting.
-->
<script setup lang="ts">
import { Trash2, EllipsisVertical, Pin, CalendarOff, CalendarPlus, Undo2, Diamond } from 'lucide-vue-next'
import { computed } from 'vue'

const props = defineProps<{
  isCompleted: boolean
  hasDeadline: boolean
  hasRating: boolean
  pinned: boolean
}>()

const emit = defineEmits<{
  (e: 'delete'): void
  (e: 'pin'): void
  (e: 'resetDeadline'): void
  (e: 'unComplete'): void
  (e: 'edit'): void
  (e: 'resetRating'): void
}>()

interface MenuItem {
  label: string
  icon: any
  action: () => void
  show: boolean
}

const menuItems = computed<MenuItem[]>(() => [
  {
    label: props.pinned ? 'Unpin task' : 'Pin task',
    icon: Pin,
    action: () => emit('pin'),
    show: !props.isCompleted
  },
  {
    label: 'Mark incomplete',
    icon: Undo2,
    action: () => emit('unComplete'),
    show: props.isCompleted
  },
  {
    label: 'Remove deadline',
    icon: CalendarOff,
    action: () => emit('resetDeadline'),
    show: !props.isCompleted && props.hasDeadline
  },
  {
    label: 'Add deadline',
    icon: CalendarPlus,
    action: () => emit('edit'),
    show: !props.isCompleted && !props.hasDeadline
  },
  {
    label: 'Clear rating',
    icon: Diamond,
    action: () => emit('resetRating'),
    show: !props.isCompleted && props.hasRating
  }
])

const activeMenuItems = computed(() => menuItems.value.filter(item => item.show))

const baseItemClass = 'flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium rounded-sm transition-colors'
const defaultItemClass = `${baseItemClass} text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5`
const dangerItemClass = `${baseItemClass} text-error hover:bg-error/10`
</script>

<!-- Component Template: Dropdown menu for task actions -->
<template>
  <div class="dropdown dropdown-end lg:dropdown-right lg:dropdown-start">
    <!-- Trigger button for the dropdown menu -->
    <button
        tabindex="0"
        role="button"
        class="w-[28px] h-[28px] rounded-full flex items-center justify-center text-swoosh-text-faint hover:text-swoosh-text-muted hover:bg-base-300 transition-colors"
    >
      <EllipsisVertical :size="18" fill="currentColor" />
    </button>

    <!-- Dropdown content list -->
    <ul
        tabindex="0"
        class="dropdown-content menu p-1.5 shadow-2xl bg-base-300 border border-swoosh-border-hover rounded-sm w-[180px] z-[100] gap-0.5"
    >
      <div class="px-2 py-1.5 flex flex-col gap-0.5">
        <li v-for="item in activeMenuItems" :key="item.label">
          <button @click="item.action" :class="defaultItemClass">
            <component :is="item.icon" :size="14" /> {{ item.label }}
          </button>
        </li>
      </div>

      <div class="h-px bg-white/10 my-1 mx-2"></div>

      <!-- Action: Delete the task -->
      <div class="px-2 pb-1.5">
        <li>
          <button @click="emit('delete')" :class="dangerItemClass">
            <Trash2 :size="14" /> Delete task
          </button>
        </li>
      </div>
    </ul>
  </div>
</template>
