<script setup lang="ts">
    import { computed } from "vue";
    import { useRoute, useRouter } from "vue-router";
    import { useAuthStore } from "./stores/auth";

    const route = useRoute();
    const router = useRouter();
    const auth = useAuthStore();

    const hideChrome = computed(() => !!route.meta.hideChrome);

    function logout() {
        if (typeof auth.logout === "function") auth.logout();
        router.push("/login");
    }
</script>

<template>
    <div class="app-shell">
        <header v-if="!hideChrome" class="topbar">
            <div class="topbar__inner">
                <div class="brand" aria-label="RequestHub">
                    <div class="brand__logo" aria-hidden="true">R</div>
                    <div class="brand__text">
                        <div class="brand__title">RequestHub</div>
                        <div class="brand__subtitle">Mesa de Servicios</div>
                    </div>
                </div>

                <nav class="nav">
                    <RouterLink class="nav__link" to="/mis-solicitudes">Mis Solicitudes</RouterLink>
                    <RouterLink class="nav__link" to="/bandeja">Bandeja</RouterLink>
                    <RouterLink class="nav__link" to="/admin/catalogos">Catálogos</RouterLink>
                </nav>

                <div class="actions">
                    <button class="btn btn--ghost" type="button" @click="logout">Salir</button>
                </div>
            </div>
        </header>

        <!-- ✅ cuando hideChrome=true, NO container/padding; el view manda -->
        <main v-if="hideChrome">
            <RouterView />
        </main>

        <main v-else class="main">
            <div class="container">
                <RouterView />
            </div>
        </main>
    </div>
</template>

<style scoped>
    .app-shell {
        min-height: 100vh;
        background: radial-gradient(1000px 420px at 15% 10%, rgba(83, 120, 255, 0.12), rgba(255, 255, 255, 0) 60%), radial-gradient(900px 520px at 85% 85%, rgba(164, 80, 255, 0.10), rgba(255, 255, 255, 0) 60%), linear-gradient(160deg, #f7f9ff 0%, #e8eeff 100%);
        color: #1d2a44;
    }

    .topbar {
        position: sticky;
        top: 0;
        z-index: 10;
        padding: 14px 16px;
        background: rgba(13, 22, 48, 0.88);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid rgba(255, 255, 255, 0.10);
    }

    .topbar__inner {
        max-width: 1100px;
        margin: 0 auto;
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        align-items: center;
        gap: 14px;
    }

    .brand {
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 240px;
    }

    .brand__logo {
        width: 38px;
        height: 38px;
        border-radius: 14px;
        display: grid;
        place-items: center;
        font-weight: 900;
        color: #fff;
        background: linear-gradient(135deg, #3f58ff, #9a3cff);
    }

    .brand__title {
        font-weight: 900;
        color: #fff;
        line-height: 1;
    }

    .brand__subtitle {
        font-size: 12px;
        color: rgba(207, 224, 255, 0.85);
    }

    .nav {
        display: flex;
        gap: 10px;
        justify-content: center;
    }

    .nav__link {
        text-decoration: none;
        font-weight: 800;
        font-size: 13px;
        color: rgba(207, 224, 255, 0.88);
        padding: 10px 12px;
        border-radius: 14px;
        border: 1px solid rgba(255, 255, 255, 0.10);
        background: rgba(255, 255, 255, 0.06);
    }

        .nav__link.router-link-active {
            color: #fff;
            background: linear-gradient(135deg, rgba(63, 88, 255, 0.55), rgba(154, 60, 255, 0.50));
            border-color: rgba(255, 255, 255, 0.18);
        }

    .actions {
        display: flex;
        justify-content: flex-end;
    }

    .btn {
        height: 38px;
        padding: 0 14px;
        border-radius: 14px;
        font-weight: 900;
        cursor: pointer;
    }

    .btn--ghost {
        color: #fff;
        border: 1px solid rgba(255, 255, 255, 0.14);
        background: rgba(255, 255, 255, 0.07);
    }

    .main {
        padding: 18px 16px 28px;
    }

    .container {
        max-width: 1100px;
        margin: 0 auto;
    }
</style>
