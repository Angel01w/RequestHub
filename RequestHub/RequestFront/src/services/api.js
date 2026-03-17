import axios from "axios"

const baseURL = String(import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")

const api = axios.create({
	baseURL,
	headers: {
		"Content-Type": "application/json"
	}
})

api.interceptors.request.use(
	config => {
		const token = localStorage.getItem("rh_token") || localStorage.getItem("token")

		config.headers = config.headers || {}

		if (token) {
			config.headers.Authorization = `Bearer ${token}`
		} else {
			delete config.headers.Authorization
		}

		return config
	},
	error => Promise.reject(error)
)

api.interceptors.response.use(
	response => response,
	error => {
		if (error?.response?.status === 401) {
			localStorage.removeItem("rh_token")
			localStorage.removeItem("token")
			localStorage.removeItem("rh_user")
			localStorage.removeItem("user")
			localStorage.removeItem("userId")
			localStorage.removeItem("role")
			localStorage.removeItem("email")
			localStorage.removeItem("username")
			localStorage.removeItem("fullName")
			localStorage.removeItem("areaId")
		}

		return Promise.reject(error)
	}
)

export default api