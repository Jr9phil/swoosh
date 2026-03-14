<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import { Github, User, LogOut, KeyRound } from 'lucide-vue-next'

const auth = useAuthStore()
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
      </div>
    </footer>
  </div>
</template>
