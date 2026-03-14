import { createRouter, createWebHistory } from "vue-router"
import { useAuthStore } from "../stores/auth"

import LoginView from "../views/LoginView.vue"
import MyRequestsView from "../views/MyRequestsView.vue"
import AreaQueueView from "../views/AreaQueueView.vue"
import RequestDetailView from "../views/RequestDetailView.vue"
import CatalogAdminView from "../views/CatalogAdminView.vue"

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
			meta: { requiresAuth: true }
		},
		{
			path: "/dashboard",
			redirect: "/dashboardview"
		},
		{
			path: "/mis-solicitudes",
			name: "MyRequests",
			component: MyRequestsView,
			meta: { requiresAuth: true }
		},
		{
			path: "/bandeja",
			name: "AreaQueue",
			component: AreaQueueView,
			meta: { requiresAuth: true }
		},
		{
			path: "/requests/:id",
			name: "RequestDetail",
			component: RequestDetailView,
			props: true,
			meta: { requiresAuth: true }
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
			meta: { requiresAuth: true }
		}
	]
})

router.beforeEach((to) => {
	const auth = useAuthStore()

	if (to.meta?.public) {
		if (auth.isAuthenticated && to.path === "/login") {
			return "/dashboardview"
		}
		return true
	}

	if (to.meta?.requiresAuth && !auth.isAuthenticated) {
		return "/login"
	}

	return true
})

export default router