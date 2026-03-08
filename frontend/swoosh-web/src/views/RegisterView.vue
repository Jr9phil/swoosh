<!-- 
  RegisterView.vue
  Provides a registration form for new users to create an account.
-->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
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
  <main class="flex-1 flex justify-center pt-20 px-5">
    <div class="w-full max-w-[360px]">
      <header class="mb-8 text-center">
        <h1 class="text-[24px] font-extrabold tracking-tight text-swoosh-text">Join Swoosh</h1>
        <p class="text-[13px] text-swoosh-text-faint font-mono uppercase tracking-widest mt-1">Start organizing today</p>
      </header>

      <form class="flex flex-col gap-4" @submit.prevent="submit">
        <div class="flex flex-col gap-1.5">
          <label class="text-[11px] font-bold font-mono tracking-widest uppercase text-swoosh-text-faint ml-1">Email</label>
          <input type="email" class="swoosh-input" placeholder="Enter your email" required v-model="email" />
        </div>

        <div class="flex flex-col gap-1.5">
          <label class="text-[11px] font-bold font-mono tracking-widest uppercase text-swoosh-text-faint ml-1">Password</label>
          <div class="relative">
            <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Min 8 characters" required v-model="password" minlength="8" />
            <button type="button" class="absolute right-2 top-1/2 -translate-y-1/2 text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors" @click="showPassword = !showPassword">
              <Eye v-if="!showPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
        </div>

        <div class="flex flex-col gap-1.5">
          <label class="text-[11px] font-bold font-mono tracking-widest uppercase text-swoosh-text-faint ml-1">Confirm Password</label>
          <div class="relative">
            <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Confirm password" required v-model="confirmPassword" :class="{ 'border-swoosh-danger': passwordMismatch }" />
            <button type="button" class="absolute right-2 top-1/2 -translate-y-1/2 text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors" @click="showPassword = !showPassword">
              <Eye v-if="!showPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
          <span v-if="passwordMismatch" class="text-swoosh-danger text-[11px] ml-1">Passwords do not match</span>
        </div>

        <div v-if="error" class="text-swoosh-danger text-[13px] font-medium text-center mt-1">
          {{ error }}
        </div>

        <button type="submit" class="w-full bg-swoosh-text text-swoosh-bg py-3.5 rounded-sm font-extrabold text-[15px] mt-2 hover:scale-[1.01] active:scale-[0.99] transition-all disabled:opacity-50" :disabled="loading || passwordMismatch || !fieldsEntered">
          {{ loading ? 'Creating account...' : 'Create Account' }}
        </button>

        <div class="flex justify-center mt-4">
          <router-link to="/login" class="text-[13px] text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Already have an account? Sign In</router-link>
        </div>
      </form>
    </div>
  </main>
</template>