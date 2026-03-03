<script setup>
import { ref, onMounted } from 'vue'
import api from '../services/api'

const requests = ref([])
const form = ref({ areaId: 1, requestTypeId: 1, subject: '', description: '', priorityId: 2 })

const load = async () => {
  const { data } = await api.get('/servicerequests')
  requests.value = data
}

const create = async () => {
  const fd = new FormData()
  Object.entries(form.value).forEach(([k, v]) => fd.append(k, v))
  await api.post('/servicerequests', fd)
  form.value.subject = ''
  form.value.description = ''
  await load()
}

onMounted(load)
</script>

<template>
  <h2>Mis Solicitudes</h2>
  <div class="card">
    <h3>Nueva Solicitud</h3>
    <div class="grid">
      <div><label>Área ID</label><input type="number" v-model="form.areaId" /></div>
      <div><label>Tipo ID</label><input type="number" v-model="form.requestTypeId" /></div>
      <div><label>Prioridad ID</label><input type="number" v-model="form.priorityId" /></div>
    </div>
    <label>Asunto</label><input v-model="form.subject" />
    <label>Descripción</label><textarea v-model="form.description"></textarea>
    <button @click="create">Crear solicitud</button>
  </div>

  <div class="card" v-for="r in requests" :key="r.id">
    <strong>{{ r.number }}</strong> - {{ r.subject }}
    <p>Estado: {{ r.status }} | Área: {{ r.area }} | Prioridad: {{ r.priority }}</p>
    <router-link :to="`/solicitudes/${r.id}`">Ver detalle</router-link>
  </div>
</template>
