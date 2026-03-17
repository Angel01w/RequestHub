import { defineStore } from "pinia"
import api from "../services/api"

function normalizeRole(value) {
	return String(value ?? "").trim().toLowerCase()
}

function normalizeAreaId(value, role) {
	const normalizedRole = normalizeRole(role)
	if (normalizedRole === "superadmin" || normalizedRole === "solicitante" || value == null || value === "") return ""
	const parsed = Number(value)
	return Number.isFinite(parsed) && parsed > 0 ? String(parsed) : ""
}

function normalizeUserId(value) {
	if (value == null || value === "") return ""
	const parsed = Number(value)
	return Number.isFinite(parsed) && parsed > 0 ? String(parsed) : ""
}

function buildUserObject(source = {}, fallback = {}) {
	const role = normalizeRole(source.role ?? fallback.role)
	const id = normalizeUserId(
		source.id ??
		source.userId ??
		source.idUsuario ??
		fallback.userId ??
		fallback.id
	)
	const areaId = normalizeAreaId(source.areaId ?? source.idArea ?? fallback.areaId ?? fallback.idArea, role)
	const email = String(source.email ?? fallback.email ?? "").trim()
	const username = String(source.username ?? source.userName ?? fallback.username ?? fallback.userName ?? "").trim()
	const fullName = String(
		source.fullName ??
		source.nombreCompleto ??
		source.name ??
		fallback.fullName ??
		fallback.nombreCompleto ??
		fallback.name ??
		""
	).trim()

	return {
		id: id ? Number(id) : null,
		userId: id ? Number(id) : null,
		role,
		email,
		username,
		fullName,
		areaId: areaId ? Number(areaId) : null
	}
}

export const useAuthStore = defineStore("auth", {
	state: () => ({
		token: localStorage.getItem("rh_token") || localStorage.getItem("token") || "",
		userId: localStorage.getItem("userId") || "",
		role: normalizeRole(localStorage.getItem("role")),
		email: localStorage.getItem("email") || "",
		username: localStorage.getItem("username") || "",
		fullName: localStorage.getItem("fullName") || "",
		areaId: localStorage.getItem("areaId") || "",
		userData: (() => {
			try {
				return JSON.parse(localStorage.getItem("rh_user") || localStorage.getItem("user") || "null")
			} catch {
				return null
			}
		})()
	}),
	getters: {
		isAuthenticated: state => !!String(state.token || "").trim(),
		user: state => {
			if (state.userData && typeof state.userData === "object") {
				return buildUserObject(state.userData, {
					userId: state.userId,
					role: state.role,
					email: state.email,
					username: state.username,
					fullName: state.fullName,
					areaId: state.areaId
				})
			}

			return buildUserObject({}, {
				userId: state.userId,
				role: state.role,
				email: state.email,
				username: state.username,
				fullName: state.fullName,
				areaId: state.areaId
			})
		},
		isSuperAdmin: state => normalizeRole(state.role) === "superadmin",
		isAdmin: state => normalizeRole(state.role) === "admin",
		isGestor: state => normalizeRole(state.role) === "gestor",
		isSolicitante: state => normalizeRole(state.role) === "solicitante",
		isAdminLike: state => {
			const role = normalizeRole(state.role)
			return role === "admin" || role === "superadmin"
		}
	},
	actions: {
		async login(email, password) {
			const payload = {
				email: String(email || "").trim(),
				password: String(password || "")
			}

			const { data } = await api.post("/api/Auth/login", payload)

			const rawUser = data?.user ?? {}
			const token = String(data?.token ?? "").trim()
			const normalizedUser = buildUserObject(rawUser, data)

			this.token = token
			this.userId = normalizedUser.userId ? String(normalizedUser.userId) : ""
			this.role = normalizedUser.role
			this.email = normalizedUser.email
			this.username = normalizedUser.username
			this.fullName = normalizedUser.fullName
			this.areaId = normalizedUser.areaId ? String(normalizedUser.areaId) : ""
			this.userData = normalizedUser

			localStorage.setItem("rh_token", token)
			localStorage.setItem("token", token)
			localStorage.setItem("userId", this.userId)
			localStorage.setItem("role", this.role)
			localStorage.setItem("email", this.email)
			localStorage.setItem("username", this.username)
			localStorage.setItem("fullName", this.fullName)
			localStorage.setItem("areaId", this.areaId)
			localStorage.setItem("rh_user", JSON.stringify(normalizedUser))
			localStorage.setItem("user", JSON.stringify(normalizedUser))
		},
		logout() {
			this.token = ""
			this.userId = ""
			this.role = ""
			this.email = ""
			this.username = ""
			this.fullName = ""
			this.areaId = ""
			this.userData = null

			localStorage.removeItem("rh_token")
			localStorage.removeItem("token")
			localStorage.removeItem("userId")
			localStorage.removeItem("role")
			localStorage.removeItem("email")
			localStorage.removeItem("username")
			localStorage.removeItem("fullName")
			localStorage.removeItem("areaId")
			localStorage.removeItem("rh_user")
			localStorage.removeItem("user")
		}
	}
})