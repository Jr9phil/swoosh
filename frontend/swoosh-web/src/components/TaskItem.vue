<script setup lang="ts">
import type { Task } from '../types/task'
import { useTasksStore } from '../stores/tasks'
import TaskMenu from './TaskMenu.vue'
import { ref } from 'vue'
import { Trash2, GripVertical, EllipsisVertical, ListStart, Pin, PinOff, ChessPawn } from 'lucide-vue-next'

const props = defineProps<{
  task: Task
}>()

const originalTitle = ref(props.task.title)
const originalNotes = ref(props.task.notes ?? '')
const originalPinned = ref(props.task.pinned)

const editing = ref(false)
const editedTitle = ref(props.task.title)
const editedNotes = ref(props.task.notes ?? '')
const editedPinned = ref(props.task.pinned)

const tasksStore = useTasksStore()

function formattedDeadline() {
  if (!props.task.deadline) return null
  return new Date(props.task.deadline).toLocaleDateString()
}

function formattedCompletionDate() {
  if (!props.task.completed) return null
  return new Date(props.task.completed).toLocaleDateString()
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
  editPinned.value = props.task.pinned
  
  originalTitle.value = props.task.title
  originalNotes.value = props.task.notes ?? ''
}
async function toggleComplete() {
  if(props.task.completed) {
    if(!confirm('Mark task as incomplete?')) {
      return
    }
  }
  await tasksStore.toggleComplete(props.task)
}
async function togglePinned() {
  await tasksStore.togglePinned(props.task)
}
async function finishEditing() {
  if (!editing.value) return

  editing.value = false
  
  if (
      editedTitle.value === originalTitle.value &&
      editedNotes.value === originalNotes.value &&
      editedPinned.value === originalPinned.value
  ) {
    return
  }
  
  if (!editedTitle.value.trim()) {
    editedTitle.value = originalTitle.value
    editedNotes.value = originalNotes.value
    editedPinned.value = originalPinned.value
    return
  }

  await tasksStore.editTask(props.task.id, {
    title: editedTitle.value.trim(),
    notes: editedNotes.value || null,
    pinned: editedPinned.value
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
    <div class="flex flex-col">
      <input
        type="checkbox"
        :checked="!!task.completed"
        :class="task.completed ? 'checkbox checkbox-primary' : 'checkbox' "
        disabled 
      />
      
      <div class="flex-1 flex items-center justify-center cursor-grab group">
        <GripVertical class="opacity-10 group-hover:opacity-50 transition-opacity duration-200" />
      </div>
      
    </div>

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
    <div v-if="!task.completed" class="flex justify-end">
      <button
          id="priority"
          class="btn btn-ghost btn-circle opacity-60 hover:opacity-100">
        <ChessPawn />
      </button>
      
      <label class="swap btn btn-ghost btn-circle opacity-60 hover:opacity-100">
        <input type="checkbox" v-model="editedPinned" />
        <Pin class="swap-off" />
        <PinOff class="swap-on" />
      </label>
    </div>
    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          @delete="remove"
          @move-to-top=""
      />
    </div>
  </li>
  <li v-else class="list-row">
      <div><input
          type="checkbox"
          :checked="!!task.completed"
          @change="toggleComplete"
          class="checkbox hover:checkbox-primary"
          :class="{ 'checkbox-primary' : task.completed}"
      /></div>
    
    <div @click="startEditing" class="cursor-text">
      <h1 class="text-base" :class="task.completed ? 'line-through opacity-70' : 'font-semibold'">
        {{ task.title }}
      </h1>
      <p v-if="!task.completed" class="text-sm opacity-70 line-clamp-3"> {{ task.notes }}</p>
      <p v-else class="text-xs opacity-50 line-clamp-1">Completed on {{ formattedCompletionDate() }}</p>
    </div>

    <div v-if="!task.completed" class="flex justify-end group">
      <button 
          id="priority" 
          @click="startEditing"
          class="btn btn-ghost btn-circle opacity-0 group-hover:opacity-50"> 
        <ChessPawn />
      </button>
      <button 
          id="pin" 
          @click="togglePinned"
          class="btn btn-ghost btn-circle"
          :class="task.pinned ? '' : 'opacity-0 group-hover:opacity-50'">
        <Pin />
      </button>
    </div>

    <div class="flex justify-end">
      <TaskMenu
          :is-completed="!!task.completed"
          @delete="remove"
          @move-to-top=""
      />
    </div>
  </li>
</template>
