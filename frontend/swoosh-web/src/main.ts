import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './style.css'
import App from './App.vue'
import { clickOutside } from './directives/clickOutside'

createApp(App)
    .use(createPinia())
    .use(router)
    .directive('click-outside', clickOutside)
    .mount('#app')
