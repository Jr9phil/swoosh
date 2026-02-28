<script setup lang="ts">
import { computed } from 'vue'
import { PRIORITIES } from '../types/priority'

const props = defineProps<{
  rating: number
  priority: number
}>()

const priorityClass = computed(() => {
  const p = PRIORITIES.find(p => p.value === props.priority)
  return p ? p.textClass : 'text-primary'
})
</script>

<template>
  <div v-if="rating > 0" class="rating rating-xs flex-auto items-center justify-end">
    <div 
      v-for="n in 5" 
      :key="n"
      class="mask mask-diamond"
      :class="[n <= rating ? [priorityClass, 'opacity-90'] : 'bg-base-300 opacity-80', n <= rating ? 'bg-current' : '']"
    />
  </div>
</template>