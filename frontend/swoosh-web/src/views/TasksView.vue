<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import TaskForm from '../components/TaskForm.vue'
import TaskItem from '../components/TaskItem.vue'
import { CircleCheckBig, Plus, Rocket, ListChecks } from 'lucide-vue-next'


const tasksStore = useTasksStore()
const incompleteTasks = computed(() =>
    tasksStore.tasks.filter(t => !t.completed && !t.pinned)
)

const pinnedTasks = computed(() =>
    tasksStore.tasks.filter(t => !t.completed && t.pinned)
)

const completedTasks = computed(() =>
    tasksStore.tasks.filter(t => t.completed)
)

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
    <div class="bg-base-200 border-base-300 rounded-box w-xl border p-4">
      
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
        <ul v-if="pinnedTasks.length" class="list bg-base-100 rounded-box shadow-md">
          <TaskItem
              v-for="task in pinnedTasks"
              :key="task.id"
              :task="task"
          />
        </ul>
        
        <div v-if="pinnedTasks.length" class="divider"></div>
        
        <ul class="list bg-base-100 rounded-box shadow-md">
          <TaskItem
              v-for="task in incompleteTasks"
              :key="task.id"
              :task="task"
          />
        </ul>

        <div v-if="!tasksStore.tasks.length" class="card bg-base-300 rounded-box grid h-96 place-items-center">
          <div class="opacity-50 flex flex-col items-center text-center gap-2">
            <Rocket class="w-24 h-24"/>
            <b>No tasks yet</b>
            <p class="text-sm">Add a task to get started</p>
          </div>
        </div>
        
        <div v-else-if="completedTasks.length">
          
          <div v-if="!incompleteTasks.length" class="card bg-base-300 rounded-box grid h-24 place-items-center">
            <div class="flex opacity-50">
              <ListChecks class="mr-2"/> All tasks completed!
            </div>
          </div>
          
          <div class="collapse collapse-arrow">
            <input type="checkbox" name="completed-tasks-list" />
            <div class="collapse-title opacity-50">Completed</div>
            <div class="collapse-content p-0">
              <ul class="list bg-base-100 rounded-box shadow-md">
                <TaskItem
                    v-for="task in completedTasks"
                    :key="task.id"
                    :task="task"
                />
              </ul>
            </div>
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
