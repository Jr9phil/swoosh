<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'

const email = ref('')
const password = ref('')
const auth = useAuthStore()
const router = useRouter()

async function submit() {
  await auth.login(email.value, password.value)
  router.push('/')
}
</script>

<template>
  <div class="container" id="login">
      <form @submit.prevent="submit">
        <div>
          <input v-model="email" placeholder="Email" />
        </div>
        <div>
          <input v-model="password" type="password" placeholder="Password" />
        </div>
        <div>
          <a href="/register">Create Account</a>
          <button>Login</button>
        </div>
      </form>
  </div>
</template>