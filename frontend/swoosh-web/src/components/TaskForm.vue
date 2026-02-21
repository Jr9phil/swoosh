<!-- 
  TaskForm.vue
  A form component for creating new tasks.
  Provides inputs for title, notes, and handles the creation logic via the tasks store.
-->
<script setup lang="ts">
import { ref } from 'vue'
import { useTasksStore } from '../stores/tasks'

const tasksStore = useTasksStore()

const title = ref('')
const notes = ref('')
const deadline = ref('')

const error = ref<string | null>(null)
const loading = ref(false)

// Handles form submission to create a new task
async function submit() {
  if (!title.value.trim()) {
    error.value = 'Title is required'
    return
  }

  loading.value = true
  error.value = null

  try {
    await tasksStore.createTask({
      title: title.value,
      notes: notes.value || null,
      deadline: deadline.value || null
    })

    // Reset form fields after successful creation
    title.value = ''
    notes.value = ''
    deadline.value = ''
  } catch {
    error.value = 'Failed to create task'
  } finally {
    loading.value = false
  }
}
</script>

<!-- Component Template: Form for task creation -->
<template>
  <form method="dialog" class="fieldset p-4" @submit="submit">
    <h1>New Task</h1>
    <!-- Task title input field -->
    <fieldset class="fieldset">
      <input 
          type="text" 
          class="input validator" 
          placeholder="Title"
          maxlength="200"
          required 
          v-model="title" />
      <p class="validator-hint hidden">Required</p>
    </fieldset>

    <!-- Task notes textarea field -->
    <label class="fieldset">
      <textarea 
          class="textarea" 
          placeholder="Notes"
          maxlength="1000"
          v-model="notes" />
      <p class="label">Optional</p>
    </label>
    
    <!-- Error message display if task creation fails -->
    <div v-if="error" style="color: red">
      {{ error }}
    </div>
    
    <!-- Submission button for adding the task -->
    <button class="btn btn-neutral mt-4" type="submit" :disabled="loading">
      {{ loading ? 'Creating…' : 'Add Task' }}
    </button>
  </form>
</template>
