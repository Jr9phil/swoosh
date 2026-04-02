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
  <main class="flex-1 flex justify-center items-center px-5 py-10">
    <div class="w-full max-w-[340px]">
      <header class="mb-7 text-center">
        <div class="auth-deco-row">
          <span class="auth-deco-line"></span>
          <span class="auth-deco-diamond">◆</span>
          <span class="auth-deco-line"></span>
        </div>
        <h1 class="auth-page-title">Swoosh</h1>
        <p class="auth-page-subtitle">Reprogram your brain</p>
      </header>

      <div class="auth-card">
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

          <button type="submit" class="auth-submit-btn mt-2" :disabled="loading">
            {{ loading ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>
      </div>

      <div class="flex justify-center mt-5">
        <router-link to="/register" class="text-[11px] font-mono font-bold tracking-widest uppercase text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Register</router-link>
      </div>
    </div>
  </main>
</template>