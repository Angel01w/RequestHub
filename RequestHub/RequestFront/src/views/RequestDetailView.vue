<script setup>
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import api from '../services/api'

const route = useRoute()
const detail = ref(null)
const comment = ref('')

const load = async () => {
  const { data } = await api.get(`/servicerequests/${route.params.id}`)
  detail.value = data
}

const addComment = async () => {
  await api.post(`/servicerequests/${route.params.id}/comments`, { text: comment.value })
  comment.value = ''
  await load()
}

onMounted(load)
</script>

<template>
	<h2>Detalle de Solicitud</h2>
	<div v-if="detail" class="card">
		<h3>{{ detail.number }} - {{ detail.subject }}</h3>
		<p>{{ detail.description }}</p>
		<p>Estado: {{ detail.status }} | Área: {{ detail.area }}</p>
		<h4>Comentarios</h4>
		<ul><li v-for="c in detail.comments" :key="c.id">{{ c.user }}: {{ c.text }}</li></ul>
		<textarea v-model="comment" placeholder="Agregar comentario"></textarea>
		<button @click="addComment">Comentar</button>
		<h4>Historial</h4>
		<ul><li v-for="h in detail.history" :key="h.id">{{ h.createdAtUtc }} - {{ h.action }}</li></ul>
	</div>
</template>
