<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  rating: number
  priority: number
  pinned?: boolean
  interactive?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:rating', value: number): void
}>()

// Hard-coded colour values
const COLORS = {
  high:   'var(--color-warning)',
  med:    'var(--color-info)',
  low:    'var(--color-success)',
  accent: 'var(--color-secondary)',
  muted:  'var(--color-swoosh-text-muted)',
  empty:  '#3a3d46',
} as const

const filledColor = computed((): string => {
  if (props.interactive) return COLORS.muted
  if (props.pinned)         return COLORS.accent
  if (props.priority === 3) return COLORS.high
  if (props.priority === 2) return COLORS.med
  if (props.priority === 1) return COLORS.low
  return COLORS.muted
})

function diamondStyle(n: number) {
  return { background: n <= props.rating ? filledColor.value : COLORS.empty }
}

function setRating(n: number) {
  if (props.interactive) {
    emit('update:rating', props.rating === n ? 0 : n)
  }
}
</script>

<template>
  <div v-if="rating > 0 || interactive" class="task-rating">
    <div
        v-for="n in 5"
        :key="n"
        :class="interactive ? 'rdiamond' : 'diamond'"
        :style="diamondStyle(n)"
        @click="setRating(n)"
    />
  </div>
</template>