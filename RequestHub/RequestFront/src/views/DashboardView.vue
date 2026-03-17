<script setup>
	import { computed, onMounted, ref } from "vue"
	import { useRouter } from "vue-router"
	import {
		buildUniqueAreaOptions,
		buildUniqueStatusOptions,
		fetchServiceRequests,
		filterServiceRequests
	} from "../../../Application/Services/serviceRequests.service.js"

	const router = useRouter()

	const isLoading = ref(false)
	const loadError = ref("")
	const requests = ref([])
	const deletingId = ref(null)

	const filters = ref({
		search: "",
		status: "",
		area: "",
		date: ""
	})

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")
	const MY_REQUESTS_PATH = "/mis-solicitudes"

	function getToken() {
		return localStorage.getItem("rh_token") || localStorage.getItem("token") || ""
	}

	function iconPath(name) {
		switch (name) {
			case "grid":
				return "M4 4h7v7H4V4Zm9 0h7v7h-7V4ZM4 13h7v7H4v-7Zm9 0h7v7h-7v-7Z"
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3"
			case "calendar":
				return "M7 3v3M17 3v3M4 8h16M5 6h14v15H5V6Z"
			case "spark":
				return "M12 2l1.4 5.1L18.5 8.5l-5.1 1.4L12 15l-1.4-5.1L5.5 8.5l5.1-1.4L12 2Z"
			case "clock":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm0-11v5l3 2"
			case "done":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.2 2.2L16.8 7.8"
			case "check":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.3 2.3L16.5 8"
			case "eye":
				return "M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Zm10 3a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z"
			case "edit":
				return "M4 20h4l10-10-4-4L4 16v4Zm9-13 4 4"
			case "trash":
				return "M6 7h12M9 7V5h6v2M8 7l1 13h6l1-13"
			default:
				return ""
		}
	}

	function toValidDate(value) {
		if (!value) return null

		if (value instanceof Date && !Number.isNaN(value.getTime())) {
			return value
		}

		if (typeof value === "string") {
			const trimmed = value.trim()
			if (!trimmed) return null

			const normalized = /^\d{4}-\d{2}-\d{2}$/.test(trimmed)
				? `${trimmed}T00:00:00`
				: trimmed

			const parsed = new Date(normalized)
			if (!Number.isNaN(parsed.getTime())) {
				return parsed
			}
		}

		const parsed = new Date(value)
		return Number.isNaN(parsed.getTime()) ? null : parsed
	}

	function formatDateLabel(value) {
		const date = toValidDate(value)
		if (!date) return "N/D"

		return date.toLocaleDateString("es-DO", {
			day: "2-digit",
			month: "2-digit",
			year: "numeric"
		})
	}

	function formatDateInputValue(value) {
		const date = toValidDate(value)
		if (!date) return ""

		const year = date.getFullYear()
		const month = String(date.getMonth() + 1).padStart(2, "0")
		const day = String(date.getDate()).padStart(2, "0")

		return `${year}-${month}-${day}`
	}

	function resolveRequestDate(request) {
		return (
			request?.createdAt ??
			request?.createdOn ??
			request?.createdDate ??
			request?.requestDate ??
			request?.dateCreated ??
			request?.fechaCreacion ??
			request?.fecha ??
			request?.submittedAt ??
			request?.createdAtUtc ??
			request?.raw?.createdAt ??
			request?.raw?.createdAtUtc ??
			null
		)
	}

	function normalizeStatusName(value) {
		const normalized = String(value ?? "").trim().toLowerCase()

		if (!normalized) return ""
		if (normalized === "nueva" || normalized === "new") return "Nueva"
		if (normalized === "en proceso" || normalized === "enproceso" || normalized === "in progress" || normalized === "inprogress") return "En Proceso"
		if (normalized === "resuelta" || normalized === "resolved") return "Resuelta"
		if (normalized === "cerrada" || normalized === "closed") return "Cerrada"
		if (normalized === "rechazada" || normalized === "rejected") return "Rechazada"

		return String(value ?? "").trim()
	}

	function buildStatusKey(statusName) {
		const normalized = String(statusName ?? "").trim().toLowerCase()

		if (normalized === "nueva") return "new"
		if (normalized === "en proceso") return "in-progress"
		if (normalized === "resuelta") return "resolved"
		if (normalized === "cerrada") return "closed"
		if (normalized === "rechazada") return "rejected"

		return "unknown"
	}

	function normalizeRequest(request) {
		const rawDate = resolveRequestDate(request)

		const requestNumber =
			request?.requestNumber ||
			request?.number ||
			request?.raw?.requestNumber ||
			request?.raw?.number ||
			""

		const areaName =
			request?.areaName ||
			request?.area ||
			request?.raw?.areaName ||
			request?.raw?.area ||
			""

		const priorityName =
			request?.priorityName ||
			request?.priority ||
			request?.raw?.priorityName ||
			request?.raw?.priority ||
			""

		const statusName = normalizeStatusName(
			request?.statusName ||
			request?.status ||
			request?.raw?.statusName ||
			request?.raw?.status ||
			""
		)

		return {
			...request,
			id: request?.id ?? request?.raw?.id ?? request?.raw?.requestId ?? request?.raw?.serviceRequestId ?? null,
			rawDate,
			createdAtLabel: request?.createdAtLabel || formatDateLabel(rawDate),
			dateFilterValue: formatDateInputValue(rawDate),
			requestNumber,
			areaName,
			priorityName,
			statusName,
			statusKey: request?.statusKey || buildStatusKey(statusName),
			canEdit: Boolean(request?.canEdit ?? request?.raw?.canEdit),
			canDelete: Boolean(request?.canDelete ?? request?.raw?.canDelete)
		}
	}

	async function apiRequest(path, { method = "GET" } = {}) {
		const token = getToken()

		const headers = {
			Accept: "application/json"
		}

		if (token) {
			headers.Authorization = `Bearer ${token}`
		}

		const response = await fetch(`${API_BASE}${path}`, {
			method,
			headers
		})

		if (!response.ok) {
			let message = `HTTP ${response.status}`

			try {
				const data = await response.clone().json()
				message = data?.message || data?.title || message
			} catch {
				try {
					const text = await response.clone().text()
					if (text) message = text
				} catch {
				}
			}

			throw new Error(message)
		}

		if (response.status === 204) return null

		const contentType = response.headers.get("content-type") || ""
		if (contentType.includes("application/json")) return await response.json()
		return await response.text()
	}

	async function loadRequests() {
		isLoading.value = true
		loadError.value = ""

		try {
			const data = await fetchServiceRequests()
			requests.value = Array.isArray(data) ? data.map(normalizeRequest) : []
		} catch (error) {
			loadError.value = error?.message || "No se pudieron cargar las solicitudes."
			requests.value = []
		} finally {
			isLoading.value = false
		}
	}

	function openRequest(request) {
		if (!request?.id) return

		router.push({
			name: "RequestDetail",
			params: { id: request.id }
		})
	}

	function editRequest(request) {
		if (!request?.id || !request?.canEdit) return

		router.push({
			path: MY_REQUESTS_PATH,
			query: {
				editId: String(request.id),
				mode: "edit",
				t: String(Date.now())
			}
		})
	}

	async function removeRequest(request) {
		if (!request?.id || !request?.canDelete) return

		const confirmed = window.confirm(`¿Deseas eliminar la solicitud ${request.requestNumber || ""}?`)
		if (!confirmed) return

		deletingId.value = request.id
		loadError.value = ""

		try {
			await apiRequest(`/api/ServiceRequests/${request.id}`, {
				method: "DELETE"
			})

			await loadRequests()
		} catch (error) {
			loadError.value = error?.message || "No se pudo eliminar la solicitud."
		} finally {
			deletingId.value = null
		}
	}

	const statusOptions = computed(() => buildUniqueStatusOptions(requests.value))
	const areaOptions = computed(() => buildUniqueAreaOptions(requests.value))

	const filteredRequests = computed(() => {
		const baseFiltered = filterServiceRequests(requests.value, {
			search: filters.value.search,
			status: filters.value.status,
			area: filters.value.area,
			date: ""
		})

		if (!filters.value.date) return baseFiltered

		return baseFiltered.filter(request => request.dateFilterValue === filters.value.date)
	})

	const summary = computed(() => {
		const source = requests.value

		return {
			total: source.length,
			inProgress: source.filter(x => x.statusKey === "in-progress").length,
			resolved: source.filter(x => x.statusKey === "resolved").length,
			new: source.filter(x => x.statusKey === "new").length
		}
	})

	onMounted(loadRequests)
