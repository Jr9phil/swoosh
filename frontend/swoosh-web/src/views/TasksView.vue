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
    <div class="bg-base-200 border-base-300 rounded-box w-lg border p-4">

      <div v-if="tasksStore.loading"><span class="loading loading-spinner"></span></div>

      <ul v-else class="list bg-base-100 rounded-box shadow-md">
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

    <div class="divider"></div>

    <TaskForm />
  </div>
</template>
