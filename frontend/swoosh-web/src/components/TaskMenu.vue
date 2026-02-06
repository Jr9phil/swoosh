<script setup lang="ts">
import { Trash2, EllipsisVertical, ListStart } from 'lucide-vue-next'

defineProps<{
  isCompleted: boolean
}>()

const emit = defineEmits<{
  (e: 'delete'): void
  (e: 'moveToTop'): void
  (e: 'edit'): void
}>()
</script>

<template>
  <div v-if="!isCompleted" class="dropdown dropdown-right">
    <button
        tabindex="0"
        role="button"
        class="btn btn-ghost btn-circle btn-sm group"
    >
      <EllipsisVertical class="opacity-10 group-hover:opacity-60" />
    </button>

    <ul
        tabindex="-1"
        class="dropdown-content menu bg-base-300 rounded-box ml-2 z-1 w-40 p-2 shadow-sm"
    >
      <li>
        <a @click="emit('moveToTop')">
          <ListStart :size="16" /> Move to top
        </a>
      </li>
      
      <li>
        <a @click="emit('delete')">
          <Trash2 :size="16" /> Delete
        </a>
      </li>
    </ul>
  </div>
  <div v-else>
    <button @click="emit('delete')" class="btn btn-ghost btn-circle btn-sm group">
      <Trash2 :size="16" class="opacity-10 group-hover:opacity-60" />
    </button>
  </div>
</template>
