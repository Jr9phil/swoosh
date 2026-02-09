<script setup lang="ts">
import { Trash2, EllipsisVertical, ListStart, CalendarOff, Undo2 } from 'lucide-vue-next'

defineProps<{
  isCompleted: boolean
  hasDeadline: boolean
}>()

const emit = defineEmits<{
  (e: 'delete'): void
  (e: 'moveToTop'): void
  (e: 'resetDeadline'): void
  (e: 'unComplete'): void
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
      
      <li>
        <a @click="emit('delete')">
          <Trash2 :size="16" /> Delete
        </a>
      </li>
    </ul>
  </div>
</template>
