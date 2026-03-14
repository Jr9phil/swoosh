<!-- 
  LoginView.vue
  Provides a login form for existing users to authenticate.
-->
<script setup lang="ts">
import { ref, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { Eye, EyeOff } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const passwordInput = ref<HTMLInputElement | null>(null)
const showPassword = ref(false)

const error = ref<string | null>(null)
const loading = ref(false)

watch(password, (newValue) => {
  if (newValue === '') {
    showPassword.value = false
  }
})

// Handles form submission, attempting to authenticate the user
async function submit() {
  error.value = null
  loading.value = true
  showPassword.value = false
  
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

// Auto-focus password input
function focusPassword() {
  passwordInput.value?.focus()
}
</script>

<!-- View Template: Login form with email and password fields -->
<template>
  <main class="flex-1 flex justify-center pt-20 px-5">
    <div class="w-full max-w-[360px]">
      <header class="mb-8 text-center">
        <h1 class="text-[24px] font-extrabold tracking-tight text-base-content">Swoosh</h1>
        <p class="text-[13px] text-swoosh-text-faint font-mono uppercase tracking-widest mt-1">Ultraminimalist Tasks</p>
      </header>

      <form class="flex flex-col gap-4" @submit.prevent="submit">
        <div class="auth-field">
          <label class="auth-label">Email</label>
          <input type="email" class="swoosh-input" placeholder="Enter your email" required v-model="email" />
        </div>

        <div class="auth-field">
          <label class="auth-label">Password</label>
          <div class="relative">
            <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="••••••••" required v-model="password" />
            <button type="button" class="pw-toggle" @click="showPassword = !showPassword">
              <Eye v-if="!showPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
        </div>

        <div v-if="error" class="text-error text-[13px] font-medium text-center mt-1">
          {{ error }}
        </div>

        <button type="submit" class="auth-submit-btn rounded-sm mt-2" :disabled="loading">
          {{ loading ? 'Signing in...' : 'Sign In' }}
        </button>

        <div class="flex justify-center mt-4">
          <router-link to="/register" class="text-[13px] text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Don't have an account? Register</router-link>
        </div>
      </form>
    </div>
  </main>
</template>