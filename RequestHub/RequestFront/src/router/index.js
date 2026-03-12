import { createRouter, createWebHistory } from "vue-router";

import LoginView from "../views/LoginView.vue";
import MyRequestsView from "../views/MyRequestsView.vue";
import AreaQueueView from "../views/AreaQueueView.vue";
import RequestDetailView from "../views/RequestDetailView.vue";
import CatalogAdminView from "../views/CatalogAdminView.vue";

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
			meta: { hideChrome: true }
		},
		{
			path: "/dashboardview",
			name: "dashboard",
			component: () => import("../views/DashboardView.vue"),
			meta: { hideChrome: true }
		},
		{
			path: "/dashboard",
			redirect: "/dashboardview"
		},
		{
			path: "/mis-solicitudes",
			name: "MyRequests",
			component: MyRequestsView
		},
		{
			path: "/bandeja",
			name: "AreaQueue",
			component: AreaQueueView
		},
		{
			path: "/requests/:id",
			name: "RequestDetail",
			component: RequestDetailView,
			props: true
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
			component: CatalogAdminView
		}
	]
});

export default router;
