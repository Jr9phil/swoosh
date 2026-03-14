import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './style.css'
import App from './App.vue'
import { clickOutside } from './directives/clickOutside'
import { animateSync } from './directives/animateSync'

createApp(App)
    .use(createPinia())
    .use(router)
    .directive('click-outside', clickOutside)
    .directive('animate-sync', animateSync)
    .mount('#app')
