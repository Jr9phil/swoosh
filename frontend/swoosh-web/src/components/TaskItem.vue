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
  <li v-if="editing">
    <fieldset class="fieldset">
      <input type="text" class="input validator" placeholder="Title" required v-model="editedTitle" />
      <p class="validator-hint hidden">Required</p>
    </fieldset>

    <label class="fieldset">
      <textarea class="textarea" placeholder="Notes" v-model="editedNotes" />
    </label>

    <button class="btn btn-outline btn-primary" @click="save">Save</button>
    <button class="btn btn-outline" @click="editing = false">Cancel</button>
    <button class="btn btn-outline btn-secondary" @click="remove"> Delete </button>
  </li>
  <li v-else class="list-row">
      <div><input
          type="checkbox"
          :checked="task.isCompleted"
          @change="toggleComplete"
          :class="task.isCompleted ? 'checkbox checkbox-primary' : 'checkbox'"
      /></div>
    
    <div><h1 class="text-base font-semibold" @click="editing = true">{{ task.title }}</h1></div>
  </li>
</template>
