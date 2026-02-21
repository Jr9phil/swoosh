<!-- 
  ChangePassword.vue
  Provides a form for authenticated users to change their account password.
-->
<script setup lang="ts">
import { ref, computed } from 'vue'
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
const showConfNewPassword = ref(false)

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
  
  try {
    await auth.changePassword(password.value, newPassword.value)
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
</script>

<!-- View Template: Password change form with current, new, and confirm password fields -->
<template>
  <!-- Main password change form -->
  <div>
    <form class="fieldset bg-base-200 border-base-300 rounded-box w-sm border p-8" @submit.prevent="submit">
      <!-- Current password input field with visibility toggle -->
      <label class="fieldset">
        <span class="label">Current Password</span>
        <div class="join">
          <input :type="showPassword ? 'text' : 'password'" class="input validator join-item" placeholder="Password" required v-model="password" />
          <!-- Toggle button for current password visibility -->
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

      <!-- New password input field with visibility toggle -->
      <label class="fieldset">
        <span class="label">New Password</span>
        <div class="join">
          <input :type="showNewPassword ? 'text' : 'password'" class="input validator join-item" placeholder="New Password" required v-model="newPassword" minlength="8" />
          <!-- Toggle button for new password visibility -->
          <button
              type="button"
              class="btn btn-soft btn-square join-item"
              @click="showNewPassword = !showNewPassword"
              tabindex="-1"
          >
            <Eye v-if="!showNewPassword" class="w-4 h-4" />
            <EyeOff v-else class="w-4 h-4" />
          </button>
        </div>
      </label>

      <!-- Confirm new password input field with visibility toggle -->
      <label class="fieldset">
        <span class="label">Confirm New Password</span>
        <div class="join">
          <input
              :type="showConfNewPassword ? 'text' : 'password'"
              class="input join-item"
              placeholder="Confirm password"
              required
              v-model="confirmNewPassword"
              :class="{
          'input-error': passwordMismatch
        }"
          />
          <!-- Toggle button for confirmation password visibility -->
          <button
              type="button"
              class="btn btn-soft btn-square join-item"
              @click="showConfNewPassword = !showConfNewPassword"
              tabindex="-1"
          >
            <Eye v-if="!showConfNewPassword" class="w-4 h-4" />
            <EyeOff v-else class="w-4 h-4" />
          </button>
        </div>

        <!-- Password mismatch error message -->
        <span
            v-if="passwordMismatch"
            class="text-error text-sm mt-1"
        >
        Passwords do not match
      </span>
      </label>

      <!-- Error message display -->
      <div v-if="error" class="alert alert-error alert-soft mt-2">
        {{ error }}
      </div>

      <div class="flex flex-row mt-4">
        <!-- Link to cancel and return to main page -->
        <a class="btn btn-secondary btn-outline" href="/">Cancel</a>
        <div class="flex-grow" />
        <!-- Password change submission button -->
        <button class="btn btn-primary" :disabled="passwordMismatch || !fieldsEntered" type="submit"><span v-if="loading" class="loading loading-spinner loading-sm"></span>{{ loading ? 'Changing Password...' : 'Change Password' }}</button>
      </div>
    </form>
  </div>
</template>