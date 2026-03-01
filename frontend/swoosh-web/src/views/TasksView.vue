<!-- 
  TasksView.vue
  Main view for displaying and managing tasks. 
  Includes sections for pinned, incomplete, and completed tasks.
-->
<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useTasksStore } from '../stores/tasks'
import { useAuthStore } from '../stores/auth'
import type { task } from '../types/task'
import { PRIORITIES } from '../types/priority.ts'
import { useRouter } from 'vue-router'
import TaskEdit from '../components/TaskEdit.vue'
import TaskItem from '../components/TaskItem.vue'
import { CircleCheckBig, Plus, Rocket, ListChecks } from 'lucide-vue-next'


const tasksStore = useTasksStore()
const incompleteTasks = computed(() =>
    tasksStore.tasks
        .filter(t => !t.completed && !t.pinned && !isDueToday(t.deadline))
        .slice()
        .sort((a, b) => {
          const overdueA = isOverdue(a.deadline)
          const overdueB = isOverdue(b.deadline)
          if (overdueA !== overdueB) return overdueA ? -1 : 1
          
          if (b.priority !== a.priority) return b.priority - a.priority
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        })
)

const tasksByPriority = computed(() => {
  const groups = PRIORITIES.map(p => ({
    priority: p,
    tasks: incompleteTasks.value.filter(t => t.priority === p.value)
  }))
  return groups.filter(g => g.tasks.length > 0).reverse()
})

// Checks if a task is due today based on its deadline
function isDueToday(deadline? : string | null) {
  if (!deadline) return false
  
  const now = new Date()
  const d = new Date(deadline)
  
  return (
      now.getFullYear() === d.getFullYear() &&
      now.getMonth() === d.getMonth() &&
      now.getDate() === d.getDate()
  )
}

// Checks if a task is overdue based on its deadline
function isOverdue(deadline? : string | null) {
  if (!deadline) return false
  
  const now = new Date()
  const d = new Date(deadline)
  
  return now.getTime() > d.getTime()
}

// Pinned Tasks
const pinnedTasks = computed(() =>
    tasksStore.tasks
        .filter(t =>
            !t.completed &&
            (t.pinned || isDueToday(t.deadline))
        )
        .slice()
        .sort((a, b) => {
          const overdueA = isOverdue(a.deadline)
          const overdueB = isOverdue(b.deadline)
          if (overdueA !== overdueB) return overdueA ? -1 : 1

          if (b.priority !== a.priority) {
            return b.priority - a.priority
          }
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        })
)

// Checks if any tasks are due today
const anyTaskDueToday = computed(() =>
    pinnedTasks.value.some(t => isDueToday(t.deadline))
)

// Completed Tasks
const completedTasks = computed(() =>
    tasksStore.tasks
        .filter(t => t.completed)
        .slice()
        .sort((a, b) => {
          return (
              new Date(b.completed!).getTime() -
              new Date(a.completed!).getTime()
          )
        })
)

const draggedTask = ref<task | null>(null)

// Handles the start of a drag operation for a task
function onDragStart(task: task) {
  draggedTask.value = { ...task }
}

// Handles the drop operation when a task is reordered
async function onDrop(targetTask: task) {
  const source = draggedTask.value
  if (!source || source.id === targetTask.id) return
  
  if (source.priority !== targetTask.priority) {
    await tasksStore.updatePriority(source, targetTask.priority)
  } else {
    const dropBefore = true // for now, assume "above" target; can be replaced with actual mouse position later
    await tasksStore.moveTaskRelative(source, targetTask, dropBefore)
  }

  draggedTask.value = null
}

const auth = useAuthStore()
const router = useRouter()
const createTaskEdit = ref<any>(null)

function closeModal() {
  const modal = document.getElementById('create') as HTMLDialogElement
  modal?.close()
}

function handleModalClose() {
  createTaskEdit.value?.resetForm()
}

// Fetches tasks when the component is mounted; redirects to login on failure
onMounted(async () => {
  try {
    await tasksStore.fetchTasks()
  } catch {
    auth.logout()
    router.push('/login')
  }
})
</script>

