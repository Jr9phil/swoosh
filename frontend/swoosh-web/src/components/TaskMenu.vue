<!-- 
  TaskMenu.vue
  A dropdown menu component for task-specific actions.
  Provides options like moving to top, resetting deadline, toggling completion, and deleting.
-->
<script setup lang="ts">
import { Trash2, EllipsisVertical, ListStart, CalendarOff, CalendarPlus, Undo2, ChessPawn } from 'lucide-vue-next'

defineProps<{
  isCompleted: boolean
  hasDeadline: boolean
  hasPriority: boolean
}>()

const emit = defineEmits<{
  (e: 'delete'): void
  (e: 'moveToTop'): void
  (e: 'resetDeadline'): void
  (e: 'unComplete'): void
  (e: 'edit'): void
  (e: 'priority'): void
}>()
</script>

<!-- Component Template: Dropdown menu for task actions -->
<template>
  <!-- Main dropdown container, positioned based on completion status -->
  <div class="dropdown" :class="isCompleted ? 'dropdown-left' : 'dropdown-right'">
    <!-- Trigger button for the dropdown menu -->
    <button
        tabindex="0"
        role="button"
        class="btn btn-ghost btn-circle btn-sm group"
    >
      <EllipsisVertical class="opacity-10 group-hover:opacity-60" />
    </button>

    <!-- Dropdown content list -->
    <ul
        tabindex="-1"
        class="dropdown-content menu bg-base-300 rounded-box ml-2 z-1 w-44 p-2 shadow-sm"
    >
      <!-- Action: Move task to the top (only for incomplete tasks) -->
      <li v-if="!isCompleted">
        <a @click="emit('moveToTop')">
          <ListStart :size="16" /> Move to top
        </a>
      </li>

      <!-- Action: Revert completion status (only for completed tasks) -->
      <li v-else>
        <a @click="emit('unComplete')">
          <Undo2 :size="16" /> Mark incomplete
        </a>
      </li>

      <!-- Action: Remove an existing deadline -->
      <li v-if="!isCompleted && !!hasDeadline">
        <a @click="emit('resetDeadline')">
          <CalendarOff :size="16" /> Remove deadline
        </a>
      </li>
      
      <!-- Action: Open editor to add a deadline -->
      <li v-else-if="!isCompleted">
        <a @click="emit('edit')">
          <CalendarPlus :size="16" /> Add deadline
        </a>
      </li>
      
      <!-- Action: Reset task priority to default -->
      <li v-if="!isCompleted && !!hasPriority">
        <a @click="emit('priority')">
          <ChessPawn :size="16" /> Reset priority
        </a>
      </li>
      
      <!-- Action: Delete the task -->
      <li>
        <a @click="emit('delete')" class="link-error">
          <Trash2 :size="16" /> Delete
        </a>
      </li>
    </ul>
  </div>
</template>
