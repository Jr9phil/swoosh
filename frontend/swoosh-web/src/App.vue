<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useTasksStore } from './stores/tasks'
import { useUiStore } from './stores/ui'
import { useRouter, RouterLink } from 'vue-router'
import { LogOut, KeyRound, Download, PanelLeft, ChevronUp, SquareCheckBig, Plus, CalendarSync, Lightbulb, BellRing, Archive, ChartColumn, Network } from 'lucide-vue-next'
import { onMounted, onUnmounted } from 'vue'
import CreateModal from './components/CreateModal.vue'

const auth = useAuthStore()
const tasksStore = useTasksStore()
const ui = useUiStore()
const router = useRouter()

let scrollTimer: number | null = null

// Shows scrollbars when scrolling starts and hides them after 1 second of inactivity
function handleScroll() {
  document.documentElement.classList.add('is-scrolling')
  if (scrollTimer) clearTimeout(scrollTimer)
  scrollTimer = window.setTimeout(() => {
    document.documentElement.classList.remove('is-scrolling')
  }, 1000)
}

function getSidebarCheckbox() {
  return document.getElementById('sidebar') as HTMLInputElement | null
}

function handleKeydown(e: KeyboardEvent) {
  if (!auth.token || !e.ctrlKey) return
  // Ctrl++ (= or + key) → expand sidebar
  if (e.key === '=' || e.key === '+') {
    e.preventDefault()
    const cb = getSidebarCheckbox()
    if (cb) cb.checked = true
  }
  // Ctrl+- → collapse sidebar
  if (e.key === '-') {
    e.preventDefault()
    const cb = getSidebarCheckbox()
    if (cb) cb.checked = false
  }
}

onMounted(() => {
  window.addEventListener('scroll', handleScroll, { passive: true, capture: true })
  window.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll, { capture: true })
  window.removeEventListener('keydown', handleKeydown)
})

// Navigate to tasks view and close sidebar on mobile
function goHome() {
  router.push('/')
  closeSidebarOnMobile()
}

// Closes sidebar on mobile after nav
function closeSidebarOnMobile() {
  if (window.innerWidth < 1024) {
    const cb = getSidebarCheckbox()
    if (cb) cb.checked = false
  }
}

// Triggers the create task modal (watched by TasksView)
function openCreateModal() {
  ui.triggerCreateModal()
  closeSidebarOnMobile()
}

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
    <div :class="auth.token ? 'drawer lg:drawer-open' : ''">
      <input v-if="auth.token" id="sidebar" type="checkbox" class="drawer-toggle" />
      <div :class="auth.token ? 'drawer-content' : ''">
        <router-view />
      </div>

      <div v-if="auth.token" class="drawer-side is-drawer-close:overflow-visible">
        <label for="sidebar" aria-label="close sidebar" class="drawer-overlay"></label>
        <div class="flex min-h-full flex-col items-start bg-base-200 is-drawer-close:w-14 is-drawer-open:w-64 border-r border-swoosh">
          <ul class="menu w-full grow">
            <li>
              <label for="sidebar" class="hover:cursor-pointer">
                <span class="sidebar-brand is-drawer-close:hidden" @click.stop="goHome">Swoosh</span>
                <PanelLeft class="flex-shrink-0" />
              </label>
            </li>
            <div class="divider" />

            <!-- Add button: "Add" label + circular plus -->
            <li class="sidebar-add-item">
              <button class="sidebar-add-btn hover:cursor-pointer" @click="openCreateModal">
                <span class="sidebar-add-circle">
                  <Plus class="w-3.5 h-3.5" />
                </span>
                <span class="sidebar-add-label is-drawer-close:hidden">Add</span>
              </button>
            </li>

            <li>
              <RouterLink to="/" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <SquareCheckBig /> <span class="is-drawer-close:hidden">Tasks</span>
              </RouterLink>
            </li>
            <li>
              <RouterLink to="/recurring" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <CalendarSync /> <span class="is-drawer-close:hidden">Recurring</span>
              </RouterLink>
            </li>
            <li>
              <RouterLink to="/noteboard" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <Lightbulb /> <span class="is-drawer-close:hidden">Noteboard</span>
              </RouterLink>
            </li>
            <li>
              <RouterLink to="/reminders" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <BellRing /> <span class="is-drawer-close:hidden">Reminders</span>
              </RouterLink>
            </li>
            <div class="divider" />
            <li>
              <RouterLink to="/progress" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <ChartColumn /> <span class="is-drawer-close:hidden">Progress</span>
              </RouterLink>
            </li>
            <li>
              <RouterLink to="/skilltree" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <Network /> <span class="is-drawer-close:hidden">Skill Tree</span>
              </RouterLink>
            </li>
            <li>
              <RouterLink to="/archive" @click="closeSidebarOnMobile" class="hover:cursor-pointer">
                <Archive /> <span class="is-drawer-close:hidden">Archive</span>
              </RouterLink>
            </li>
          </ul>

          <!-- Sidebar footer: user section with dropup actions -->
          <footer v-if="auth.token" class="w-full border-t border-swoosh">
            <div class="dropdown dropdown-top w-full">
              <div tabindex="0" role="button" class="sidebar-user-btn">
                <div class="sidebar-user-avatar">{{ auth.currentUser?.[0]?.toUpperCase() }}</div>
                <span class="is-drawer-close:hidden sidebar-user-name truncate flex-1 min-w-0">{{ auth.currentUser }}</span>
                <ChevronUp class="is-drawer-close:hidden w-3 h-3 flex-shrink-0 sidebar-user-chevron" />
              </div>
              <ul tabindex="0" class="sidebar-user-menu dropdown-content menu">
                <li class="is-drawer-open: hidden is-drawer-close:flex sidebar-user-name">
                  <div>
                    {{ auth.currentUser }}
                  </div>
                </li>
                <li>
                  <button @click="exportCsv">
                    <Download class="w-4 h-4" />
                    Export CSV
                  </button>
                </li>
                <li>
                  <button @click="changePassword">
                    <KeyRound class="w-4 h-4" />
                    Change Password
                  </button>
                </li>
                <li class="sidebar-user-menu-divider">
                  <button @click="logout">
                    <LogOut class="w-4 h-4" />
                    Logout
                  </button>
                </li>
              </ul>
            </div>
          </footer>
        </div>
      </div>

    </div>

    <!-- Global create modal (always mounted when logged in) -->
    <CreateModal v-if="auth.token" />
  </div>
</template>