</script>

<template>
	<div class="dash-content">
		<section class="content">
			<div class="heading">
				<h1>Dashboard</h1>
				<p>
					<span class="miniicon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('grid')" /></svg>
					</span>
					Vista general del sistema de solicitudes internas
				</p>
			</div>

			<div class="cards">
				<div class="card">
					<div class="card__left">
						<div class="card__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('check')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.total }}</div>
							<div class="card__label">Total</div>
						</div>
					</div>
					<div class="card__delta" />
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--amber" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('clock')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.inProgress }}</div>
							<div class="card__label">En Proceso</div>
						</div>
					</div>
					<div class="card__delta" />
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--teal" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('done')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.resolved }}</div>
							<div class="card__label">Resueltas</div>
						</div>
					</div>
					<div class="card__delta" />
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--violet" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('spark')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.new }}</div>
							<div class="card__label">Nuevas</div>
						</div>
					</div>
					<div class="card__delta" />
				</div>
			</div>

			<div class="filters">
				<div class="pill pill--search">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
					</span>
					<input v-model.trim="filters.search"
						   class="pill__input"
						   type="text"
						   placeholder="Buscar por número o asunto..." />
				</div>

				<label class="pill pill--select">
					<select v-model="filters.status" class="pill__select">
						<option value="">Todos los Estados</option>
						<option v-for="option in statusOptions"
								:key="option.value"
								:value="option.value">
							{{ option.label }}
						</option>
					</select>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</label>

				<label class="pill pill--select">
					<select v-model="filters.area" class="pill__select">
						<option value="">Todas las Áreas</option>
						<option v-for="option in areaOptions"
								:key="option.value"
								:value="option.value">
							{{ option.label }}
						</option>
					</select>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</label>

				<label class="pill pill--date">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
					</span>
					<input v-model="filters.date" class="pill__date" type="date" />
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</label>
			</div>

			<div v-if="loadError" class="statebox statebox--error">
				{{ loadError }}
			</div>

			<div class="tablewrap">
				<table class="table">
					<thead>
						<tr>
							<th>NÚMERO DE<br />SOLICITUD</th>
							<th>ASUNTO</th>
							<th>ÁREA</th>
							<th>PRIORIDAD</th>
							<th>ESTADO</th>
							<th>FECHA</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody v-if="!isLoading && filteredRequests.length">
						<tr v-for="request in filteredRequests" :key="request.id || request.requestNumber">
							<td class="cell-number">{{ request.requestNumber || "N/D" }}</td>
							<td class="cell-subject">{{ request.subject || "Sin asunto" }}</td>
							<td>{{ request.areaName || "N/D" }}</td>
							<td>{{ request.priorityName || "N/D" }}</td>
							<td>
								<span class="status-badge"
									  :class="`status-badge--${request.statusKey || 'unknown'}`">
									{{ request.statusName || "N/D" }}
								</span>
							</td>
							<td>{{ request.createdAtLabel }}</td>
							<td class="td-actions">
								<div class="actions-row">
									<button class="action-btn" type="button" @click="openRequest(request)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('eye')" /></svg>
									</button>

									<button v-if="request.canEdit"
											class="action-btn action-btn--edit"
											type="button"
											@click="editRequest(request)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('edit')" /></svg>
									</button>

									<button v-if="request.canDelete"
											class="action-btn action-btn--delete"
											type="button"
											:disabled="deletingId === request.id"
											@click="removeRequest(request)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
									</button>
								</div>
							</td>
						</tr>
					</tbody>
					<tbody v-else>
						<tr>
							<td colspan="7" class="table-empty">
								{{ isLoading ? "Cargando solicitudes..." : "No hay solicitudes para mostrar." }}
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</section>

		<button class="help" type="button" aria-label="Ayuda">?</button>
	</div>
