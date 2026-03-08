<!-- 
  TaskMenu.vue
  A dropdown menu component for task-specific actions.
  Provides options like moving to top, resetting deadline, toggling completion, and deleting.
-->
<script setup lang="ts">
import { Trash2, EllipsisVertical, Pin, PinOff, CalendarOff, CalendarPlus, Undo2, ChessPawn, StarOff } from 'lucide-vue-next'

defineProps<{
  isCompleted: boolean
  hasDeadline: boolean
  hasPriority: boolean
  hasRating: boolean
  pinned: boolean
}>()

const emit = defineEmits<{
  (e: 'delete'): void
  (e: 'pin'): void
  (e: 'resetDeadline'): void
  (e: 'unComplete'): void
  (e: 'edit'): void
  (e: 'priority'): void
  (e: 'resetRating'): void
}>()
</script>

<!-- Component Template: Dropdown menu for task actions -->
<template>
  <div class="dropdown dropdown-end">
    <!-- Trigger button for the dropdown menu -->
    <button
        tabindex="0"
        role="button"
        class="w-[28px] h-[28px] rounded-full flex items-center justify-center text-swoosh-text-faint hover:text-swoosh-text-muted hover:bg-surface-raised transition-colors"
    >
      <EllipsisVertical :size="18" fill="currentColor" />
    </button>

    <!-- Dropdown content list -->
    <ul
        tabindex="0"
        class="dropdown-content menu p-1.5 shadow-2xl bg-swoosh-surface-raised border border-swoosh-border-hover rounded-sm w-[180px] z-[100] gap-0.5"
    >
      <div class="px-2 py-1.5 flex flex-col gap-0.5">
        <!-- Action: Pin task -->
        <li v-if="!isCompleted">
          <button @click="emit('pin')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <Pin :size="14" /> {{ pinned ? 'Unpin task' : 'Pin task' }}
          </button>
        </li>

        <!-- Action: Revert completion status -->
        <li v-else>
          <button @click="emit('unComplete')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <Undo2 :size="14" /> Mark incomplete
          </button>
        </li>

        <!-- Action: Deadlines -->
        <li v-if="!isCompleted && !!hasDeadline">
          <button @click="emit('resetDeadline')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <CalendarOff :size="14" /> Remove deadline
          </button>
        </li>
        <li v-else-if="!isCompleted">
          <button @click="emit('edit')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <CalendarPlus :size="14" /> Add deadline
          </button>
        </li>

        <!-- Action: Reset priority -->
        <li v-if="!isCompleted && !!hasPriority">
          <button @click="emit('priority')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <ChessPawn :size="14" /> Reset priority
          </button>
        </li>

        <!-- Action: Reset rating -->
        <li v-if="!isCompleted && !!hasRating">
          <button @click="emit('resetRating')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-text-muted hover:text-swoosh-text hover:bg-white/5 rounded-sm transition-colors">
            <StarOff :size="14" /> Clear rating
          </button>
        </li>
      </div>

      <div class="h-px bg-white/10 my-1 mx-2"></div>

      <!-- Action: Delete the task -->
      <div class="px-2 pb-1.5">
        <li>
          <button @click="emit('delete')" class="flex items-center gap-2.5 px-2 py-1.5 w-full text-left text-[13px] font-medium text-swoosh-danger hover:bg-swoosh-danger/10 rounded-sm transition-colors">
            <Trash2 :size="14" /> Delete task
          </button>
        </li>
      </div>
    </ul>
  </div>
</template>
