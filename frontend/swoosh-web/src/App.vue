<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import { User, LogOut } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div>
    <div class="flex w-full justify-center">
      <router-view />
    </div>
    
    <footer v-if="auth.token">
      <div class="fab">
        <div class="tooltip tooltip-left" :data-tip="auth.currentUser || 'user'">
          <div tabindex="0" role="button" class="btn btn-lg btn-circle btn-info"><User /></div>
        </div>

        
        <div class="fab-close"><span class="btn btn-circle btn-lg btn-error">✕</span></div>
        
        <div>Logout <button class="btn btn-lg btn-circle" @click="logout"><LogOut /></button></div>
      </div>
    </footer>
  </div>
</template>
