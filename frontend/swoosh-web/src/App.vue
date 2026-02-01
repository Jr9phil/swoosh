<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div>
    <header>
      <button class="btn btn-neutral" v-if="auth.token" @click="logout">
        Logout
      </button>
    </header>

    <div class="flex w-full justify-center">
      <router-view />
    </div>
  </div>
</template>

<style>
  body {
    font-family: system-ui, sans-serif;
    margin: 0.4em;
  }
  
  button {
    margin-left: 0.25rem;
    cursor: pointer;
  }
  
  ul {
    list-style: none;
    padding: 0;
  }
  
  li {
    padding: 0.75rem;
    margin-bottom: 0.5rem;
    border-radius: 4px;
  }
  
</style>
