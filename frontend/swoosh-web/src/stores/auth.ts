import { defineStore } from 'pinia'
import api from '../api/client'

export const useAuthStore = defineStore('auth', {
    state: () => ({
        token: localStorage.getItem('token') as string | null,
        currentUser: localStorage.getItem('currentUser') as string | null,
    }),
    actions: {
        async login(email: string, password: string) {
            const res = await api.post('/auth/login', { email, password })
            this.token = res.data.token
            localStorage.setItem('token', this.token!)
            if (res.status === 200) {
                this.currentUser = email
                localStorage.setItem('currentUser', email)
            }
        },
        async register(email: string, password: string) {
            await api.post('/auth/register', { email, password })
        },
        async changePassword(currentPassword: string, newPassword: string) {
          await api.post('/auth/change-password', { currentPassword, newPassword })  
        },
        logout() {
            this.token = null
            this.currentUser = null
            localStorage.removeItem('token')
            localStorage.removeItem('currentUser')
        }
    }
})