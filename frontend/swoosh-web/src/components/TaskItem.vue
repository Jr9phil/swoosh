<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import { ref } from 'vue'

const props = defineProps<{
  task: Task
}>()

const editing = ref(false)
const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')

const tasksStore = useTasksStore()

function formattedDeadline() {
  if (!props.task.deadline) return null
  return new Date(props.task.deadline).toLocaleDateString()
}
async function toggleComplete() {
  await tasksStore.toggleComplete(props.task)
}
async function save() {
  await tasksStore.editTask(props.task.id, {
    title: editedTitle.value,
    notes: editedNotes.value || null
  })
  editing.value = false
}
async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
}
</script>

<template>
  <li>
    <label>
      <input
          type="checkbox"
          :checked="task.isCompleted"
          @change="toggleComplete"
      />

      <span v-if="editing">
        <input v-model="editedTitle" />
        <textarea v-model="editedNotes" />

        <button @click="save">Save</button>
        <button @click="editing = false">Cancel</button>
        <button @click="remove" style="margin-left: 0.5rem"> Delete </button>
      </span>
      
      <span v-else>
        <span>
          {{ task.title }}
        </span>
        <div v-if="task.deadline" style="font-size: 0.8rem; color: gray">
          Due: {{ formattedDeadline() }}
        </div>
        
        <button @click="editing = true">Edit</button>
        <button @click="remove" style="margin-left: 0.5rem"> Delete </button>
      </span>
    </label>
  </li>
</template>
