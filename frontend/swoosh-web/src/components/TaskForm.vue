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
  <div id="login">
    <form @submit.prevent="submit">
      <h2>Create Task</h2>
  
      <div>
        <input
            v-model="title"
            placeholder="Title"
            required
        />
      </div>
      <div>
        <textarea
            v-model="notes"
            placeholder="Notes (optional)"
        />
      </div>
      <div>
        <input
            type="datetime-local"
            v-model="deadline"
        />
      </div>
      <div v-if="error" style="color: red">
        {{ error }}
      </div>
      <button type="submit" :disabled="loading">
        {{ loading ? 'Creating…' : 'Add Task' }}
      </button>
    </form>
  </div>
</template>
