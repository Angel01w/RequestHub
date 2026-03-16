import { defineStore } from "pinia"
import api from "../services/api"

function normalizeRole(value) {
	return String(value || "").trim()
}

function normalizeAreaId(value, role) {
	const normalizedRole = normalizeRole(role)
	if (normalizedRole === "SuperAdmin" || normalizedRole === "Solicitante" || value == null || value === "") return ""
	return String(value)
}

function normalizeUserId(value) {
	if (value == null || value === "") return ""
	const parsed = Number(value)
	return Number.isFinite(parsed) && parsed > 0 ? String(parsed) : ""
}

export const useAuthStore = defineStore("auth", {
	state: () => ({
		token: localStorage.getItem("token") || "",
		userId: localStorage.getItem("userId") || "",
		role: normalizeRole(localStorage.getItem("role")),
		email: localStorage.getItem("email") || "",
		username: localStorage.getItem("username") || "",
		fullName: localStorage.getItem("fullName") || "",
		areaId: localStorage.getItem("areaId") || ""
	}),
	getters: {
		isAuthenticated: state => !!String(state.token || "").trim(),
		user: state => ({
			id: state.userId ? Number(state.userId) : null,
			userId: state.userId ? Number(state.userId) : null,
			role: state.role,
			email: state.email,
			username: state.username,
			fullName: state.fullName,
			areaId: state.areaId ? Number(state.areaId) : null
		}),
		isSuperAdmin: state => normalizeRole(state.role) === "SuperAdmin",
		isAdmin: state => normalizeRole(state.role) === "Admin",
		isGestor: state => normalizeRole(state.role) === "Gestor",
		isSolicitante: state => normalizeRole(state.role) === "Solicitante",
		isAdminLike: state => {
			const role = normalizeRole(state.role)
			return role === "Admin" || role === "SuperAdmin"
		}
	},
	actions: {
		async login(email, password) {
			const payload = {
				email: String(email || "").trim(),
				password: String(password || "")
			}

			const { data } = await api.post("/api/Auth/login", payload)

			const user = data?.user ?? {}
			const token = data?.token ?? ""
			const role = normalizeRole(user.role ?? data?.role)
			const userId = normalizeUserId(
				user.id ??
				user.userId ??
				user.idUsuario ??
				data?.userId ??
				data?.id
			)
			const areaId = normalizeAreaId(user.areaId ?? data?.areaId, role)
			const emailValue = user.email ?? data?.email ?? ""
			const usernameValue = user.username ?? data?.username ?? ""
			const fullNameValue = user.fullName ?? data?.fullName ?? ""

			this.token = token
			this.userId = userId
			this.role = role
			this.email = emailValue
			this.username = usernameValue
			this.fullName = fullNameValue
			this.areaId = areaId

			localStorage.setItem("token", token)
			localStorage.setItem("userId", userId)
			localStorage.setItem("role", role)
			localStorage.setItem("email", emailValue)
			localStorage.setItem("username", usernameValue)
			localStorage.setItem("fullName", fullNameValue)
			localStorage.setItem("areaId", areaId)
		},
		logout() {
			this.token = ""
			this.userId = ""
			this.role = ""
			this.email = ""
			this.username = ""
			this.fullName = ""
			this.areaId = ""

			localStorage.removeItem("token")
			localStorage.removeItem("userId")
			localStorage.removeItem("role")
			localStorage.removeItem("email")
			localStorage.removeItem("username")
			localStorage.removeItem("fullName")
			localStorage.removeItem("areaId")
		}
	}
})