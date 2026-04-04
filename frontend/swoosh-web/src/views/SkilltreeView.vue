<script setup lang="ts">
import { Network } from 'lucide-vue-next'
</script>

<template>
    <div class="placeholder-view">
        <Network class="placeholder-icon" />
        <h1 class="placeholder-title">Skill Tree</h1>
        <p class="placeholder-subtitle">Coming soon.</p>
    </div>
</template>

<style scoped>
.placeholder-view {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    min-height: 60vh;
    gap: 12px;
    color: var(--color-swoosh-text-faint);
}

.placeholder-icon {
    width: 40px;
    height: 40px;
    opacity: 0.2;
}

.placeholder-title {
    font-family: var(--font-mono);
    font-size: 13px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.12em;
    color: var(--color-swoosh-text-muted);
}

.placeholder-subtitle {
    font-size: 0.875rem;
    color: var(--color-swoosh-text-faint);
}
</style>