<!-- View Template: Renders the task list, add task button, and modals -->
<template>
  <div>
    <!-- Main tasks container -->
    <div class="bg-base-200 border-base-300 rounded-box w-xl border p-4">
      
      <!-- Header section with loading indicator and title -->
      <div class="mb-6 cursor-default">
        <div class="flex items-center">
          <div class="mr-4">
            <span v-if="tasksStore.loading" class="loading loading-spinner w-10 text-primary" />
            <button v-else class="btn btn-square btn-soft btn-primary shadow-md" onclick="create.showModal()">
              <Plus />
            </button>
          </div>
          <h1 class="text-xl text-heading">My Tasks</h1>
        </div>
      </div>
      
      <!-- Skeleton loader shown while tasks are loading -->
      <div v-if="tasksStore.loading" class="skeleton w-full h-96" />

      <!-- Content area for task lists -->
      <div v-else class="min-h-96">
        <!-- List of pinned tasks -->
        <ul v-if="pinnedTasks.length" class="list bg-base-100 rounded-box shadow-md border-2" :class="anyTaskDueToday ? 'border-info/50' : 'border-white/50'">
          <TaskItem
              v-for="task in pinnedTasks"
              :key="task.id"
              :task="task"
          />
        </ul>
        
        <!-- List of incomplete (regular) tasks grouped by priority with drag and drop support -->
        <template v-for="(group, index) in tasksByPriority" :key="group.priority.value">
          <div v-if="index === 0 && pinnedTasks.length" class="divider" />
          <div v-else-if="index > 0" class="h-6" />
          
          <div v-if="group.priority.value !== 0" class="flex items-center gap-2 mb-2 px-2 text-xs font-bold uppercase tracking-wider" :class="group.priority.textClass">
            <component :is="group.priority.icon" class="w-4 h-4" />
            {{ group.priority.label }}
          </div>
          
          <ul class="drop-zone list bg-base-100 rounded-box shadow-md" :class="[group.priority.value !== 0 ? 'border-2 ' + group.priority.borderColor : '']">
            <TaskItem
                v-for="task in group.tasks"
                :key="task.id"
                :task="task"
                @drag-start="onDragStart(task)"
                @drop="onDrop(task)"
            />
          </ul>
        </template>

        <!-- Empty state message when no tasks exist -->
        <div v-if="!tasksStore.tasks.length" class="card bg-base-300 rounded-box grid h-96 place-items-center">
          <div class="opacity-50 flex flex-col items-center text-center gap-2">
            <Rocket class="w-24 h-24"/>
            <b>No tasks yet</b>
            <p class="text-sm">Add a task to get started</p>
          </div>
        </div>
        
        <!-- Section for completed tasks -->
        <div v-else-if="completedTasks.length">
          
          <!-- Encouragement message when all tasks are done -->
          <div v-if="!incompleteTasks.length && !pinnedTasks.length" class="card bg-base-300 rounded-box grid h-24 place-items-center">
            <div class="flex opacity-50">
              <ListChecks class="mr-2"/> All tasks completed!
            </div>
          </div>
          
          <!-- Collapsible section for completed tasks list -->
          <div class="collapse collapse-arrow">
            <input type="checkbox" name="completed-tasks-list" />
            <div class="collapse-title opacity-50">Completed ({{ completedTasks.length }})</div>
            <!-- TODO: fix bottom padding issue -->
            <div class="collapse-content p-0 pb-6">
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

  <!-- Modal dialog for creating new tasks -->
  <dialog id="create" class="modal" @close="handleModalClose">
    <div class="modal-box w-2xl bg-base-200 border border-base-300 p-4">
      <div class="flex flex-row items-center ml-4 gap-2 opacity-60">
        <CircleCheckBig /> <h3 class="text-lg font-bold">New Task</h3>
      </div>
      <TaskEdit ref="createTaskEdit" @close="closeModal" />
    </div>

    <!-- Backdrop to close the modal when clicking outside -->
    <form method="dialog" class="modal-backdrop">
      <button>close</button>
    </form>
  </dialog>
</template>
