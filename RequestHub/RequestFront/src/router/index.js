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

function getUserRole(auth) {
	return normalizeRole(auth.user?.role || auth.role)
}

function isSolicitante(auth) {
	return getUserRole(auth) === "solicitante"
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
	return isSolicitante(auth) ? "/mis-solicitudes" : "/dashboardview"
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
			name: "dashboard",
			component: () => import("../views/DashboardView.vue"),
			meta: {
				requiresAuth: true,
				roles: ["SuperAdmin", "Admin", "Gestor"]
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
				roles: ["SuperAdmin", "Admin", "Gestor", "Solicitante"]
			}
		},
		{
			path: "/bandeja",
			name: "AreaQueue",
			component: AreaQueueView,
			meta: {
				requiresAuth: true,
				roles: ["SuperAdmin", "Admin", "Gestor"]
			}
		},
		{
			path: "/requests/:id",
			name: "RequestDetail",
			component: RequestDetailView,
			props: true,
			meta: {
				requiresAuth: true,
				roles: ["SuperAdmin", "Admin", "Gestor", "Solicitante"]
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
				roles: ["SuperAdmin", "Admin"]
			}
		}
	]
})

router.beforeEach((to) => {
	const auth = useAuthStore()

	if (to.meta?.public) {
		if (auth.isAuthenticated && to.path === "/login") {
			return getDefaultAuthenticatedRoute(auth)
		}
		return true
	}

	if (to.meta?.requiresAuth && !auth.isAuthenticated) {
		return "/login"
	}

	if (to.meta?.requiresAuth && !canAccessRoute(auth, to)) {
		return getDefaultAuthenticatedRoute(auth)
	}

	return true
})

export default router