</template>

<style scoped>
	.dash-content {
		min-height: 100vh;
		position: relative;
	}

	.content {
		padding: 22px 22px 28px;
	}

	.heading h1 {
		margin: 0;
		font-size: 26px;
		font-weight: 900;
		color: #232a52;
		letter-spacing: -0.4px;
	}

	.heading p {
		margin: 6px 0 0;
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.62);
	}

	.miniicon {
		width: 18px;
		height: 18px;
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
	}

		.miniicon svg {
			width: 16px;
			height: 16px;
		}

		.miniicon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.cards {
		margin-top: 16px;
		display: grid;
		grid-template-columns: repeat(4, minmax(0, 1fr));
		gap: 12px;
	}

	.card {
		border-radius: 16px;
		background: rgba(255, 255, 255, 0.58);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 18px 34px rgba(40, 55, 95, 0.1);
		padding: 14px;
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.card__left {
		display: flex;
		align-items: center;
		gap: 12px;
	}

	.card__icon {
		width: 36px;
		height: 36px;
		border-radius: 14px;
		background: rgba(120, 105, 235, 0.16);
		display: grid;
		place-items: center;
		color: #6b5cff;
	}

	.card__icon--amber {
		color: #c8801b;
		background: rgba(255, 197, 90, 0.22);
	}

	.card__icon--teal {
		color: #2c8a6a;
		background: rgba(60, 196, 151, 0.14);
	}

	.card__icon--violet {
		color: #5b49c6;
		background: rgba(150, 122, 255, 0.18);
	}

	.card__icon svg {
		width: 18px;
		height: 18px;
	}

	.card__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.card__value {
		font-weight: 900;
		font-size: 20px;
		color: #232a52;
		line-height: 1;
		min-width: 24px;
	}

	.card__label {
		margin-top: 4px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.62);
	}

	.card__delta {
		min-width: 54px;
	}

	.filters {
		margin-top: 14px;
		display: grid;
		grid-template-columns: 1.45fr 0.85fr 0.85fr 0.85fr;
		gap: 10px;
	}

	.pill {
		height: 42px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.62);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		padding: 0 12px;
		display: flex;
		align-items: center;
		gap: 10px;
		color: rgba(39, 46, 86, 0.82);
	}

	.pill--search {
		cursor: text;
	}

	.pill--select,
	.pill--date {
		position: relative;
		cursor: pointer;
	}

	.pill__icon {
		width: 28px;
		height: 28px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.14);
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
		flex: 0 0 auto;
	}

		.pill__icon svg {
			width: 16px;
			height: 16px;
		}

		.pill__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.pill__input,
	.pill__select,
	.pill__date {
		border: none;
		outline: none;
		background: transparent;
		flex: 1 1 auto;
		min-width: 0;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		font-family: inherit;
	}

		.pill__input::placeholder {
			color: rgba(39, 46, 86, 0.4);
		}

	.pill__select {
		appearance: none;
		cursor: pointer;
		padding-right: 18px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.78);
	}

	.pill__date {
		cursor: pointer;
	}

	.pill__chev {
		margin-left: auto;
		color: rgba(39, 46, 86, 0.38);
		display: grid;
		place-items: center;
		pointer-events: none;
	}

		.pill__chev svg {
			width: 16px;
			height: 16px;
		}

		.pill__chev path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.statebox {
		margin-top: 12px;
		padding: 12px 14px;
		border-radius: 14px;
		font-size: 13px;
		font-weight: 700;
	}

	.statebox--error {
		background: rgba(255, 95, 95, 0.1);
		border: 1px solid rgba(255, 95, 95, 0.18);
		color: #8a2c4b;
	}

	.tablewrap {
		margin-top: 12px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.58);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 22px 44px rgba(40, 55, 95, 0.12);
		overflow: hidden;
	}

	.table {
		width: 100%;
		border-collapse: separate;
		border-spacing: 0;
	}

	thead th {
		padding: 12px 14px;
		font-size: 10px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.62);
		text-align: left;
		background: linear-gradient(180deg, rgba(240, 236, 255, 0.7), rgba(255, 255, 255, 0.4));
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
	}

	tbody td {
		padding: 14px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
		vertical-align: middle;
	}

	tbody tr:last-child td {
		border-bottom: none;
	}

	.cell-number {
		font-weight: 800;
		color: #232a52;
	}

	.cell-subject {
		max-width: 280px;
	}

	.th-actions,
	.td-actions {
		text-align: right;
	}

	.actions-row {
		display: flex;
		justify-content: flex-end;
		align-items: center;
		gap: 8px;
	}

	.status-badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-height: 28px;
		padding: 0 10px;
		border-radius: 999px;
		font-size: 11px;
		font-weight: 900;
		white-space: nowrap;
	}

	.status-badge--new {
		background: rgba(120, 105, 235, 0.14);
		color: #5b49c6;
	}

	.status-badge--in-progress {
		background: rgba(255, 197, 90, 0.22);
		color: #c8801b;
	}

	.status-badge--resolved {
		background: rgba(60, 196, 151, 0.14);
		color: #2c8a6a;
	}

	.status-badge--closed {
		background: rgba(184, 194, 218, 0.2);
		color: rgba(72, 89, 125, 0.98);
	}

	.status-badge--rejected {
		background: rgba(255, 90, 130, 0.12);
		color: rgba(170, 40, 80, 0.98);
	}

	.status-badge--unknown {
		background: rgba(94, 106, 210, 0.12);
		color: #4c5699;
	}

	.action-btn {
		width: 34px;
		height: 34px;
		border: 1px solid rgba(110, 102, 182, 0.14);
		border-radius: 12px;
		background: rgba(255, 255, 255, 0.75);
		display: inline-grid;
		place-items: center;
		cursor: pointer;
		color: #5b49c6;
	}

	.action-btn--edit {
		color: #5b49c6;
	}

	.action-btn--delete {
		color: #c04a62;
	}

	.action-btn:disabled {
		opacity: 0.65;
		cursor: not-allowed;
	}

	.action-btn svg {
		width: 16px;
		height: 16px;
	}

	.action-btn path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.table-empty {
		text-align: center;
		padding: 28px 14px;
		color: rgba(39, 46, 86, 0.56);
		font-weight: 700;
	}

	.help {
		position: fixed;
		right: 18px;
		bottom: 18px;
		width: 40px;
		height: 40px;
		border-radius: 16px;
		border: 1px solid rgba(110, 102, 182, 0.16);
		background: rgba(85, 76, 210, 0.7);
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 18px 34px rgba(40, 55, 95, 0.2);
	}

	@media (max-width: 1100px) {
		.cards {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}

		.filters {
			grid-template-columns: 1fr 1fr;
		}
	}

	@media (max-width: 860px) {
		.content {
			padding: 18px 14px 24px;
		}

		.tablewrap {
			overflow-x: auto;
		}

		.table {
			min-width: 820px;
		}
	}
</style>