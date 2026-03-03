<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const username = ref('')
const password = ref('')
const error = ref('')
const router = useRouter()
const auth = useAuthStore()

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
  <div class="card" style="max-width:500px;margin:auto;">
    <h2>Login</h2>
    <label>Usuario</label>
    <input v-model="username" />
    <label>Contraseña</label>
    <input type="password" v-model="password" />
    <button @click="submit">Ingresar</button>
    <p v-if="error" style="color:red">{{ error }}</p>
  </div>
</template>
