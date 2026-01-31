import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import TasksView from '../views/TasksView.vue'
import { useAuthStore } from '../stores/auth'

const routes = [
    { path: '/login', component: LoginView },
    { path: '/register', component: RegisterView },
    {
        path: '/',
        component: TasksView,
        meta: { requiresAuth: true }
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

router.beforeEach((to) => {
    const auth = useAuthStore()

    if (to.meta.requiresAuth && !auth.token) {
        return '/login'
    }

    if ((to.path === '/login' || to.path === '/register') && auth.token) {
        return '/'
    }
})

export default router
