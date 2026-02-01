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
  <form class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4" @submit.prevent="submit">
    <fieldset class="fieldset">
      <label class="label">Email</label>
      <input type="email" class="input validator" placeholder="Email" required v-model="email" />
      <p class="validator-hint hidden">Required</p>
    </fieldset>

    <label class="fieldset">
      <span class="label">Password</span>
      <input type="password" class="input validator" placeholder="Password" required v-model="password" />
      <span class="validator-hint hidden">Required</span>
    </label>

    <button class="btn btn-neutral mt-4" type="submit">Login</button>
    <a class="btn btn-ghost mt-1" href="/register">Create Account</a>
  </form>
</template>