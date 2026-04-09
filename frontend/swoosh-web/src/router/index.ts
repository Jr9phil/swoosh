import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import ChangePasswordView from '../views/ChangePassword.vue'
import DeleteAccountView from '../views/DeleteAccount.vue'
import TasksView from '../views/TasksView.vue'
import RecurringView from '../views/RecurringView.vue'
import NoteboardView from '../views/NoteboardView.vue'
import RemindersView from '../views/RemindersView.vue'
import ProgressView from '../views/ProgressView.vue'
import SkilltreeView from '../views/SkilltreeView.vue'
import ArchiveView from '../views/ArchiveView.vue'
import { useAuthStore } from '../stores/auth'

const routes = [
    { path: '/login', component: LoginView },
    { path: '/register', component: RegisterView },
    { path: '/changePassword', component: ChangePasswordView },
    { path: '/deleteAccount', component: DeleteAccountView, meta: { requiresAuth: true } },
    { path: '/', component: TasksView, meta: { requiresAuth: true } },
    { path: '/recurring', component: RecurringView, meta: { requiresAuth: true } },
    { path: '/noteboard', component: NoteboardView, meta: { requiresAuth: true } },
    { path: '/reminders', component: RemindersView, meta: { requiresAuth: true } },
    { path: '/progress', component: ProgressView, meta: { requiresAuth: true } },
    { path: '/skilltree', component: SkilltreeView, meta: { requiresAuth: true } },
    { path: '/archive', component: ArchiveView, meta: { requiresAuth: true } },
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
