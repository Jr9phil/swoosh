<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import { Eye, EyeOff } from 'lucide-vue-next'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfPassword = ref(false)

const passwordMismatch = computed(() => 
    confirmPassword.value.length > 0 &&
    password.value !== confirmPassword.value
)

const fieldsEntered = computed(() => 
    email.value.length > 0 &&
    password.value.length > 0 &&
    confirmPassword.value.length > 0
)

async function submit() {
  await auth.register(email.value, password.value)
  router.push('/')
}
</script>

<template>
  <form class="fieldset bg-base-200 border-base-300 rounded-box w-sm border p-8" @submit.prevent="submit">
    <fieldset class="fieldset">
      <legend class="fieldset-legend">Create Account</legend>
      <label class="label">Email</label>
      <input type="email" class="input validator" placeholder="Email" required v-model="email" />
      <p class="validator-hint hidden">Required</p>
    </fieldset>

    <label class="fieldset">
      <span class="label">Password</span>
      <div class="join">
        <input :type="showPassword ? 'text' : 'password'" class="input validator join-item" placeholder="Password" required v-model="password" minlength="8" />
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
    </label>

    <label class="fieldset">
      <span class="label">Confirm Password</span>
      <div class="join">
        <input
            :type="showConfPassword ? 'text' : 'password'"
            class="input join-item"
            placeholder="Confirm password"
            required
            v-model="confirmPassword"
            :class="{
          'input-error': passwordMismatch
        }"
        />
        <button
            type="button"
            class="btn btn-soft btn-square join-item"
            @click="showConfPassword = !showConfPassword"
            tabindex="-1"
        >
          <Eye v-if="!showConfPassword" class="w-4 h-4" />
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

    <button class="btn btn-primary mt-4" :disabled="passwordMismatch || !fieldsEntered" type="submit">Register</button>
    <a class="btn btn-neutral mt-1" href="/">Back to Login</a>
  </form>
</template>