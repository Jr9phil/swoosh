<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useTasksStore } from './stores/tasks'
import { useRouter } from 'vue-router'
import { Github, User, LogOut, KeyRound, Download } from 'lucide-vue-next'

const auth = useAuthStore()
const tasksStore = useTasksStore()
const router = useRouter()

// Logs the user out after confirmation and redirects to the login page
function logout() {
  if (confirm('Log out?')) {
    auth.logout()
    router.push('/login')
  }
}

// Redirects the user to the change password page
function changePassword() {
  router.push('/changePassword')
}

// Escapes a value for CSV output
function csvEscape(val: string): string {
  if (/[",\n\r]/.test(val)) {
    return '"' + val.replace(/"/g, '""') + '"'
  }
  return val
}

// Exports all decrypted tasks to a CSV file, prompting the user to choose a save location
async function exportCsv() {
  const headers = ['ID', 'Title', 'Notes', 'Completed', 'Deadline', 'Pinned', 'Priority', 'Rating', 'Icon', 'Created At']
  const rows = tasksStore.tasks.map(t => [
    t.id,
    csvEscape(t.title),
    csvEscape(t.notes ?? ''),
    t.completed ?? '',
    t.deadline ?? '',
    t.pinned ? 'true' : 'false',
    t.priority.toString(),
    t.rating.toString(),
    t.icon?.toString() ?? '',
    t.createdAt,
  ])

  const csv = [headers, ...rows].map(row => row.join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })

  if ('showSaveFilePicker' in window) {
    try {
      const handle = await (window as any).showSaveFilePicker({
        suggestedName: 'swoosh-tasks.csv',
        types: [{ description: 'CSV Files', accept: { 'text/csv': ['.csv'] } }],
      })
      const writable = await handle.createWritable()
      await writable.write(blob)
      await writable.close()
    } catch {
      // User cancelled the dialog — do nothing
    }
  } else {
    // Fallback for browsers without File System Access API
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'swoosh-tasks.csv'
    a.click()
    URL.revokeObjectURL(url)
  }
}
</script>

<template>
  <div id="app" class="min-h-screen flex flex-col">
    <header class="flex flex-container p-3 border-b border-swoosh">
      <a href="https://github.com/Jr9phil/swoosh" target="_blank">
        <Github class="logo" />
      </a>
    </header>
    <router-view />

    <!-- Application footer, visible only when the user is authenticated -->
    <footer v-if="auth.token">
      <!-- Floating Action Button (FAB) menu for user-related actions -->
      <div class="fab">
        <div>
          <div tabindex="0" role="button" class="btn btn-lg btn-circle btn-outline max-sm:btn-md"><User /></div>
        </div>

        <div class="fab-close">{{ auth.currentUser }}<span class="btn btn-circle btn-lg btn-outline max-sm:btn-md"><User /></span></div>

        <div>Logout <button class="btn btn-lg btn-circle max-sm:btn-md" @click="logout"><LogOut /></button></div>
        <div>Change Password <button class="btn btn-lg btn-circle max-sm:btn-md" @click="changePassword"><KeyRound /></button></div>
        <div>Export CSV <button class="btn btn-lg btn-circle max-sm:btn-md" @click="exportCsv"><Download /></button></div>
      </div>
    </footer>
  </div>
</template>
