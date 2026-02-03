<script setup lang="ts">
import { ref } from 'vue'
import { useTasksStore } from '../stores/tasks'

const tasksStore = useTasksStore()

const title = ref('')
const notes = ref('')
const deadline = ref('')

const error = ref<string | null>(null)
const loading = ref(false)

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

    // reset form
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

<template>
  <form method="dialog" class="fieldset p-4" @submit="submit">
    <h1>New Task</h1>
    <fieldset class="fieldset">
      <input type="text" class="input validator" placeholder="Title" required v-model="title" />
    </fieldset>

    <label class="fieldset">
      <textarea class="textarea" placeholder="Notes" v-model="notes" />
      <p class="label">Optional</p>
    </label>
    
    <div v-if="error" style="color: red">
      {{ error }}
    </div>
    
    <button class="btn btn-neutral mt-4" type="submit" :disabled="loading">
      {{ loading ? 'Creating…' : 'Add Task' }}
    </button>
  </form>
</template>
