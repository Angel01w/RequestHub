import { defineStore } from "pinia"
import api from "../services/api"

export const useAuthStore = defineStore("auth", {
	state: () => ({
		token: localStorage.getItem("token"),
		role: localStorage.getItem("role"),
		email: localStorage.getItem("email"),
		username: localStorage.getItem("username"),
		fullName: localStorage.getItem("fullName"),
		areaId: localStorage.getItem("areaId")
	}),
	getters: {
		isAuthenticated: state => !!state.token,
		isAdmin: state => state.role === "Admin" || state.role === "SuperAdmin",
		isSuperAdmin: state => state.role === "SuperAdmin",
		isGestor: state => state.role === "Gestor",
		isSolicitante: state => state.role === "Solicitante"
	},
	actions: {
		async login(email, password) {
			const { data } = await api.post("/api/Auth/login", { email, password })

			this.token = data.token
			this.role = data.role
			this.email = data.email
			this.username = data.username
			this.fullName = data.fullName
			this.areaId = data.areaId != null ? String(data.areaId) : ""

			localStorage.setItem("token", data.token)
			localStorage.setItem("role", data.role || "")
			localStorage.setItem("email", data.email || "")
			localStorage.setItem("username", data.username || "")
			localStorage.setItem("fullName", data.fullName || "")
			localStorage.setItem("areaId", data.areaId != null ? String(data.areaId) : "")
		},
		logout() {
			this.token = null
			this.role = null
			this.email = null
			this.username = null
			this.fullName = null
			this.areaId = null

			localStorage.removeItem("token")
			localStorage.removeItem("role")
			localStorage.removeItem("email")
			localStorage.removeItem("username")
			localStorage.removeItem("fullName")
			localStorage.removeItem("areaId")
		}
	}
})