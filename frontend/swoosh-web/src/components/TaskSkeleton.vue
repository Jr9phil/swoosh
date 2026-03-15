<template>
  <!-- A placeholder component shown while tasks are loading. -->
  <div
    class="task-skeleton"
    :class="{ 'items-center': variant === 'default' }"
    :style="{ '--delay': `-${(index ?? 0) * 150}ms` }"
  >
    <!-- Checkbox placeholder — matches actual .swoosh-check: 23×23, circular -->
    <div class="shrink-0 w-[23px] h-[23px] rounded-full shimmer"></div>

    <div class="flex-1 min-w-0 flex flex-col gap-2">
      <!-- Title bar -->
      <div class="h-[14px] rounded-sm shimmer" :style="{ width: titleWidth ?? '65%' }"></div>

      <!-- Notes variant: two description lines -->
      <template v-if="variant === 'notes'">
        <div class="h-[12px] rounded-sm shimmer w-full"></div>
        <div class="h-[12px] rounded-sm shimmer w-1/2"></div>
      </template>

      <!-- Badge variant: a pill-shaped deadline placeholder -->
      <template v-else-if="variant === 'badge'">
        <div class="h-[20px] rounded-full shimmer w-[84px]"></div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  variant?: 'default' | 'notes' | 'badge'
  titleWidth?: string
  /** Stagger index — each step offsets the shimmer phase by 150ms */
  index?: number
}>()
</script>

<style scoped>
.shimmer {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.04) 25%,
    rgba(255, 255, 255, 0.10) 50%,
    rgba(255, 255, 255, 0.04) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 2s linear infinite;
  animation-delay: var(--delay, 0ms);
}

@keyframes shimmer {
  0%   { background-position:  200% 0; }
  100% { background-position: -200% 0; }
}
</style>