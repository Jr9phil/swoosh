<!-- 
  ChangePassword.vue
  Provides a form for authenticated users to change their account password.
-->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { Eye, EyeOff } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

const password = ref('')
const showPassword = ref(false)
const newPassword = ref('')
const confirmNewPassword = ref('')
const showNewPassword = ref(false)

const newPasswordInput = ref<HTMLInputElement | null>(null)
const confirmNewPasswordInput = ref<HTMLInputElement | null>(null)

// Clear confirmation password if new password is cleared
watch(newPassword, (newValue) => {
  if (newValue === '') {
    confirmNewPassword.value = ''
    showNewPassword.value = false
  }
})

const error = ref<string | null>(null)
const loading = ref(false)

// Computes whether the new password and confirmation password match
const passwordMismatch = computed(() =>
    confirmNewPassword.value.length > 0 &&
    newPassword.value !== confirmNewPassword.value
)

// Computes whether all required fields have been filled out
const fieldsEntered = computed(() =>
    password.value.length > 0 &&
    newPassword.value.length > 0 &&
    confirmNewPassword.value.length > 0
)

// Handles form submission to change the user's password
async function submit() {
  error.value = null
  loading.value = true
  showPassword.value = false
  showNewPassword.value = false
  
  try {
    await auth.changePassword(password.value, newPassword.value)
    router.push('/')
  } catch (err) {
    if (axios.isAxiosError(err)) {
      const status = err.response?.status
      if (status === 400) {
        error.value = 'Current Password is Incorrect'
      } else if (status === 500) {
        error.value = 'Internal Server Error'
      } else {
        error.value = 'Change Password Failed'
      }
    } else {
      error.value = 'Unexpected error occurred'
    }
  } finally {
    loading.value = false
  }

}

// Auto-focus
function focusNewPassword() {
  newPasswordInput.value?.focus()
}

function focusConfirmNewPassword() {
  confirmNewPasswordInput.value?.focus()
}
</script>

<!-- View Template: Password change form with current, new, and confirm password fields -->
<template>
  <main class="flex-1 flex justify-center pt-20 px-5">
    <div class="w-full max-w-[360px]">
      <header class="mb-8 text-center">
        <h1 class="auth-page-title">Security</h1>
        <p class="auth-page-subtitle">Change your password</p>
      </header>

      <form class="flex flex-col gap-4" @submit.prevent="submit">
        <div class="auth-field">
          <label class="auth-label">Current Password</label>
          <div class="relative">
            <input :type="showPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="••••••••" required v-model="password" />
            <button type="button" class="pw-toggle" @click="showPassword = !showPassword">
              <Eye v-if="!showPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
        </div>

        <div class="auth-field">
          <label class="auth-label">New Password</label>
          <div class="relative">
            <input :type="showNewPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Min 8 characters" required v-model="newPassword" minlength="8" />
            <button type="button" class="pw-toggle" @click="showNewPassword = !showNewPassword">
              <Eye v-if="!showNewPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
        </div>

        <div class="auth-field">
          <label class="auth-label">Confirm New Password</label>
          <div class="relative">
            <input :type="showNewPassword ? 'text' : 'password'" class="swoosh-input w-full pr-10" placeholder="Confirm new password" required v-model="confirmNewPassword" :class="{ 'border-error': passwordMismatch }" />
            <button type="button" class="pw-toggle" @click="showNewPassword = !showNewPassword">
              <Eye v-if="!showNewPassword" :size="18" />
              <EyeOff v-else :size="18" />
            </button>
          </div>
          <span v-if="passwordMismatch" class="text-error text-[11px] ml-1">Passwords do not match</span>
        </div>

        <div v-if="error" class="text-error text-[13px] font-medium text-center mt-1">
          {{ error }}
        </div>

        <div class="flex flex-col gap-2 mt-2">
          <button type="submit" class="auth-submit-btn rounded-sm" :disabled="loading || passwordMismatch || !fieldsEntered">
            {{ loading ? 'Updating...' : 'Update Password' }}
          </button>
          <router-link to="/" class="w-full text-center py-2 text-[13px] text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Cancel</router-link>
          <router-link to="/deleteAccount" class="w-full text-center py-2 text-[13px] text-red-500 hover:text-red-400 transition-colors">Delete Account</router-link>
        </div>
      </form>
    </div>
  </main>
</template>