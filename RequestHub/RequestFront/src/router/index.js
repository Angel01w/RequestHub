import { createRouter, createWebHistory } from "vue-router"
import { useAuthStore } from "../stores/auth"

import LoginView from "../views/LoginView.vue"
import MyRequestsView from "../views/MyRequestsView.vue"
import AreaQueueView from "../views/AreaQueueView.vue"
import RequestDetailView from "../views/RequestDetailView.vue"
import CatalogAdminView from "../views/CatalogAdminView.vue"

function normalizeRole(role) {
	return String(role ?? "").trim().toLowerCase()
}

function getStoredUser() {
	try {
		return JSON.parse(localStorage.getItem("rh_user") || localStorage.getItem("user") || "null")
	} catch {
		return null
	}
}

function getUserRole(auth) {
	return normalizeRole(
		auth.user?.role ||
		auth.role ||
		getStoredUser()?.role
	)
}

function hasToken(auth) {
	return Boolean(
		auth.token ||
		localStorage.getItem("rh_token") ||
		localStorage.getItem("token")
	)
}

function canAccessRoute(auth, to) {
	const allowedRoles = to.meta?.roles

	if (!Array.isArray(allowedRoles) || allowedRoles.length === 0) {
		return true
	}

	const currentRole = getUserRole(auth)
	return allowedRoles.map(normalizeRole).includes(currentRole)
}

function getDefaultAuthenticatedRoute(auth) {
	const role = getUserRole(auth)

	if (role === "superadmin") return "/dashboardview"
	if (role === "admin" || role === "gestor") return "/bandeja"
	if (role === "solicitante") return "/mis-solicitudes"

	return "/login"
}

const router = createRouter({
	history: createWebHistory(),
	routes: [
		{
			path: "/",
			redirect: "/login"
		},
		{
			path: "/login",
			name: "Login",
			component: LoginView,
			meta: { hideChrome: true, public: true }
		},
		{
			path: "/dashboardview",
			name: "Dashboard",
			component: () => import("../views/DashboardView.vue"),
			meta: {
				requiresAuth: true,
				roles: ["superadmin"]
			}
		},
		{
			path: "/dashboard",
			redirect: "/dashboardview"
		},
		{
			path: "/mis-solicitudes",
			name: "MyRequests",
			component: MyRequestsView,
			meta: {
				requiresAuth: true,
				roles: ["solicitante", "superadmin"]
			}
		},
		{
			path: "/bandeja",
			name: "AreaQueue",
			component: AreaQueueView,
			meta: {
				requiresAuth: true,
				roles: ["admin", "gestor"]
			}
		},
		{
			path: "/requests/:id",
			name: "RequestDetail",
			component: RequestDetailView,
			props: true,
			meta: {
				requiresAuth: true,
				roles: ["superadmin", "admin", "gestor", "solicitante"]
			}
		},
		{
			path: "/solicitudes/:id",
			redirect: to => ({
				name: "RequestDetail",
				params: { id: to.params.id }
			})
		},
		{
			path: "/admin/catalogos",
			name: "CatalogAdmin",
			component: CatalogAdminView,
			meta: {
				requiresAuth: true,
				roles: ["superadmin", "admin"]
			}
		}
	]
})

router.beforeEach((to) => {
	const auth = useAuthStore()
	const authenticated = auth.isAuthenticated || hasToken(auth)

	if (to.meta?.public) {
		if (authenticated && to.path === "/login") {
			return getDefaultAuthenticatedRoute(auth)
		}
		return true
	}

	if (to.meta?.requiresAuth && !authenticated) {
		return "/login"
	}

	if (to.meta?.requiresAuth && !canAccessRoute(auth, to)) {
		return getDefaultAuthenticatedRoute(auth)
	}

	return true
})

export default router