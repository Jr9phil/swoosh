<script setup lang="ts">
import { onMounted } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'

const tasksStore = useTasksStore()
const auth = useAuthStore()
const router = useRouter()

onMounted(async () => {
  try {
    await tasksStore.fetchTasks()
  } catch {
    auth.logout()
    router.push('/login')
  }
})
</script>

<template>
  <div>
    <h1>Your Tasks</h1>

    <div v-if="tasksStore.loading">Loading…</div>

    <ul v-else>
      <li v-for="task in tasksStore.tasks" :key="task.id">
        <strong>{{ task.title }}</strong>
        <span v-if="task.isCompleted">✔</span>
      </li>
    </ul>

    <p v-if="!tasksStore.tasks.length">
      No tasks yet.
    </p>
  </div>
</template>
