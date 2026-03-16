<script setup lang="ts">
import { computed } from 'vue'
import { TASK_ICONS } from '../types/icon'

const props = defineProps<{
  value: number
}>()

const icon = computed(() => TASK_ICONS.find(i => i.value === props.value) ?? TASK_ICONS[0])
</script>

<template>
  <component :is="icon.icon" :size="16" :class="icon.color" class="shrink-0" />
</template>
