<!--
  DeleteAccount.vue
  Allows authenticated users to permanently delete their account and all associated data.
  Requires the user to confirm their email and password before deletion proceeds.
-->
<script setup lang="ts">
import { ref, computed } from 'vue'
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

const fieldsEntered = computed(() =>
    email.value.length > 0 &&
    password.value.length > 0
)

async function submit() {
  error.value = null
  loading.value = true
  showPassword.value = false

  try {
    await auth.deleteAccount(email.value, password.value)
    auth.logout()
    router.push('/login')
  } catch (err) {
    if (axios.isAxiosError(err)) {
      const status = err.response?.status
      if (status === 400) {
        error.value = 'Email or password is incorrect'
      } else if (status === 500) {
        error.value = 'Internal Server Error'
      } else {
        error.value = 'Account deletion failed'
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
  <main class="flex-1 flex justify-center pt-20 px-5">
    <div class="w-full max-w-[360px]">
      <header class="mb-8 text-center">
        <h1 class="auth-page-title">Delete Account</h1>
        <p class="auth-page-subtitle">This will permanently delete your account and all of your tasks. This action cannot be undone.</p>
      </header>

      <form class="flex flex-col gap-4" @submit.prevent="submit">
        <div class="auth-field">
          <label class="auth-label">Email</label>
          <input type="email" class="swoosh-input w-full" placeholder="your@email.com" required v-model="email" autocomplete="email" />
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

        <div class="flex flex-col gap-2 mt-2">
          <button type="submit" class="auth-submit-btn rounded-sm bg-red-600 hover:bg-red-700 border-red-600 hover:border-red-700" :disabled="loading || !fieldsEntered">
            {{ loading ? 'Deleting...' : 'Delete My Account' }}
          </button>
          <router-link to="/changePassword" class="w-full text-center py-2 text-[13px] text-swoosh-text-faint hover:text-swoosh-text-muted transition-colors">Cancel</router-link>
        </div>
      </form>
    </div>
  </main>
</template>
