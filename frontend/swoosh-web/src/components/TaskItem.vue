<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import { ref } from 'vue'
import { Trash2 } from 'lucide-vue-next'

const props = defineProps<{
  task: Task
}>()

const originalTitle = ref(props.task.title)
const originalNotes = ref(props.task.notes ?? '')

const editing = ref(false)
const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')

const tasksStore = useTasksStore()

function formattedDeadline() {
  if (!props.task.deadline) return null
  return new Date(props.task.deadline).toLocaleDateString()
}
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    e.preventDefault()
    finishEditing()
  }

  if (e.key === 'Escape') {
    editedTitle.value = originalTitle.value
    editedNotes.value = originalNotes.value
    editing.value = false
  }
}
function startEditing() {
  editing.value = true
  editedTitle.value = props.task.title
  editedNotes.value = props.task.notes ?? ''
  originalTitle.value = props.task.title
  originalNotes.value = props.task.notes ?? ''
}
async function toggleComplete() {
  await tasksStore.toggleComplete(props.task)
}
async function finishEditing() {
  if (!editing.value) return

  editing.value = false
  
  if (
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value
  ) {
    return
  }
  
  if (!editedTitle.value.trim()) {
    editedTitle.value = originalTitle.value
    editedNotes.value = originalNotes.value
    return
  }

  await tasksStore.editTask(props.task.id, {
    title: editedTitle.value.trim(),
    notes: editedNotes.value || null
  })
}
async function remove() {
  if (confirm('Delete this task?')) {
    await tasksStore.deleteTask(props.task.id)
  }
}
</script>

<template>
  <li v-if="editing" class="list-row">
    <div><input
        type="checkbox"
        :checked="task.isCompleted"
        :class="task.isCompleted ? 'checkbox checkbox-primary' : 'checkbox'"
        disabled 
    /></div>

    <div class="flex flex-col gap-2 w-full">
      <input
          ref="titleInput"
          type="text"
          class="input input-bordered"
          v-model="editedTitle"
          @blur="finishEditing"
          @keydown="onKeydown"
          autofocus
      />

      <textarea
          class="textarea textarea-bordered"
          placeholder="Notes"
          v-model="editedNotes"
          @blur="finishEditing"
          @keydown="onKeydown"
      />
    </div>
    <button class="btn btn-soft btn-square btn-error" @click="remove"><Trash2 /></button>
  </li>
  <li v-else class="list-row">
      <div><input
          type="checkbox"
          :checked="task.isCompleted"
          @change="toggleComplete"
          :class="task.isCompleted ? 'checkbox checkbox-primary' : 'checkbox'"
      /></div>
    
    <div @click="startEditing" class="cursor-text hover:underline">
      <h1 class="text-base font-semibold">
        {{ task.title }}
      </h1>
    </div>
  </li>
</template>
