<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import { UserRound, LogOut, Sun, Moon, Github } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  if (confirm('Log out?')) {
    auth.logout()
    router.push('/login')
  }
}
</script>

<template>
  <div id="app" class="min-h-screen grid grid-rows-[auto_1fr_auto]">
    <header class="flex flex-container p-4">
      <a href="https://github.com/Jr9phil/swoosh" target="_blank">
        <Github class="logo" />
      </a>
      <div class="flex-grow"/>
      <label class="toggle text-base-content">

        <input type="checkbox" checked="checked" class="theme-controller" name="theme" value="dark" />
        <Sun class="h-4 w-4 fill-current" />
        <Moon class="h-4 w-4 fill-current" />

      </label>
    </header>
    <div class="flex items-center justify-center">
      <router-view />
    </div>
    
    <footer v-if="auth.token">
      <div class="fab">
        <div class="tooltip tooltip-left" :data-tip="auth.currentUser || 'user'">
          <div tabindex="0" role="button" class="btn btn-lg btn-circle btn-info"><UserRound /></div>
        </div>
        
        <div class="fab-close"><span class="btn btn-circle btn-lg btn-error">✕</span></div>
        
        <div>Logout <button class="btn btn-lg btn-circle" @click="logout"><LogOut /></button></div>
      </div>
    </footer>
  </div>
</template>
