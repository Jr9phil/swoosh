<script setup lang="ts">
import { onMounted } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import TaskForm from '../components/TaskForm.vue'
import TaskItem from '../components/TaskItem.vue'


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
    <TaskForm />
    
    <h1>Your Tasks</h1>

    <div v-if="tasksStore.loading">Loading…</div>

    <ul v-else>
      <TaskItem
          v-for="task in tasksStore.tasks"
          :key="task.id"
          :task="task"
      />
    </ul>

    <p v-if="!tasksStore.tasks.length">
      No tasks yet.
    </p>
  </div>
</template>
