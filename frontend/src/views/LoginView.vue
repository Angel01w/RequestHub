<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const roles = [
  { key: 'Solicitante', label: 'Solicitante', icon: '👤' },
  { key: 'Gestor', label: 'Gestor', icon: '📋' },
  { key: 'Administrador', label: 'Administrador', icon: '🛡️' }
]

const role = ref('Solicitante')
const username = ref('')
const password = ref('')
const showPassword = ref(false)
const error = ref('')

const roleHint = computed(() => {
  if (role.value === 'Solicitante') return 'Crea y da seguimiento a tus solicitudes'
  if (role.value === 'Gestor') return 'Gestiona solicitudes del área asignada'
  return 'Administra catálogos y supervisa todas las solicitudes'
})

const submit = async () => {
  error.value = ''
  try {
    await auth.login(username.value, password.value)
    router.push('/mis-solicitudes')
  } catch {
    error.value = 'Credenciales inválidas'
  }
}
</script>

<template>
  <div class="login-screen">
    <div class="login-wrap">
      <div class="brand">
        <div class="brand-icon">R</div>
        <h1>RequestHub</h1>
      </div>
      <p class="subtitle">Sistema de Gestión de Solicitudes Internas</p>

      <div class="panel">
        <h2>Seleccionar Rol</h2>

        <div class="roles">
          <button
            v-for="r in roles"
            :key="r.key"
            type="button"
            class="role-btn"
            :class="{ active: role === r.key }"
            @click="role = r.key"
          >
            <span class="emoji">{{ r.icon }}</span>
            <span>{{ r.label }}</span>
          </button>
        </div>

        <p class="hint">{{ roleHint }}</p>

        <label>Usuario</label>
        <input v-model.trim="username" type="text" placeholder="usuario@empresa.com" />

        <label>Contraseña</label>
        <div class="password-wrap">
          <input v-model="password" :type="showPassword ? 'text' : 'password'" placeholder="••••••••" />
          <button class="eye" type="button" @click="showPassword = !showPassword">
            {{ showPassword ? '🙈' : '👁️' }}
          </button>
        </div>

        <button class="submit" type="button" @click="submit">Iniciar Sesión</button>
        <p v-if="error" class="error">{{ error }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-screen {
  min-height: 100vh;
  display: grid;
  place-items: center;
  background: linear-gradient(160deg, #f8f9ff 0%, #e8eeff 100%);
  padding: 24px;
}

.login-wrap { width: 100%; max-width: 420px; }

.brand { display: flex; align-items: center; gap: 12px; margin-bottom: 6px; }
.brand-icon {
  width: 52px; height: 52px; border-radius: 14px;
  display: grid; place-items: center;
  color: #fff; font-weight: 800; font-size: 1.7rem;
  background: linear-gradient(135deg, #4157ff 0%, #953cff 100%);
}
.brand h1 { margin: 0; font-size: 2.2rem; color: #25304f; }
.subtitle { margin: 0 0 14px; color: #5a6485; }

.panel {
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.9);
  border: 1px solid #e6e9ff;
  box-shadow: 0 25px 60px -40px rgba(35, 46, 88, 0.45);
  padding: 24px;
}

.panel h2 { margin: 0 0 12px; font-size: .95rem; color: #3f4a70; }
.roles { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-bottom: 8px; }
.role-btn {
  border-radius: 14px; border: 1px solid #d8dff6; background: #fff;
  padding: 10px 6px; display: flex; flex-direction: column; align-items: center; gap: 6px;
  color: #4f5a7a; font-weight: 600; cursor: pointer;
}
.role-btn.active {
  border-color: transparent;
  color: #fff;
  background: linear-gradient(135deg, #4157ff 0%, #953cff 100%);
}
.emoji { font-size: 1.1rem; }
.hint { text-align: center; font-size: .82rem; color: #566188; margin: 10px 0 16px; }

label { font-size: .84rem; color: #414c71; font-weight: 600; display: block; margin-bottom: 6px; }
input {
  width: 100%; box-sizing: border-box; height: 46px;
  border-radius: 14px; border: 1px solid #d5dcf3; background: #fff;
  padding: 0 12px; margin-bottom: 12px;
}
input:focus { outline: none; border-color: #7f8cff; box-shadow: 0 0 0 4px #dce2ff; }

.password-wrap { position: relative; }
.password-wrap input { padding-right: 46px; margin-bottom: 16px; }
.eye {
  position: absolute; right: 10px; top: 9px;
  border: none; background: transparent; width: 28px; cursor: pointer;
}

.submit {
  width: 100%; border: none; height: 48px; border-radius: 16px;
  color: #fff; font-weight: 700; cursor: pointer;
  background: linear-gradient(90deg, #4157ff 0%, #953cff 100%);
}
.error { color: #d3204c; font-size: .84rem; margin: 10px 0 0; }
</style>
