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
    <header style="margin-bottom: 1rem">
      <button v-if="auth.token" @click="logout">
        Logout
      </button>
    </header>

    <div class="content">
      <router-view />
    </div>
  </div>
</template>

<style>
  body {
    font-family: system-ui, sans-serif;
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
  .content {
    border-radius: 2rem;
    background-color: #1a1a1a;
    padding: 2rem;
    padding-top: 2.5rem;
  }
  
</style>
