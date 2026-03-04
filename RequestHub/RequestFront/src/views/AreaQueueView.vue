<script setup>
import { ref, onMounted } from 'vue'
import api from '../services/api'

const requests = ref([])
const load = async () => {
  const { data } = await api.get('/servicerequests')
  requests.value = data
}
const take = async id => { await api.post(`/servicerequests/${id}/take`); await load() }
onMounted(load)
</script>

<template>
	<h2>Bandeja del Área</h2>
	<div class="card" v-for="r in requests" :key="r.id">
		<strong>{{ r.number }}</strong> - {{ r.subject }}
		<p>Estado: {{ r.status }}</p>
		<button @click="take(r.id)">Tomar</button>
		<router-link :to="`/solicitudes/${r.id}`">Detalle</router-link>
	</div>
</template>
