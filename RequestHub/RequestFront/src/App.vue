<script setup>
    import { computed } from "vue"
    import { RouterLink, useRoute, useRouter } from "vue-router"
    import { useAuthStore } from "./stores/auth"

    const router = useRouter()
    const route = useRoute()
    const auth = useAuthStore()

    const isLogin = computed(() => route.path.startsWith("/login"))

    const menu = computed(() => [
        { key: "dashboard", label: "Dashboard", sub: "Vista general", to: "/dashboard", icon: "grid" },
        { key: "my", label: "Mis Solicitudes", to: "/mis-solicitudes", icon: "list" },
        { key: "area", label: "Bandeja del Área", to: "/bandeja", icon: "inbox" },
        { key: "admin", label: "Administración", to: "/admin/catalogos", icon: "gear" },
    ])

    const activeKey = computed(() => {
        const p = route.path

        if (p.startsWith("/dashboard") || p.startsWith("/dashboardview")) return "dashboard"
        if (p.startsWith("/mis-solicitudes") || p.startsWith("/solicitudes")) return "my"
        if (p.startsWith("/bandeja")) return "area"
        if (p.startsWith("/admin")) return "admin"

        return "dashboard"
    })

    function iconPath(name) {
        switch (name) {
            case "grid":
                return "M4 4h7v7H4V4Zm9 0h7v7h-7V4ZM4 13h7v7H4v-7Zm9 0h7v7h-7v-7Z"
            case "list":
                return "M6 7h14M6 12h14M6 17h14M4 7h.01M4 12h.01M4 17h.01"
            case "inbox":
                return "M4 4h16v12H4V4Zm0 12h5l1.5 2h3L15 16h5v4H4v-4Z"
            case "gear":
                return "M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM19.4 12a7.4 7.4 0 0 0-.1-1l2-1.6-2-3.4-2.4 1a7.6 7.6 0 0 0-1.7-1l-.4-2.6H9.2L8.8 6a7.6 7.6 0 0 0-1.7 1l-2.4-1-2 3.4 2 1.6a7.4 7.4 0 0 0 0 2l-2 1.6 2 3.4 2.4-1c.5.4 1.1.7 1.7 1l.4 2.6h5.6l.4-2.6c.6-.3 1.2-.6 1.7-1l2.4 1 2-3.4-2-1.6c.1-.3.1-.6.1-1Z"
            default:
                return ""
        }
    }

    function logout() {
        auth.logout()
        router.replace("/login")
    }
</script>

<template>
    <div v-if="isLogin" class="app-login">
        <router-view />
    </div>

    <div v-else class="app-shell">
        <div class="dash">
            <div class="layout">
                <aside class="sidebar">
                    <div class="brand">
                        <div class="brand__logo">
                            <span class="brand__letter">R</span>
                        </div>
                        <div class="brand__txt">
                            <div class="brand__title">RequestHub</div>
                            <div class="brand__sub">Mesa de Servicios</div>
                        </div>
                    </div>

                    <nav class="nav" aria-label="Navegación">
                        <RouterLink v-for="item in menu"
                                    :key="item.key"
                                    class="navitem"
                                    :class="{ 'navitem--active': activeKey === item.key }"
                                    :to="item.to"
                                    :aria-current="activeKey === item.key ? 'page' : undefined">
                            <span class="navitem__icon" aria-hidden="true">
                                <svg viewBox="0 0 24 24"><path :d="iconPath(item.icon)" /></svg>
                            </span>
                            <span class="navitem__text">
                                <span class="navitem__label">{{ item.label }}</span>
                                <span v-if="item.sub" class="navitem__sub">{{ item.sub }}</span>
                            </span>
                        </RouterLink>
                    </nav>

                    <div class="spacer"></div>

                    <button class="logout" type="button" @click="logout">
                        <span class="logout__icon" aria-hidden="true">
                            <svg viewBox="0 0 24 24">
                                <path d="M10 17v2H5V5h5v2M14 7l5 5-5 5M19 12H10" />
                            </svg>
                        </span>
                        <span>Cerrar Sesión</span>
                    </button>
                </aside>

                <main class="main">
                    <router-view />
                </main>
            </div>
        </div>
    </div>
</template>

<style>
    html,
    body,
    #app {
        height: 100%;
        margin: 0;
    }
</style>

