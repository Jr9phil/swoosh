<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import { ref } from 'vue'
import { Trash2, GripVertical } from 'lucide-vue-next'

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
  <li v-if="editing" class="list-row" v-click-outside="finishEditing">
    <div><input
        type="checkbox"
        :checked="task.isCompleted"
        :class="task.isCompleted ? 'checkbox checkbox-primary' : 'checkbox' "
        disabled 
    /></div>

    <div class="flex flex-col gap-2 w-full">
      <input
          ref="titleInput"
          type="text"
          class="input input-bordered text-base font-semibold"
          maxlength="200"
          v-model="editedTitle"
          @keydown="onKeydown"
          autofocus
      />

      <textarea
          class="textarea textarea-bordered"
          placeholder="Notes"
          maxlength="1000"
          v-model="editedNotes"
          @keydown="onKeydown"
      />
    </div>
    <button class="btn btn-ghost btn-circle btn-sm hover:btn-error" @click="remove"><Trash2 :size="16"/></button>
  </li>
  <li v-else class="list-row group">
      <div><input
          type="checkbox"
          :checked="task.isCompleted"
          @change="toggleComplete"
          class="checkbox hover:checkbox-primary"
          :class="{ 'checkbox-primary' : task.isCompleted}"
      /></div>
    
    <div @click="startEditing" class="cursor-text">
      <h1 class="text-base" :class="task.isCompleted ? 'line-through opacity-70' : 'font-semibold'">
        {{ task.title }}
      </h1>
      <p v-if="!task.isCompleted" class="text-sm opacity-70 line-clamp-2"> {{ task.notes }}</p>
    </div>
    
    <div class="flex items-center justify-end cursor-grab">
      <GripVertical class="opacity-0 group-hover:opacity-50 transition-opacity duration-200"/>
    </div>
  </li>
</template>
