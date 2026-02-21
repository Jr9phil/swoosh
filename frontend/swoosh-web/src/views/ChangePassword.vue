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

const passwordMismatch = computed(() =>
    confirmNewPassword.value.length > 0 &&
    newPassword.value !== confirmNewPassword.value
)

const fieldsEntered = computed(() =>
    password.value.length > 0 &&
    newPassword.value.length > 0 &&
    confirmNewPassword.value.length > 0
)
async function submit() {
  await auth.changePassword(password.value, newPassword.value)
  router.push('/')
}
</script>

<template>
  <form class="fieldset bg-base-200 border-base-300 rounded-box w-sm border p-8" @submit.prevent="submit">
    <label class="fieldset">
      <span class="label">Current Password</span>
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

    <label class="fieldset">
      <span class="label">New Password</span>
      <div class="join">
        <input :type="showNewPassword ? 'text' : 'password'" class="input validator join-item" placeholder="New Password" required v-model="newPassword" minlength="8" />
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

      <span
          v-if="passwordMismatch"
          class="text-error text-sm mt-1"
      >
        Passwords do not match
      </span>
    </label>

    <div v-if="error" class="alert alert-error alert-soft mt-2">
      {{ error }}
    </div>

    <button class="btn btn-primary mt-4" :disabled="passwordMismatch || !fieldsEntered" type="submit">Change Password</button>
    <a class="btn btn-secondary mt-1" href="/">Cancel</a>
  </form>
</template>