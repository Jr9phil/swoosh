/**
 * auth.ts
 * Pinia store for managing user authentication state and actions.
 * Handles login, registration, password changes, and session persistence via localStorage.
 */
import { defineStore } from 'pinia'
import api from '../api/client'

export const useAuthStore = defineStore('auth', {
    // Store state: persists the authentication token and current user email
    state: () => ({
        token: localStorage.getItem('token') as string | null,
        currentUser: localStorage.getItem('currentUser') as string | null,
    }),
    actions: {
        // Authenticates a user and stores the returned token
        async login(email: string, password: string) {
            const res = await api.post('/auth/login', { email, password })
            this.token = res.data.token
            localStorage.setItem('token', this.token!)
            if (res.status === 200) {
                this.currentUser = email
                localStorage.setItem('currentUser', email)
            }
        },
        // Registers a new user account
        async register(email: string, password: string) {
            await api.post('/auth/register', { email, password })
        },
        // Updates the password for the currently authenticated user
        async changePassword(currentPassword: string, newPassword: string) {
          await api.post('/auth/change-password', { currentPassword, newPassword })  
        },
        // Clears authentication state and removes stored tokens/user info
        logout() {
            this.token = null
            this.currentUser = null
            localStorage.removeItem('token')
            localStorage.removeItem('currentUser')
        }
    }
})