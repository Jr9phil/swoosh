<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { Eye, EyeOff } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const showPassword = ref(false)

const error = ref<string | null>(null)
const loading = ref(false)

async function submit() {
  error.value = null
  loading.value = true
  
  try {
    await auth.login(email.value, password.value)
    router.push('/')
  } catch (err) {
    if (axios.isAxiosError(err)) {
      const status = err.response?.status
      if (status === 401) {
        error.value = 'Invalid email or password'
      } else if (status === 500) {
        error.value = 'Internal Server Error'
      } else {
        error.value = 'Login Failed'
      }
    } else {
      error.value = 'Unexpected error occurred'
    }
  } finally {
    loading.value = false
  }

}
</script>

<template>
  <form class="fieldset bg-base-200 border-base-300 rounded-box w-sm border p-8" @submit.prevent="submit">
    <fieldset class="fieldset">
      <label class="label">Email</label>
      <input type="email" class="input validator" placeholder="Email" required v-model="email" />
      <p class="validator-hint hidden">Required</p>
    </fieldset>

    <label class="fieldset">
      <span class="label">Password</span>
      <div class="join">
        <input :type="showPassword ? 'text' : 'password'" class="input validator join-item" placeholder="Password" required v-model="password" />
        <button
            type="button"
            class="btn btn-soft btn-square join-item"
            @click="showPassword = !showPassword"
            tabindex="-1"
        >
          <Eye v-if="!showPassword" class="w-4 h-4" />
          <EyeOff v-else class="w-4 h-4" />
        </button>
      </div>
      <span class="validator-hint hidden">Required</span>
    </label>

    <div v-if="error" class="alert alert-error alert-soft mt-2">
      {{ error }}
    </div>

    <button class="btn btn-neutral mt-4" :disabled="loading" type="submit"><span v-if="loading" class="loading loading-spinner loading-sm"></span>{{ loading ? 'Logging in...' : 'Login' }}</button>
    <a v-if="!loading" class="btn btn-primary mt-1" href="/register">Create Account</a>
  </form>
</template>