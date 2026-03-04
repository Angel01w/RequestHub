import { createRouter, createWebHistory } from "vue-router";

import LoginView from "../views/LoginView.vue";
import MyRequestsView from "../views/MyRequestsView.vue";
import AreaQueueView from "../views/AreaQueueView.vue";
import RequestDetailView from "../views/RequestDetailView.vue";
import CatalogAdminView from "../views/CatalogAdminView.vue";

export default createRouter({
    history: createWebHistory(),
    routes: [
        { path: "/", redirect: "/login" },

        { path: "/login", component: LoginView, meta: { hideChrome: true } },

       
        {
            path: "/dashboardview",
            name: "dashboard",
            component: () => import("../views/DashboardView.vue"),
            meta: { hideChrome: true },
        },

 
        { path: "/dashboard", redirect: "/dashboardview" },

        { path: "/mis-solicitudes", component: MyRequestsView },
        { path: "/bandeja", component: AreaQueueView },
        { path: "/solicitudes/:id", component: RequestDetailView, props: true },
        { path: "/admin/catalogos", component: CatalogAdminView },
    ],
});