<style scoped>
    .app-login {
        min-height: 100vh;
    }

    .app-shell {
        min-height: 100vh;
    }

    .dash {
        min-height: 100vh;
        background: radial-gradient(900px 520px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(860px 520px at 85% 85%, rgba(179, 94, 255, 0.2), rgba(255, 255, 255, 0) 62%), linear-gradient(180deg, #f7f7ff 0%, #ece9ff 55%, #e9e7ff 100%);
        position: relative;
        overflow: hidden;
    }

        .dash::before {
            content: "";
            position: absolute;
            inset: 0;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1200' height='700' viewBox='0 0 1200 700'%3E%3Cg fill='none' stroke='%23ffffff' stroke-opacity='.24'%3E%3Cpath d='M0 520 C 220 480 360 600 560 560 C 760 520 860 420 1200 460' stroke-width='18'/%3E%3Cpath d='M0 580 C 220 540 360 660 560 620 C 760 580 860 480 1200 520' stroke-width='14'/%3E%3Cpath d='M0 640 C 240 600 380 700 580 670 C 780 640 900 560 1200 600' stroke-width='10'/%3E%3C/g%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: center bottom;
            background-size: cover;
            pointer-events: none;
            opacity: 0.9;
        }

    .layout {
        position: relative;
        display: grid;
        grid-template-columns: 270px 1fr;
        min-height: 100vh;
    }

    .sidebar {
        padding: 20px 16px;
        background: rgba(255, 255, 255, 0.58);
        border-right: 1px solid rgba(110, 102, 182, 0.1);
        backdrop-filter: blur(10px);
        display: flex;
        flex-direction: column;
        gap: 16px;
    }

    .brand {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 10px 10px;
        border-radius: 18px;
    }

    .brand__logo {
        width: 44px;
        height: 44px;
        border-radius: 16px;
        background: linear-gradient(135deg, #3f58ff 0%, #9a3cff 100%);
        display: grid;
        place-items: center;
        box-shadow: 0 18px 30px rgba(88, 78, 212, 0.22);
    }

    .brand__letter {
        color: #fff;
        font-weight: 900;
        font-size: 18px;
    }

    .brand__title {
        font-weight: 900;
        color: #232a52;
        letter-spacing: -0.2px;
    }

    .brand__sub {
        font-size: 12px;
        color: rgba(39, 46, 86, 0.7);
        margin-top: 1px;
    }

    .nav {
        display: grid;
        gap: 10px;
        padding: 6px 4px;
    }

    .navitem {
        text-decoration: none;
        text-align: left;
        cursor: pointer;
        padding: 10px 10px;
        border-radius: 14px;
        background: rgba(255, 255, 255, 0.42);
        border: 1px solid rgba(110, 102, 182, 0.08);
        display: grid;
        grid-template-columns: 38px 1fr;
        gap: 10px;
        align-items: center;
        color: #2a2f57;
        box-shadow: 0 12px 24px rgba(40, 55, 95, 0.06);
    }

    .navitem__icon {
        width: 34px;
        height: 34px;
        border-radius: 12px;
        background: rgba(120, 105, 235, 0.12);
        display: grid;
        place-items: center;
        color: #5b4df0;
    }

        .navitem__icon svg {
            width: 18px;
            height: 18px;
        }

        .navitem__icon path {
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

    .navitem__label {
        font-weight: 900;
        font-size: 13px;
        line-height: 1.15;
    }

    .navitem__sub {
        display: block;
        font-size: 11px;
        color: rgba(39, 46, 86, 0.62);
        margin-top: 2px;
    }

    .navitem--active {
        background: linear-gradient(135deg, rgba(99, 80, 255, 0.26), rgba(163, 88, 255, 0.18));
        border-color: rgba(120, 105, 235, 0.18);
    }

    .spacer {
        flex: 1;
    }

    .logout {
        border: none;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 10px;
        border-radius: 14px;
        background: rgba(255, 255, 255, 0.42);
        border: 1px solid rgba(110, 102, 182, 0.08);
        color: rgba(43, 50, 92, 0.88);
        font-weight: 900;
        box-shadow: 0 12px 24px rgba(40, 55, 95, 0.06);
    }

    .logout__icon {
        width: 34px;
        height: 34px;
        border-radius: 12px;
        background: rgba(120, 105, 235, 0.12);
        display: grid;
        place-items: center;
        color: #5b4df0;
    }

        .logout__icon svg {
            width: 18px;
            height: 18px;
        }

        .logout__icon path {
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

    .main {
        min-width: 0;
        min-height: 100vh;
    }

    @media (max-width: 860px) {
        .layout {
            grid-template-columns: 1fr;
        }

        .sidebar {
            display: none;
        }
    }
</style>