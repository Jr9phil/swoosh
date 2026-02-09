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

<template>
  <div class="dropdown" :class="isCompleted ? 'dropdown-left' : 'dropdown-right'">
    <button
        tabindex="0"
        role="button"
        class="btn btn-ghost btn-circle btn-sm group"
    >
      <EllipsisVertical class="opacity-10 group-hover:opacity-60" />
    </button>

    <ul
        tabindex="-1"
        class="dropdown-content menu bg-base-300 rounded-box ml-2 z-1 w-44 p-2 shadow-sm"
    >
      <li v-if="!isCompleted">
        <a @click="emit('moveToTop')">
          <ListStart :size="16" /> Move to top
        </a>
      </li>

      <li v-else>
        <a @click="emit('unComplete')">
          <Undo2 :size="16" /> Mark incomplete
        </a>
      </li>

      <li v-if="!isCompleted && !!hasDeadline">
        <a @click="emit('resetDeadline')">
          <CalendarOff :size="16" /> Remove deadline
        </a>
      </li>
      
      <li v-else-if="!isCompleted">
        <a @click="emit('edit')">
          <CalendarPlus :size="16" /> Add deadline
        </a>
      </li>
      
      <li v-if="!isCompleted && !!hasPriority">
        <a @click="emit('priority')">
          <ChessPawn :size="16" /> Reset priority
        </a>
      </li>
      
      <li>
        <a @click="emit('delete')" class="link-error">
          <Trash2 :size="16" /> Delete
        </a>
      </li>
    </ul>
  </div>
</template>
