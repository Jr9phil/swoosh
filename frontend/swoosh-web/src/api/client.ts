import axios from 'axios'
import { useAuthStore } from '../stores/auth'
import router from '../router'

const api = axios.create({
    baseURL: 'http://localhost:8080/api'
})

api.interceptors.request.use(config => {
    const token = localStorage.getItem('token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

api.interceptors.response.use(
    res => res,
    err => {
        if (err.response?.status === 401) {
            const auth = useAuthStore()
            auth.logout()
            router.push('/login')
        }
        return Promise.reject(err)
    }
)

export default api
