<script setup lang="ts">
import { onMounted } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import TaskForm from '../components/TaskForm.vue'
import TaskItem from '../components/TaskItem.vue'
import { CircleCheckBig, Plus, List } from 'lucide-vue-next'


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
      
      <div class="flex mb-2 cursor-default">
        <span v-if="tasksStore.loading" class="loading loading-spinner text-primary mr-4" />
        <CircleCheckBig v-else class="text-primary mr-4" />
        <h1 class="text-xl text-heading">My Tasks</h1>
      </div>
      
      <button :disabled = "tasksStore.loading" class="btn btn-ghost btn-info mb-2" onclick="create.showModal()">
        <Plus /> Add a task
      </button>
      
      <div v-if="tasksStore.loading" class="skeleton w-full h-96" />

      <div v-else class="min-h-96">
        <ul class="list bg-base-100 rounded-box shadow-md">
          <TaskItem
              v-for="task in tasksStore.tasks"
              :key="task.id"
              :task="task"
          />
        </ul>

        <div v-if="!tasksStore.tasks.length" class="card bg-base-300 rounded-box grid h-96 place-items-center">
          <div>
            <List class=" text-base-200 w-24 h-24"/>
          </div>
        </div>

      </div>
    </div>
  </div>

  <dialog id="create" class="modal">
    <form method="dialog" class="modal-box w-xs bg-base-200 border border-base-300">
      <TaskForm />
    </form>

    <form method="dialog" class="modal-backdrop">
      <button>close</button>
    </form>
  </dialog>
</template>
