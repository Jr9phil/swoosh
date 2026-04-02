<!-- 
  RegisterView.vue
  Provides a registration form for new users to create an account.
-->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { Eye, EyeOff } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)

const passwordInput = ref<HTMLInputElement | null>(null)
const confirmPasswordInput = ref<HTMLInputElement | null>(null)

const error = ref<string | null>(null)
const loading = ref(false)

// Clear confirmation password if new password is cleared
watch(password, (newValue) => {
  if (newValue === '') {
    confirmPassword.value = ''
    showPassword.value = false
  }
})

// Computes whether the password and confirmation password match
const passwordMismatch = computed(() => 
    confirmPassword.value.length > 0 &&
    password.value !== confirmPassword.value
)

// Computes whether all required fields have been filled out
const fieldsEntered = computed(() => 
    email.value.length > 0 &&
    password.value.length > 0 &&
    confirmPassword.value.length > 0
)

// Handles form submission to register a new user
async function submit() {
  error.value = null
  loading.value = true
  showPassword.value = false
  
  try {
    await auth.register(email.value, password.value)
    router.push('/')
  } catch (err) {
    if (axios.isAxiosError(err)) {
      const status = err.response?.status
      if (status === 500) {
        error.value = 'Internal Server Error'
      } else {
        error.value = 'Registration Failed'
      }
    } else {
      error.value = 'Unexpected error occurred'
    }
  } finally {
    loading.value = false
  }

}

// Auto-focus
function focusPassword() {
  passwordInput.value?.focus()
}

function focusConfirmPassword() {
  confirmPasswordInput.value?.focus()
}
</script>

<!-- View Template: Registration form with email, password, and password confirmation -->
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
        <p class="auth-page-subtitle">Start organizing today</p>
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
              <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Min 8 characters" required v-model="password" minlength="8" />
              <button type="button" class="pw-toggle" @click="showPassword = !showPassword">
                <Eye v-if="!showPassword" :size="18" />
                <EyeOff v-else :size="18" />
              </button>
            </div>
          </div>

          <div class="auth-field">
            <label class="auth-label">Confirm Password</label>
            <div class="relative">
              <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Confirm password" required v-model="confirmPassword" :class="{ 'border-error': passwordMismatch }" />
              <button type="button" class="pw-toggle" @click="showPassword = !showPassword">
                <Eye v-if="!showPassword" :size="18" />
                <EyeOff v-else :size="18" />
              </button>
            </div>
            <span v-if="passwordMismatch" class="text-error text-[11px] ml-1">Passwords do not match</span>
          </div>

          <div v-if="error" class="text-error text-[13px] font-medium text-center mt-1">
            {{ error }}
          </div>

          <button type="submit" class="auth-submit-btn mt-2" :disabled="loading || passwordMismatch || !fieldsEntered">
            {{ loading ? 'Creating account...' : 'Create Account' }}
          </button>
        </form>
      </div>

      <div class="flex justify-center mt-5">
        <router-link to="/login" class="text-[11px] font-mono font-bold tracking-widest uppercase text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Sign In</router-link>
      </div>
    </div>
  </main>
</template>