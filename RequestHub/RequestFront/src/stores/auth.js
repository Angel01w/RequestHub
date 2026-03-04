import { defineStore } from 'pinia'
import api from '../services/api'

export const useAuthStore = defineStore('auth', {
    state: () => ({ token: localStorage.getItem('token'), role: localStorage.getItem('role') }),
    actions: {
        async login(username, password) {
            const { data } = await api.post('/auth/login', { username, password })
            this.token = data.token
            this.role = data.role
            localStorage.setItem('token', data.token)
            localStorage.setItem('role', data.role)
        },
        logout() {
            this.token = null
            this.role = null
            localStorage.clear()
        }
    }
})
