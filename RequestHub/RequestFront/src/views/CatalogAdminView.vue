<script setup>
import { ref, onMounted } from 'vue'
import api from '../services/api'

const areas = ref([])
const priorities = ref([])
const requestTypes = ref([])
const newArea = ref('')

const load = async () => {
  const [a,p,t] = await Promise.all([
    api.get('/catalogs/areas'),
    api.get('/catalogs/priorities'),
    api.get('/catalogs/request-types')
  ])
  areas.value = a.data
  priorities.value = p.data
  requestTypes.value = t.data
}

const createArea = async () => {
  await api.post('/catalogs/areas', { name: newArea.value })
  newArea.value = ''
  await load()
}

onMounted(load)
</script>

<template>
	<h2>Administración de Catálogos</h2>
	<div class="card">
		<h3>Áreas</h3>
		<ul><li v-for="a in areas" :key="a.id">{{ a.name }}</li></ul>
		<input v-model="newArea" placeholder="Nueva área" />
		<button @click="createArea">Crear área</button>
	</div>
	<div class="card">
		<h3>Prioridades</h3>
		<ul><li v-for="p in priorities" :key="p.id">{{ p.name }}</li></ul>
	</div>
	<div class="card">
		<h3>Tipos de Solicitud</h3>
		<ul><li v-for="t in requestTypes" :key="t.id">{{ t.name }} (Área {{ t.areaId }})</li></ul>
	</div>
</template>
