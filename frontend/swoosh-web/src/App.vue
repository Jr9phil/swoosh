<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import { UserRound, LogOut, Sun, Moon, Github, KeyRound } from 'lucide-vue-next'

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
  <div id="app" class="min-h-screen grid grid-rows-[auto_1fr_auto]">
    <header class="flex flex-container bg-base-200 p-4">
      <!-- Github repository link -->
      <a href="https://github.com/Jr9phil/swoosh" target="_blank">
        <Github class="logo" />
      </a>
      <div class="flex-grow"/>
      <!-- Theme controller for switching between light and dark modes -->
      <label class="toggle text-base-content">

        <input type="checkbox" checked="checked" class="theme-controller" name="theme" value="dark" />
        <Sun class="h-4 w-4 fill-current" />
        <Moon class="h-4 w-4 fill-current" />

      </label>
    </header>
    <!-- Main content area where the routed views are rendered -->
    <div class="flex items-center justify-center">
      <router-view />
    </div>
    
    <!-- Application footer, visible only when the user is authenticated -->
    <footer v-if="auth.token">
      <!-- Floating Action Button (FAB) menu for user-related actions -->
      <div class="fab">
        <div class="tooltip tooltip-left" :data-tip="auth.currentUser || 'user'">
          <div tabindex="0" role="button" class="btn btn-lg btn-circle btn-info"><UserRound /></div>
        </div>
        
        <div class="fab-close">{{ auth.currentUser }}<span class="btn btn-circle btn-lg btn-info"><UserRound /></span></div>
        
        <div>Logout <button class="btn btn-lg btn-circle" @click="logout"><LogOut /></button></div>
        <div>Change Password <button class="btn btn-lg btn-circle" @click="changePassword"><KeyRound /></button></div>
      </div>
    </footer>
  </div>
</template>
