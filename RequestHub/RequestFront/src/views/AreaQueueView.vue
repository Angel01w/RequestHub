<script setup>
	import { computed, onMounted, ref, watch } from "vue"
	import { useRouter } from "vue-router"

	const router = useRouter()

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")

	const isLoading = ref(false)
	const loadError = ref("")
	const search = ref("")
	const requests = ref([])
	const areas = ref([])
	const allRequestTypes = ref([])
	const requestTypes = ref([])
	const isLoadingAreas = ref(false)
	const isLoadingTypes = ref(false)
	const takingId = ref(null)
	const deletingId = ref(null)
	const isEditOpen = ref(false)
	const isSubmitting = ref(false)
	const submitError = ref("")
	const activeDropdown = ref(null)
	const editingRequestId = ref(null)

	const filters = ref({
		status: "active",
		assignment: "",
		priority: "",
		datePreset: "7d",
		dateFrom: "",
		dateTo: ""
	})

	const form = ref(createDefaultForm())

	const priorityCatalog = Object.freeze([
		{ id: 1, label: "Baja", tone: "low" },
		{ id: 2, label: "Media", tone: "medium" },
		{ id: 3, label: "Alta", tone: "high" }
	])

	const statusCatalog = Object.freeze([
		{ id: 1, label: "Nueva", tone: "new" },
		{ id: 2, label: "En Proceso", tone: "progress" },
		{ id: 3, label: "Resuelta", tone: "done" },
		{ id: 4, label: "Cerrada", tone: "closed" },
		{ id: 5, label: "Rechazada", tone: "rejected" }
	])

	const assignmentOptions = Object.freeze([
		{ value: "", label: "Todas" },
		{ value: "unassigned", label: "Sin asignar" },
		{ value: "assigned", label: "Asignadas" }
	])

	const dateOptions = Object.freeze([
		{ value: "all", label: "Todas las fechas" },
		{ value: "today", label: "Hoy" },
		{ value: "7d", label: "Última semana" },
		{ value: "30d", label: "Últimos 30 días" },
		{ value: "month", label: "Este mes" },
		{ value: "custom", label: "Rango personalizado" }
	])

	const userCard = computed(() => {
		try {
			const raw = localStorage.getItem("sm_user") || localStorage.getItem("user") || localStorage.getItem("rh_user")
			if (!raw) {
				return {
					initial: "J",
					name: "Gestor",
					role: "Gestor",
					area: "Bandeja del Área"
				}
			}

			const parsed = JSON.parse(raw)
			const name =
				parsed.fullName ||
				parsed.name ||
				parsed.username ||
				parsed.userName ||
				"Gestor"

			const role =
				parsed.roleName ||
				parsed.role ||
				"Gestor"

			const area =
				parsed.areaName ||
				parsed.area ||
				"Bandeja del Área"

			return {
				initial: String(name).trim().charAt(0).toUpperCase() || "J",
				name,
				role,
				area
			}
		} catch {
			return {
				initial: "J",
				name: "Gestor",
				role: "Gestor",
				area: "Bandeja del Área"
			}
		}
	})

	const statusFilterLabel = computed(() => {
		switch (filters.value.status) {
			case "active":
				return "Nuevas y En Proceso"
			case "all":
				return "Todos los estados"
			case "new":
				return "Nuevas"
			case "progress":
				return "En Proceso"
			case "resolved":
				return "Resueltas"
			case "rejected":
				return "Rechazadas"
			case "closed":
				return "Cerradas"
			default:
				return "Nuevas y En Proceso"
		}
	})

	const assignmentFilterLabel = computed(() => {
		const found = assignmentOptions.find(x => x.value === filters.value.assignment)
		return found?.label || "Todas"
	})

	const priorityFilterLabel = computed(() => {
		if (!filters.value.priority) return "Alta, Media, Baja"
		return filters.value.priority
	})

	const dateFilterLabel = computed(() => {
		if (filters.value.dateFrom || filters.value.dateTo) return "Rango personalizado"
		const found = dateOptions.find(x => x.value === filters.value.datePreset)
		return found?.label || "Última semana"
	})

	const hasRequests = computed(() => filteredRequests.value.length > 0)

	const summary = computed(() => ({
		newCount: requests.value.filter(x => x.statusKey === "new").length,
		progressCount: requests.value.filter(x => x.statusKey === "progress").length,
		resolvedCount: requests.value.filter(x => x.statusKey === "done").length,
		rejectedCount: requests.value.filter(x => x.statusKey === "rejected").length
	}))

	const filteredRequests = computed(() => {
		const term = search.value.trim().toLowerCase()
		const selectedPriority = String(filters.value.priority || "").trim().toLowerCase()
		const range = resolveDateRange(filters.value.datePreset, filters.value.dateFrom, filters.value.dateTo)

		return requests.value.filter(item => {
			const matchesSearch =
				!term ||
				String(item.requestNumber || "").toLowerCase().includes(term) ||
				String(item.subject || "").toLowerCase().includes(term)

			const matchesStatus = (() => {
				switch (filters.value.status) {
					case "all":
						return true
					case "active":
						return item.statusKey === "new" || item.statusKey === "progress"
					case "new":
						return item.statusKey === "new"
					case "progress":
						return item.statusKey === "progress"
					case "resolved":
						return item.statusKey === "done"
					case "rejected":
						return item.statusKey === "rejected"
					case "closed":
						return item.statusKey === "closed"
					default:
						return true
				}
			})()

			const matchesAssignment = (() => {
				if (!filters.value.assignment) return true
				if (filters.value.assignment === "assigned") return !!item.assignedToUserId
				if (filters.value.assignment === "unassigned") return !item.assignedToUserId
				return true
			})()

			const matchesPriority =
				!selectedPriority ||
				String(item.priority || "").trim().toLowerCase() === selectedPriority

			const createdAtDate = parseDateValue(item.createdAtUtc)

			const matchesDate =
				!range ||
				(createdAtDate &&
					createdAtDate.getTime() >= range.start.getTime() &&
					createdAtDate.getTime() <= range.end.getTime())

			return matchesSearch && matchesStatus && matchesAssignment && matchesPriority && matchesDate
		})
	})

	function createDefaultForm() {
		return {
			areaId: "",
			typeId: "",
			priority: "",
			status: "Nueva",
			rejectionReason: "",
			subject: "",
			description: ""
		}
	}

	function resetForm() {
		form.value = createDefaultForm()
		requestTypes.value = []
		submitError.value = ""
		editingRequestId.value = null
	}

	function onBack() {
		router.back()
	}

	function iconPath(name) {
		switch (name) {
			case "arrowLeft":
				return "M15 18l-6-6 6-6"
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3"
			case "chev":
				return "M7 10l5 5 5-5"
			case "calendar":
				return "M7 3v3M17 3v3M4 8h16M5 6h14v15H5V6Z"
			case "clock":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm0-11v5l3 2"
			case "spark":
				return "M12 2l1.4 5.1L18.5 8.5l-5.1 1.4L12 15l-1.4-5.1L5.5 8.5l5.1-1.4L12 2Z"
			case "done":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.2 2.2L16.8 7.8"
			case "ban":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-5-5 10-10"
			case "user":
				return "M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Zm-8 9a8 8 0 0 1 16 0"
			case "eye":
				return "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Zm10 3a3 3 0 1 0-3-3 3 3 0 0 0 3 3Z"
			case "take":
				return "M12 3v18M3 12h18"
			case "edit":
				return "M4 20h4l10.5-10.5a2.121 2.121 0 0 0-3-3L5 17v3Z"
			case "trash":
				return "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"
			default:
				return ""
		}
	}

	function firstNonEmpty(...values) {
		for (const value of values) {
			if (value === 0) return value
			if (value === false) return value
			if (value == null) continue
			if (typeof value === "string" && !value.trim()) continue
			return value
		}
		return ""
	}

	function normalizeStatusKey(value) {
		return String(value || "").trim().toLowerCase().replace(/\s+/g, "")
	}

	function canonicalStatusLabel(value) {
		const normalized = normalizeStatusKey(value)
		if (normalized === "nueva") return "Nueva"
		if (normalized === "enproceso") return "En Proceso"
		if (normalized === "resuelta") return "Resuelta"
		if (normalized === "cerrada") return "Cerrada"
		if (normalized === "rechazada") return "Rechazada"
		return typeof value === "string" ? value.trim() : ""
	}

	function resolveStatusTone(value) {
		const normalized = normalizeStatusKey(value)
		if (normalized === "nueva") return "new"
		if (normalized === "enproceso") return "progress"
		if (normalized === "resuelta") return "done"
		if (normalized === "cerrada") return "closed"
		if (normalized === "rechazada") return "rejected"
		return "neutral"
	}

	function resolvePriorityLabel(priorityValue, priorityId) {
		if (typeof priorityValue === "string" && priorityValue.trim()) return priorityValue.trim()
		if (priorityValue && typeof priorityValue === "object") {
			return priorityValue.name ?? priorityValue.nombre ?? priorityValue.label ?? ""
		}
		if (Number(priorityId) === 1) return "Baja"
		if (Number(priorityId) === 2) return "Media"
		if (Number(priorityId) === 3) return "Alta"
		return ""
	}

	function resolveStatusLabel(statusValue, statusId) {
		if (typeof statusValue === "string" && statusValue.trim()) {
			return canonicalStatusLabel(statusValue)
		}
		if (statusValue && typeof statusValue === "object") {
			return canonicalStatusLabel(
				statusValue.name ??
				statusValue.nombre ??
				statusValue.label ??
				statusValue.statusName ??
				""
			)
		}
		if (Number(statusId) === 1) return "Nueva"
		if (Number(statusId) === 2) return "En Proceso"
		if (Number(statusId) === 3) return "Resuelta"
		if (Number(statusId) === 4) return "Cerrada"
		if (Number(statusId) === 5) return "Rechazada"
		return "Nueva"
	}

	function getPriorityId(priority) {
		const normalized = String(priority || "").trim().toLowerCase()
		const found = priorityCatalog.find(x => x.label.toLowerCase() === normalized)
		return found?.id ?? 0
	}

	function getStatusId(status) {
		const normalized = normalizeStatusKey(status)
		const found = statusCatalog.find(x => normalizeStatusKey(x.label) === normalized)
		return found?.id ?? 1
	}

	function normalizeRequest(item, index) {
		const priorityId = Number(
			firstNonEmpty(
				item.priorityId,
				item.PriorityId,
				item.idPriority,
				item.idPrioridad,
				item.priority?.id,
				item.Priority?.Id
			)
		) || 0

		const statusId = Number(
			firstNonEmpty(
				item.statusId,
				item.StatusId,
				item.requestStatusId,
				item.RequestStatusId,
				item.status?.id,
				item.Status?.Id
			)
		) || 0

		const createdAtUtc = firstNonEmpty(
			item.createdAtUtc,
			item.createdAt,
			item.CreatedAtUtc,
			item.CreatedAt,
			item.creationDate,
			item.date,
			new Date().toISOString()
		)

		const status = resolveStatusLabel(
			firstNonEmpty(
				item.statusName,
				item.StatusName,
				item.status,
				item.Status,
				item.estado
			),
			statusId
		)

		return {
			id: firstNonEmpty(item.id, item.serviceRequestId, item.requestId, index + 1),
			requestNumber: firstNonEmpty(item.number, item.requestNumber, item.code, `SOL-${String(index + 1).padStart(4, "0")}`),
			subject: firstNonEmpty(item.subject, item.Subject, ""),
			description: firstNonEmpty(item.description, item.Description, ""),
			areaId: firstNonEmpty(item.areaId, item.AreaId, item.area?.id, ""),
			areaName: firstNonEmpty(item.area, item.areaName, item.Area, item.area?.name, ""),
			typeId: firstNonEmpty(item.requestTypeId, item.typeId, item.RequestTypeId, ""),
			typeName: firstNonEmpty(item.requestType, item.typeName, item.RequestType, item.requestType?.name, ""),
			priorityId,
			priority: resolvePriorityLabel(firstNonEmpty(item.priority, item.priorityName, item.Priority), priorityId),
			statusId: statusId || getStatusId(status),
			status,
			statusKey: resolveStatusTone(status),
			rejectionReason: firstNonEmpty(item.rejectionReason, item.RejectionReason, ""),
			createdAtUtc,
			assignedToUserId: firstNonEmpty(item.assignedToUserId, item.AssignedToUserId, null),
			raw: item
		}
	}

	function normalizeAreasPayload(rawData) {
		let list = []

		if (Array.isArray(rawData)) list = rawData
		else if (Array.isArray(rawData?.items)) list = rawData.items
		else if (Array.isArray(rawData?.data)) list = rawData.data
		else if (Array.isArray(rawData?.result)) list = rawData.result
		else if (Array.isArray(rawData?.areas)) list = rawData.areas
		else if (Array.isArray(rawData?.value)) list = rawData.value

		return list
			.map(a => ({
				id: a.id ?? a.areaId ?? a.Id ?? a.AreaId ?? a.idArea ?? "",
				name: a.name ?? a.nombre ?? a.Name ?? a.Nombre ?? ""
			}))
			.filter(a => a.id !== "" && a.name !== "")
	}

	function normalizeTypesPayload(rawData) {
		let list = []

		if (Array.isArray(rawData)) list = rawData
		else if (Array.isArray(rawData?.items)) list = rawData.items
		else if (Array.isArray(rawData?.data)) list = rawData.data
		else if (Array.isArray(rawData?.result)) list = rawData.result
		else if (Array.isArray(rawData?.requestTypes)) list = rawData.requestTypes
		else if (Array.isArray(rawData?.types)) list = rawData.types
		else if (Array.isArray(rawData?.value)) list = rawData.value

		return list
			.map(t => ({
				id: t.id ?? t.requestTypeId ?? t.Id ?? t.RequestTypeId ?? "",
				name: t.name ?? t.nombre ?? t.Name ?? t.Nombre ?? "",
				areaId: t.areaId ?? t.AreaId ?? t.idArea ?? t.area?.id ?? ""
			}))
			.filter(t => t.id !== "" && t.name !== "")
	}

	async function apiRequest(url, options = {}) {
		const response = await fetch(url, {
			...options,
			headers: {
				Accept: "application/json",
				...(options.headers || {})
			}
		})

		const contentType = response.headers.get("content-type") || ""
		let payload = null

		if (contentType.includes("application/json")) {
			payload = await response.json()
		} else {
			const text = await response.text()
			payload = text ? { message: text } : null
		}

		if (!response.ok) {
			const message =
				payload?.message ||
				payload?.title ||
				(typeof payload === "string" ? payload : "") ||
				`HTTP ${response.status}`
			throw new Error(message)
		}

		return payload
	}

	async function loadRequests() {
		isLoading.value = true
		loadError.value = ""

		try {
			const data = await apiRequest(`${API_BASE}/api/ServiceRequests`)
			const list = Array.isArray(data) ? data : data?.items || data?.data || data?.result || []
			requests.value = list.map(normalizeRequest)
		} catch (error) {
			requests.value = []
			loadError.value = error?.message || "No se pudieron cargar las solicitudes."
		} finally {
			isLoading.value = false
		}
	}

	async function loadAreas() {
		isLoadingAreas.value = true
		try {
			const raw = await apiRequest(`${API_BASE}/api/Catalogs/areas`)
			areas.value = normalizeAreasPayload(raw)
		} catch {
			areas.value = []
		} finally {
			isLoadingAreas.value = false
		}
	}

	async function loadAllRequestTypes() {
		isLoadingTypes.value = true
		try {
			const raw = await apiRequest(`${API_BASE}/api/Catalogs/request-types`)
			allRequestTypes.value = normalizeTypesPayload(raw)
		} catch {
			allRequestTypes.value = []
		} finally {
			isLoadingTypes.value = false
		}
	}

	async function loadTypesByArea(areaId) {
		if (!areaId) {
			requestTypes.value = []
			form.value.typeId = ""
			return
		}

		if (!allRequestTypes.value.length) {
			await loadAllRequestTypes()
		}

		requestTypes.value = allRequestTypes.value.filter(t => String(t.areaId) === String(areaId))
	}

	function parseDateValue(value) {
		if (!value) return null
		const date = value instanceof Date ? value : new Date(value)
		if (Number.isNaN(date.getTime())) return null
		return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0)
	}

	function buildEndOfDay(date) {
		return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999)
	}

	function resolveDateRange(datePreset, dateFrom, dateTo) {
		const now = new Date()
		const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0, 0)
		const todayEnd = buildEndOfDay(todayStart)

		if (dateFrom || dateTo) {
			const start = dateFrom ? parseDateValue(dateFrom) : new Date(2000, 0, 1)
			const endBase = dateTo ? parseDateValue(dateTo) : todayStart
			const end = endBase ? buildEndOfDay(endBase) : null
			if (!start || !end) return null
			return { start, end }
		}

		switch (datePreset) {
			case "today":
				return { start: todayStart, end: todayEnd }
			case "7d": {
				const start = new Date(todayStart)
				start.setDate(start.getDate() - 6)
				return { start, end: todayEnd }
			}
			case "30d": {
				const start = new Date(todayStart)
				start.setDate(start.getDate() - 29)
				return { start, end: todayEnd }
			}
			case "month": {
				const start = new Date(now.getFullYear(), now.getMonth(), 1)
				return { start, end: todayEnd }
			}
			default:
				return null
		}
	}

	function formatDate(value) {
		const date = new Date(value)
		if (Number.isNaN(date.getTime())) return "Sin fecha"
		return new Intl.DateTimeFormat("es-DO", {
			day: "2-digit",
			month: "2-digit",
			year: "numeric"
		}).format(date)
	}

	function toggleDropdown(name) {
		activeDropdown.value = activeDropdown.value === name ? null : name
	}

	function closeDropdowns() {
		activeDropdown.value = null
	}

	function selectStatus(value) {
		filters.value.status = value
		closeDropdowns()
	}

	function selectAssignment(value) {
		filters.value.assignment = value
		closeDropdowns()
	}

	function selectPriority(value) {
		filters.value.priority = value
		closeDropdowns()
	}

	function selectDatePreset(value) {
		filters.value.datePreset = value
		if (value !== "custom") {
			filters.value.dateFrom = ""
			filters.value.dateTo = ""
			closeDropdowns()
		}
	}

	function onGlobalClick(event) {
		const target = event.target
		if (!(target instanceof HTMLElement)) return
		if (!target.closest(".filterDropdown")) {
			closeDropdowns()
		}
	}

	function openDetail(item) {
		router.push({
			name: "RequestDetail",
			params: { id: item.id }
		})
	}

	async function openEdit(item) {
		resetForm()
		editingRequestId.value = item.id

		form.value.areaId = item.areaId ? String(item.areaId) : ""
		await loadTypesByArea(form.value.areaId)
		form.value.typeId = item.typeId ? String(item.typeId) : ""
		form.value.priority = item.priority || ""
		form.value.status = item.status || "Nueva"
		form.value.rejectionReason = item.rejectionReason || ""
		form.value.subject = item.subject || ""
		form.value.description = item.description || ""

		isEditOpen.value = true
	}

	function closeEdit() {
		isEditOpen.value = false
		resetForm()
	}

	function validateForm() {
		if (!form.value.areaId) return "Debes seleccionar un área."
		if (!form.value.typeId) return "Debes seleccionar un tipo de solicitud."
		if (!form.value.priority) return "Debes seleccionar una prioridad."
		if (!form.value.status) return "Debes seleccionar un estado."
		if (!form.value.subject.trim()) return "Debes ingresar el asunto."
		if (!form.value.description.trim()) return "Debes ingresar la descripción."
		if (canonicalStatusLabel(form.value.status) === "Rechazada" && !String(form.value.rejectionReason || "").trim()) {
			return "Debes indicar el motivo del rechazo."
		}
		return ""
	}

	function buildUpdatePayload() {
		const priorityId = getPriorityId(form.value.priority)
		const statusId = getStatusId(form.value.status)

		return {
			areaId: Number(form.value.areaId),
			requestTypeId: Number(form.value.typeId),
			priorityId,
			statusId,
			rejectionReason: canonicalStatusLabel(form.value.status) === "Rechazada" ? String(form.value.rejectionReason || "").trim() : null,
			subject: form.value.subject.trim(),
			description: form.value.description.trim()
		}
	}

	async function submitEdit() {
		submitError.value = ""

		const validationError = validateForm()
		if (validationError) {
			submitError.value = validationError
			return
		}

		isSubmitting.value = true

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${editingRequestId.value}`, {
				method: "PUT",
				headers: {
					"Content-Type": "application/json"
				},
				body: JSON.stringify(buildUpdatePayload())
			})

			closeEdit()
			await loadRequests()
		} catch (error) {
			submitError.value = error?.message || "No se pudo actualizar la solicitud."
		} finally {
			isSubmitting.value = false
		}
	}

	async function takeRequest(item) {
		takingId.value = item.id
		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${item.id}/take`, {
				method: "POST"
			})
			await loadRequests()
		} catch (error) {
			window.alert(error?.message || "No se pudo tomar la solicitud.")
		} finally {
			takingId.value = null
		}
	}

	async function removeRequest(item) {
		const confirmed = window.confirm(`¿Seguro que deseas eliminar la solicitud ${item.requestNumber}?`)
		if (!confirmed) return

		deletingId.value = item.id

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${item.id}`, {
				method: "DELETE"
			})
			await loadRequests()
		} catch (error) {
			window.alert(error?.message || "No se pudo eliminar la solicitud.")
		} finally {
			deletingId.value = null
		}
	}

	watch(
		() => form.value.areaId,
		async (newAreaId, oldAreaId) => {
			if (String(newAreaId) !== String(oldAreaId)) {
				form.value.typeId = ""
			}
			await loadTypesByArea(newAreaId)
		}
	)

	watch(
		() => form.value.status,
		value => {
			form.value.status = canonicalStatusLabel(value)
			if (canonicalStatusLabel(value) !== "Rechazada") {
				form.value.rejectionReason = ""
			}
		}
	)

	watch(
		() => [filters.value.dateFrom, filters.value.dateTo],
		([from, to]) => {
			if (from || to) {
				filters.value.datePreset = "custom"
			} else if (filters.value.datePreset === "custom") {
				filters.value.datePreset = "7d"
			}
		}
	)

	onMounted(async () => {
		document.addEventListener("click", onGlobalClick)
		await Promise.all([loadAreas(), loadAllRequestTypes(), loadRequests()])
	})
</script>

<template>
	<div class="page">
		<section class="shell">
			<header class="header">
				<button class="back" type="button" aria-label="Volver" @click="onBack">
					<svg viewBox="0 0 24 24" aria-hidden="true">
						<path :d="iconPath('arrowLeft')" />
					</svg>
				</button>

				<div class="titles">
					<h1>Bandeja del Área</h1>
					<p>Gestiona y da seguimiento a las solicitudes del área asignada</p>
				</div>

				<div class="headRight">
					<div class="userCard" aria-label="Usuario y área">
						<div class="userAvatar">{{ userCard.initial }}</div>
						<div class="userTxt">
							<div class="userName">{{ userCard.name }}</div>
							<div class="userMeta">
								<span class="pillTag">{{ userCard.role }}</span>
								<span class="sep"></span>
								<span class="areaName">{{ userCard.area }}</span>
							</div>
						</div>
					</div>
				</div>
			</header>

			<div class="cards">
				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--amber" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('spark')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.newCount }}</div>
							<div class="card__label">Nuevas</div>
						</div>
					</div>
					<div class="card__delta"></div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('clock')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.progressCount }}</div>
							<div class="card__label">En Proceso</div>
						</div>
					</div>
					<div class="card__delta"></div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--teal" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('done')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.resolvedCount }}</div>
							<div class="card__label">Resueltas</div>
						</div>
					</div>
					<div class="card__delta"></div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--rose" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('ban')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">{{ summary.rejectedCount }}</div>
							<div class="card__label">Rechazadas</div>
						</div>
					</div>
					<div class="card__delta"></div>
				</div>
			</div>

			<div class="filters">
				<div class="pill pill--search">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
					</span>
					<input v-model.trim="search"
						   class="pill__input"
						   type="text"
						   placeholder="Buscar por número o asunto..." />
				</div>

				<div class="filterDropdown">
					<button class="pill" type="button" @click.stop="toggleDropdown('status')">
						<span class="pill__txt">{{ statusFilterLabel }}</span>
						<span class="pill__chev" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
						</span>
					</button>

					<div v-if="activeDropdown === 'status'" class="dropdownPanel" @click.stop>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'active' }" type="button" @click="selectStatus('active')">Nuevas y En Proceso</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'all' }" type="button" @click="selectStatus('all')">Todos los estados</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'new' }" type="button" @click="selectStatus('new')">Nuevas</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'progress' }" type="button" @click="selectStatus('progress')">En Proceso</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'resolved' }" type="button" @click="selectStatus('resolved')">Resueltas</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'rejected' }" type="button" @click="selectStatus('rejected')">Rechazadas</button>
						<button class="optionItem" :class="{ 'optionItem--active': filters.status === 'closed' }" type="button" @click="selectStatus('closed')">Cerradas</button>
					</div>
				</div>

				<div class="filterDropdown">
					<button class="pill" type="button" @click.stop="toggleDropdown('assignment')">
						<span class="pill__txt">{{ assignmentFilterLabel }}</span>
						<span class="pill__chev" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
						</span>
					</button>

					<div v-if="activeDropdown === 'assignment'" class="dropdownPanel" @click.stop>
						<button v-for="option in assignmentOptions"
								:key="option.value || 'all'"
								class="optionItem"
								:class="{ 'optionItem--active': filters.assignment === option.value }"
								type="button"
								@click="selectAssignment(option.value)">
							{{ option.label }}
						</button>
					</div>
				</div>

				<div class="filterDropdown">
					<button class="pill" type="button" @click.stop="toggleDropdown('priority')">
						<span class="pill__txt">{{ priorityFilterLabel }}</span>
						<span class="pill__chev" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
						</span>
					</button>

					<div v-if="activeDropdown === 'priority'" class="dropdownPanel" @click.stop>
						<button class="optionItem" :class="{ 'optionItem--active': !filters.priority }" type="button" @click="selectPriority('')">Todas las prioridades</button>
						<button v-for="priority in priorityCatalog"
								:key="priority.id"
								class="optionItem"
								:class="{ 'optionItem--active': filters.priority === priority.label }"
								type="button"
								@click="selectPriority(priority.label)">
							{{ priority.label }}
						</button>
					</div>
				</div>

				<div class="filterDropdown">
					<button class="pill" type="button" @click.stop="toggleDropdown('date')">
						<span class="pill__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
						</span>
						<span class="pill__txt">{{ dateFilterLabel }}</span>
						<span class="pill__chev" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
						</span>
					</button>

					<div v-if="activeDropdown === 'date'" class="dropdownPanel dropdownPanel--date" @click.stop>
						<button v-for="option in dateOptions"
								:key="option.value"
								class="optionItem"
								:class="{ 'optionItem--active': filters.datePreset === option.value && !filters.dateFrom && !filters.dateTo }"
								type="button"
								@click="selectDatePreset(option.value)">
							{{ option.label }}
						</button>

						<div v-if="filters.datePreset === 'custom'" class="dateRange">
							<input v-model="filters.dateFrom" class="dateInput" type="date" />
							<input v-model="filters.dateTo" class="dateInput" type="date" />
						</div>
					</div>
				</div>
			</div>

			<div v-if="loadError" class="statebox statebox--error">
				{{ loadError }}
			</div>

			<div class="tablewrap" role="region" aria-label="Tabla de solicitudes del área">
				<table class="table" v-if="hasRequests">
					<thead>
						<tr>
							<th>N°</th>
							<th>NÚMERO SOLICITUD</th>
							<th>ASUNTO</th>
							<th>ÁREA</th>
							<th>TIPO</th>
							<th>PRIORIDAD</th>
							<th>ESTADO</th>
							<th>FECHA</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="(item, index) in filteredRequests" :key="item.id">
							<td>{{ index + 1 }}</td>
							<td class="requestNumberCell">{{ item.requestNumber }}</td>
							<td class="subjectCell">{{ item.subject }}</td>
							<td>{{ item.areaName || "N/D" }}</td>
							<td>{{ item.typeName || "N/D" }}</td>
							<td>
								<span class="priorityBadge" :class="`priorityBadge--${String(item.priority || '').toLowerCase()}`">
									{{ item.priority || "N/D" }}
								</span>
							</td>
							<td>
								<span class="statusBadge" :class="`statusBadge--${item.statusKey}`">
									{{ item.status }}
								</span>
							</td>
							<td>{{ formatDate(item.createdAtUtc) }}</td>
							<td class="td-actions">
								<div class="actionsRow">
									<button class="actionBtn actionBtn--view" type="button" @click="openDetail(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('eye')" /></svg>
										Ver
									</button>

									<button class="actionBtn actionBtn--take" type="button" :disabled="takingId === item.id" @click="takeRequest(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('take')" /></svg>
										{{ takingId === item.id ? "Tomando..." : "Tomar" }}
									</button>

									<button class="actionBtn actionBtn--edit" type="button" @click="openEdit(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('edit')" /></svg>
										Editar
									</button>

									<button class="actionBtn actionBtn--delete" type="button" :disabled="deletingId === item.id" @click="removeRequest(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
										{{ deletingId === item.id ? "Eliminando..." : "Eliminar" }}
									</button>
								</div>
							</td>
						</tr>
					</tbody>
				</table>

				<div v-else class="empty">
					<div class="empty__circle" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('user')" /></svg>
					</div>
					<div class="empty__title">
						{{ isLoading ? "Cargando solicitudes..." : "No hay solicitudes asignadas" }}
					</div>
					<div class="empty__sub">
						Cuando existan solicitudes reales podrás verlas aquí
						<br />
						y gestionarlas por estado, prioridad y fecha.
					</div>
				</div>
			</div>

			<footer class="foot">
				<div>© 2026 RequestHub • Mesa de Servicios Internos</div>
				<div class="foot__sub">SOL-YYYY-0001 • JWT Auth • v1.0</div>
			</footer>
		</section>

		<button class="help" type="button" aria-label="Ayuda">?</button>

		<div v-if="isEditOpen" class="overlay" role="dialog" aria-modal="true" aria-label="Editar solicitud">
			<div class="modal">
				<div class="modalHeader">
					<h2>Editar Solicitud</h2>
					<button class="modalClose" type="button" @click="closeEdit">×</button>
				</div>

				<div v-if="submitError" class="msg msg--error">{{ submitError }}</div>

				<div class="formGrid">
					<div class="field">
						<label>Área *</label>
						<select v-model="form.areaId" class="selectNative">
							<option value="" disabled>{{ isLoadingAreas ? "Cargando áreas..." : "Selecciona un área" }}</option>
							<option v-for="area in areas" :key="area.id" :value="String(area.id)">
								{{ area.name }}
							</option>
						</select>
					</div>

					<div class="field">
						<label>Tipo de Solicitud *</label>
						<select v-model="form.typeId" class="selectNative" :disabled="!form.areaId || isLoadingTypes">
							<option value="" disabled>{{ isLoadingTypes ? "Cargando tipos..." : "Selecciona un tipo" }}</option>
							<option v-for="type in requestTypes" :key="type.id" :value="String(type.id)">
								{{ type.name }}
							</option>
						</select>
					</div>

					<div class="field">
						<label>Prioridad *</label>
						<select v-model="form.priority" class="selectNative">
							<option value="" disabled>Selecciona prioridad</option>
							<option v-for="priority in priorityCatalog" :key="priority.id" :value="priority.label">
								{{ priority.label }}
							</option>
						</select>
					</div>

					<div class="field">
						<label>Estado *</label>
						<select v-model="form.status" class="selectNative">
							<option v-for="status in statusCatalog" :key="status.id" :value="status.label">
								{{ status.label }}
							</option>
						</select>
					</div>

					<div class="field field--full">
						<label>Asunto *</label>
						<input v-model.trim="form.subject" type="text" placeholder="Asunto de la solicitud" />
					</div>

					<div class="field field--full">
						<label>Descripción *</label>
						<textarea v-model.trim="form.description" rows="5" placeholder="Descripción detallada"></textarea>
					</div>

					<div v-if="form.status === 'Rechazada'" class="field field--full">
						<label>Motivo del rechazo *</label>
						<textarea v-model.trim="form.rejectionReason" rows="3" placeholder="Indica el motivo"></textarea>
					</div>
				</div>

				<div class="modalActions">
					<button class="btnGhost" type="button" @click="closeEdit">Cancelar</button>
					<button class="btnPrimary" type="button" :disabled="isSubmitting" @click="submitEdit">
						{{ isSubmitting ? "Guardando..." : "Guardar cambios" }}
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>
	.page {
		min-height: 100vh;
		padding: 22px 18px 90px;
		background: radial-gradient(980px 560px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255,255,255,0) 62%), radial-gradient(920px 560px at 85% 85%, rgba(179, 94, 255, 0.20), rgba(255,255,255,0) 62%), linear-gradient(180deg, #f7f7ff 0%, #ece9ff 55%, #e9e7ff 100%);
		position: relative;
		overflow: hidden;
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, "Apple Color Emoji","Segoe UI Emoji";
	}

		.page::before {
			content: "";
			position: absolute;
			inset: 0;
			background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1400' height='800' viewBox='0 0 1400 800'%3E%3Cg fill='none' stroke='%23ffffff' stroke-opacity='.24'%3E%3Cpath d='M0 600 C 260 540 420 700 660 650 C 900 600 1040 500 1400 560' stroke-width='20'/%3E%3Cpath d='M0 680 C 260 620 420 780 660 720 C 900 680 1040 580 1400 640' stroke-width='14'/%3E%3Cpath d='M0 740 C 260 700 420 820 660 780 C 900 740 1040 660 1400 720' stroke-width='10'/%3E%3C/g%3E%3C/svg%3E");
			background-repeat: no-repeat;
			background-position: center bottom;
			background-size: cover;
			pointer-events: none;
			opacity: .9;
		}

	.shell {
		max-width: 1180px;
		margin: 0 auto;
		border-radius: 22px;
		background: rgba(255,255,255,.55);
		border: 1px solid rgba(110,102,182,.10);
		box-shadow: 0 28px 70px rgba(40,55,95,.12);
		backdrop-filter: blur(10px);
		padding: 18px 18px 16px;
	}

	.header {
		display: grid;
		grid-template-columns: 44px 1fr auto;
		gap: 14px;
		align-items: center;
	}

	.back {
		width: 44px;
		height: 44px;
		border-radius: 16px;
		border: 1px solid rgba(110,102,182,.12);
		background: rgba(255,255,255,.65);
		box-shadow: 0 16px 30px rgba(40,55,95,.08);
		cursor: pointer;
		display: grid;
		place-items: center;
		color: rgba(90,82,160,.9);
	}

		.back svg {
			width: 20px;
			height: 20px;
		}

		.back path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.titles h1 {
		margin: 0;
		font-size: 30px;
		font-weight: 900;
		color: #232a52;
		letter-spacing: -.4px;
	}

	.titles p {
		margin: 4px 0 0;
		font-size: 13px;
		color: rgba(39,46,86,.62);
	}

	.headRight {
		display: flex;
		align-items: center;
		gap: 14px;
	}

	.userCard {
		height: 56px;
		border-radius: 18px;
		border: 1px solid rgba(110,102,182,.12);
		background: rgba(255,255,255,.60);
		box-shadow: 0 16px 30px rgba(40,55,95,.08);
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 0 14px;
		min-width: 260px;
	}

	.userAvatar {
		width: 38px;
		height: 38px;
		border-radius: 16px;
		background: linear-gradient(135deg, #7a67ff, #a053ff);
		color: #fff;
		font-weight: 900;
		display: grid;
		place-items: center;
	}

	.userName {
		font-weight: 900;
		color: #232a52;
		font-size: 14px;
	}

	.userMeta {
		display: flex;
		align-items: center;
		gap: 8px;
		margin-top: 4px;
		font-size: 12px;
		color: rgba(39,46,86,.55);
	}

	.pillTag {
		height: 22px;
		padding: 0 10px;
		border-radius: 999px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		color: rgba(88, 78, 212, 0.95);
		background: rgba(120,105,235,.12);
	}

	.sep {
		display: inline-block;
		width: 1px;
		height: 14px;
		background: rgba(110,102,182,.14);
	}

	.cards {
		margin-top: 14px;
		display: grid;
		grid-template-columns: repeat(4, minmax(0,1fr));
		gap: 12px;
	}

	.card {
		border-radius: 16px;
		background: rgba(255,255,255,.58);
		border: 1px solid rgba(110,102,182,.10);
		box-shadow: 0 18px 34px rgba(40,55,95,.10);
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
		background: rgba(120,105,235,.16);
		display: grid;
		place-items: center;
		color: #6b5cff;
	}

	.card__icon--amber {
		color: #c8801b;
		background: rgba(255,197,90,.22);
	}

	.card__icon--teal {
		color: #2c8a6a;
		background: rgba(60,196,151,.14);
	}

	.card__icon--rose {
		color: #c04a62;
		background: rgba(255,106,162,.18);
	}

	.card__icon svg {
		width: 18px;
		height: 18px;
	}

	.card__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
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
		color: rgba(39,46,86,.62);
	}

	.card__delta {
		min-width: 54px;
	}

	.filters {
		margin-top: 12px;
		display: grid;
		grid-template-columns: 1.25fr .82fr .72fr .82fr .82fr;
		column-gap: 20px;
		row-gap: 28px;
	}

		.filters > :nth-child(2) {
			margin-left: 20px;
		}


	.filterDropdown {
		position: relative;
	}

	.pill {
		height: 44px;
		border-radius: 14px;
		background: rgba(255,255,255,.62);
		border: 1px solid rgba(110,102,182,.10);
		box-shadow: 0 16px 30px rgba(40,55,95,.08);
		padding: 0 12px;
		display: flex;
		align-items: center;
		gap: 10px;
		cursor: pointer;
		color: rgba(39,46,86,.82);
		width: 100%;
	}

	.pill--search {
		cursor: default;
	}

	.pill__icon {
		width: 30px;
		height: 30px;
		border-radius: 12px;
		background: rgba(120,105,235,.14);
		display: grid;
		place-items: center;
		color: rgba(120,105,235,.90);
		flex: 0 0 auto;
	}

		.pill__icon svg {
			width: 16px;
			height: 16px;
		}

		.pill__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.pill__input {
		border: none;
		outline: none;
		background: transparent;
		flex: 1 1 auto;
		min-width: 0;
		font-size: 13px;
		color: rgba(39,46,86,.86);
	}

		.pill__input::placeholder {
			color: rgba(39,46,86,.40);
		}

	.pill__txt {
		font-size: 13px;
		font-weight: 900;
		color: rgba(39,46,86,.78);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.pill__chev {
		margin-left: auto;
		color: rgba(39,46,86,.38);
		display: grid;
		place-items: center;
	}

		.pill__chev svg {
			width: 16px;
			height: 16px;
		}

		.pill__chev path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.dropdownPanel {
		position: absolute;
		top: calc(100% + 10px);
		left: 0;
		width: 100%;
		min-width: 220px;
		border-radius: 16px;
		background: rgba(255,255,255,.96);
		border: 1px solid rgba(110,102,182,.12);
		box-shadow: 0 26px 56px rgba(40,55,95,.16);
		padding: 12px;
		z-index: 20;
		display: grid;
		gap: 8px;
	}

	.dropdownPanel--date {
		min-width: 260px;
	}

	.optionItem {
		height: 38px;
		border-radius: 12px;
		border: 1px solid rgba(110,102,182,.10);
		background: rgba(255,255,255,.76);
		font-size: 12px;
		font-weight: 800;
		color: rgba(39,46,86,.82);
		cursor: pointer;
		text-align: left;
		padding: 0 12px;
	}

	.optionItem--active {
		border-color: rgba(120,105,235,.45);
		color: rgba(88,78,212,.98);
		background: rgba(150,122,255,.10);
		box-shadow: 0 0 0 4px rgba(190,182,255,.20);
	}

	.dateRange {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 10px;
		margin-top: 4px;
	}

	.dateInput {
		height: 40px;
		border-radius: 12px;
		border: 1px solid rgba(110,102,182,.14);
		background: rgba(255,255,255,.76);
		padding: 0 10px;
		font-size: 12px;
		font-weight: 800;
		color: rgba(39,46,86,.82);
		outline: none;
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
		margin-top: 14px;
		border-radius: 18px;
		background: rgba(255,255,255,.58);
		border: 1px solid rgba(110,102,182,.10);
		box-shadow: 0 22px 44px rgba(40,55,95,.12);
		overflow: auto;
		min-height: 420px;
	}

	.table {
		width: 100%;
		border-collapse: separate;
		border-spacing: 0;
		min-width: 1180px;
	}

	thead th {
		padding: 14px;
		font-size: 11px;
		font-weight: 900;
		color: rgba(39,46,86,.62);
		text-align: left;
		background: linear-gradient(180deg, rgba(240,236,255,.70), rgba(255,255,255,.40));
		border-bottom: 1px solid rgba(110,102,182,.10);
	}

	tbody td {
		padding: 14px;
		font-size: 13px;
		color: rgba(39,46,86,.82);
		border-bottom: 1px solid rgba(110,102,182,.08);
		background: rgba(255,255,255,.36);
		vertical-align: middle;
	}

	tbody tr:last-child td {
		border-bottom: none;
	}

	.requestNumberCell {
		font-weight: 900;
		color: #232a52;
	}

	.subjectCell {
		max-width: 260px;
		word-break: break-word;
	}

	.priorityBadge {
		height: 28px;
		padding: 0 12px;
		border-radius: 999px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 11px;
		font-weight: 900;
		border: 1px solid rgba(110,102,182,.10);
		white-space: nowrap;
	}

	.priorityBadge--baja {
		background: rgba(60, 196, 151, 0.12);
		color: rgba(35, 130, 100, 0.98);
	}

	.priorityBadge--media {
		background: rgba(255, 197, 90, 0.16);
		color: rgba(180, 120, 30, 0.98);
	}

	.priorityBadge--alta {
		background: rgba(255, 90, 130, 0.12);
		color: rgba(170, 40, 80, 0.98);
	}

	.statusBadge {
		min-height: 30px;
		padding: 0 14px;
		border-radius: 999px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 11px;
		font-weight: 900;
		white-space: nowrap;
	}

	.statusBadge--new {
		background: rgba(150, 122, 255, 0.14);
		color: rgba(96, 73, 219, 0.98);
	}

	.statusBadge--progress {
		background: rgba(255, 197, 90, 0.18);
		color: rgba(180, 120, 30, 0.98);
	}

	.statusBadge--done {
		background: rgba(60, 196, 151, 0.14);
		color: rgba(35, 130, 100, 0.98);
	}

	.statusBadge--closed {
		background: rgba(184, 194, 218, 0.20);
		color: rgba(72, 89, 125, 0.98);
	}

	.statusBadge--rejected {
		background: rgba(255, 90, 130, 0.14);
		color: rgba(170, 40, 80, 0.98);
	}

	.statusBadge--neutral {
		background: rgba(94, 106, 210, 0.12);
		color: #4c5699;
	}

	.th-actions,
	.td-actions {
		text-align: right;
	}

	.actionsRow {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		gap: 10px;
		flex-wrap: wrap;
	}

	.actionBtn {
		height: 34px;
		padding: 0 12px;
		border-radius: 12px;
		border: 1px solid rgba(110,102,182,.14);
		background: rgba(255,255,255,.78);
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 12px 24px rgba(40,55,95,.06);
	}

		.actionBtn svg {
			width: 14px;
			height: 14px;
		}

		.actionBtn path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

		.actionBtn:disabled {
			opacity: .6;
			cursor: not-allowed;
		}

	.actionBtn--view {
		color: #5b49c6;
	}

	.actionBtn--take {
		color: #2c8a6a;
	}

	.actionBtn--edit {
		color: #6b5cff;
	}

	.actionBtn--delete {
		color: #c04a62;
	}

	.empty {
		height: 420px;
		display: grid;
		place-items: center;
		text-align: center;
		padding: 40px 18px;
		color: rgba(39,46,86,.72);
	}

	.empty__circle {
		width: 74px;
		height: 74px;
		border-radius: 999px;
		background: rgba(120,105,235,.14);
		color: rgba(120,105,235,.90);
		display: grid;
		place-items: center;
		box-shadow: 0 18px 34px rgba(88,78,212,.12);
	}

		.empty__circle svg {
			width: 28px;
			height: 28px;
		}

		.empty__circle path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.empty__title {
		margin-top: 18px;
		font-size: 22px;
		font-weight: 900;
		color: #232a52;
	}

	.empty__sub {
		margin-top: 8px;
		font-size: 13px;
		color: rgba(39,46,86,.55);
		line-height: 1.45;
	}

	.foot {
		margin-top: 18px;
		text-align: center;
		font-weight: 900;
		color: rgba(39,46,86,.70);
	}

	.foot__sub {
		margin-top: 6px;
		font-size: 12px;
		font-weight: 700;
		color: rgba(39,46,86,.45);
	}

	.help {
		position: fixed;
		right: 18px;
		bottom: 18px;
		width: 44px;
		height: 44px;
		border-radius: 16px;
		border: 1px solid rgba(110,102,182,.16);
		background: rgba(85,76,210,.70);
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 18px 34px rgba(40,55,95,.20);
	}

	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(20, 18, 40, 0.16);
		backdrop-filter: blur(10px);
		display: grid;
		place-items: center;
		padding: 16px;
		z-index: 50;
	}

	.modal {
		width: 100%;
		max-width: 820px;
		border-radius: 22px;
		background: linear-gradient(180deg, rgba(255,255,255,.90), rgba(245,242,255,.90));
		border: 1px solid rgba(110,102,182,.12);
		box-shadow: 0 30px 80px rgba(40,55,95,.22);
		padding: 18px;
		display: grid;
		gap: 14px;
	}

	.modalHeader {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
	}

		.modalHeader h2 {
			margin: 0;
			font-size: 18px;
			font-weight: 900;
			color: #232a52;
		}

	.modalClose {
		width: 36px;
		height: 36px;
		border-radius: 12px;
		border: 1px solid rgba(110,102,182,.12);
		background: rgba(255,255,255,.75);
		font-size: 22px;
		line-height: 1;
		cursor: pointer;
		color: rgba(90,82,160,.9);
	}

	.formGrid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 12px;
	}

	.field {
		display: grid;
		gap: 6px;
	}

	.field--full {
		grid-column: 1 / -1;
	}

	.field label {
		font-size: 11px;
		font-weight: 900;
		color: rgba(39,46,86,.78);
	}

	.field input,
	.field textarea,
	.selectNative {
		box-sizing: border-box;
		display: block;
		width: 100%;
		border-radius: 14px;
		border: 1px solid rgba(110,102,182,.10);
		background: rgba(255,255,255,.76);
		outline: none;
		font-size: 13px;
		color: rgba(39,46,86,.86);
	}

	.field input,
	.selectNative {
		height: 42px;
		padding: 0 12px;
	}

	.field textarea {
		padding: 12px;
		resize: vertical;
		min-height: 96px;
	}

	.selectNative {
		padding-right: 36px;
		appearance: none;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24'%3E%3Cpath d='M7 10l5 5 5-5' fill='none' stroke='%23706f8f' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
		background-repeat: no-repeat;
		background-position: right 12px center;
		background-size: 16px;
	}

	.modalActions {
		display: flex;
		justify-content: flex-end;
		gap: 12px;
	}

	.btnGhost,
	.btnPrimary {
		height: 42px;
		padding: 0 18px;
		border-radius: 14px;
		font-weight: 900;
		cursor: pointer;
	}

	.btnGhost {
		border: 1px solid rgba(110,102,182,.12);
		background: rgba(255,255,255,.66);
		color: rgba(39,46,86,.65);
	}

	.btnPrimary {
		border: 1px solid rgba(110,102,182,.10);
		background: linear-gradient(90deg, rgba(90,80,220,.9), rgba(160,86,255,.86));
		color: #fff;
		box-shadow: 0 18px 34px rgba(88,78,212,.18);
	}

		.btnPrimary:disabled {
			opacity: .7;
			cursor: not-allowed;
		}

	.msg {
		border-radius: 12px;
		padding: 10px 12px;
		font-size: 12px;
		font-weight: 700;
	}

	.msg--error {
		background: rgba(255, 90, 130, 0.12);
		color: rgba(170, 40, 80, 0.95);
		border: 1px solid rgba(255, 90, 130, 0.18);
	}

	@media (max-width: 1100px) {
		.cards {
			grid-template-columns: repeat(2, minmax(0,1fr));
		}

		.filters {
			grid-template-columns: 1fr 1fr;
		}

		.header {
			grid-template-columns: 44px 1fr;
		}

		.headRight {
			grid-column: 1 / -1;
			justify-content: flex-end;
		}
	}

	@media (max-width: 760px) {
		.filters {
			grid-template-columns: 1fr;
		}

		.formGrid {
			grid-template-columns: 1fr;
		}

		.dateRange {
			grid-template-columns: 1fr;
		}
	}

	@media (max-width: 640px) {
		.userCard {
			display: none;
		}
	}
</style>