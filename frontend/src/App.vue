<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const isLoginRoute = computed(() => route.path === '/login' || route.path === '/')
</script>

<template>
  <div>
    <header v-if="!isLoginRoute" class="header">
      <h1>RequestHub - Mesa de Servicios</h1>
      <nav>
        <router-link to="/mis-solicitudes">Mis Solicitudes</router-link> |
        <router-link to="/bandeja">Bandeja</router-link> |
        <router-link to="/admin/catalogos">Catálogos</router-link>
      </nav>
    </header>
    <main :class="isLoginRoute ? 'login-layout' : 'container'">
      <router-view />
    </main>
  </div>
</template>

<style>
body { font-family: Inter, Arial, sans-serif; margin: 0; background: #f6f8ff; }
.header { background: #0f172a; color: white; padding: 1rem; }
nav a { color: #bfdbfe; margin-right: .5rem; }
.container { padding: 1rem; }
.login-layout { min-height: 100vh; }
.card { border: 1px solid #e5e7eb; border-radius: 8px; padding: 1rem; margin-bottom: .75rem; }
input, select, textarea, button { width: 100%; padding: .5rem; margin: .25rem 0 .75rem; }
.grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: .5rem; }
</style>
