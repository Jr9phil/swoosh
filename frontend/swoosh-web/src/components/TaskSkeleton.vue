<template>
<!--  A placeholder component shown while tasks are loading.-->
  <div class="task-skeleton">
    <div class="shrink-0 mt-0.5 w-5 h-5 rounded-sm bg-white/5 animate-pulse"></div>
    <div class="flex-1 min-w-0 space-y-3">
      <div class="h-4 bg-white/5 rounded-sm w-3/4 animate-pulse"></div>
      <div class="space-y-2">
        <div class="h-3 bg-white/5 rounded-sm w-full animate-pulse"></div>
        <div class="h-3 bg-white/5 rounded-sm w-1/2 animate-pulse"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: .5;
  }
}
</style>
