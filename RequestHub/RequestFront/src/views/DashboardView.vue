<script setup>
	import { computed, onMounted, ref, watch } from "vue"
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

	const isEditOpen = ref(false)
	const isSubmitting = ref(false)
	const isLoadingAreas = ref(false)
	const isLoadingTypes = ref(false)
	const submitError = ref("")
	const editingRequestId = ref(null)
	const attachmentInputRef = ref(null)

	const areas = ref([])
	const requestTypes = ref([])

	const filters = ref({
		search: "",
		status: "",
		area: "",
		date: ""
	})

	const form = ref(createDefaultForm())

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")

	const priorityCatalog = Object.freeze([
		{ id: 1, label: "Baja" },
		{ id: 2, label: "Media" },
		{ id: 3, label: "Alta" }
	])

	const statusCatalog = Object.freeze([
		{ id: 1, label: "Nueva", tone: "new" },
		{ id: 2, label: "En Proceso", tone: "in-progress" },
		{ id: 3, label: "Resuelta", tone: "resolved" },
		{ id: 4, label: "Cerrada", tone: "closed" },
		{ id: 5, label: "Rechazada", tone: "rejected" }
	])

	const priorityIdToLabel = Object.freeze({
		1: "Baja",
		2: "Media",
		3: "Alta"
	})

	function createDefaultForm() {
		return {
			id: "",
			areaId: "",
			typeId: "",
			priority: "",
			status: "Nueva",
			rejectionReason: "",
			subject: "",
			description: "",
			attachment: null,
			attachmentName: "",
			attachmentUrl: "",
			removeAttachment: false
		}
	}

	function getToken() {
		return localStorage.getItem("rh_token") || localStorage.getItem("token") || ""
	}

	function getStoredUser() {
		try {
			return JSON.parse(localStorage.getItem("rh_user") || localStorage.getItem("user") || "null")
		} catch {
			return null
		}
	}

	function normalizeRole(role) {
		return String(role ?? "").trim().toLowerCase()
	}

	const currentRole = computed(() => normalizeRole(getStoredUser()?.role))
	const canManageStatus = computed(() => currentRole.value === "superadmin" || currentRole.value === "admin")
	const availableStatusOptions = computed(() => {
		if (!canManageStatus.value) return []
		return statusCatalog.filter(x => x.label !== "Cerrada")
	})

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
			case "arrowLeft":
				return "M15 18l-6-6 6-6"
			case "paperPlane":
				return "M21 3 3 11l7 2 2 7 9-17ZM10 13l11-10"
			case "upload":
				return "M12 16V4m0 0 4 4M12 4 8 8M4 20h16"
			case "file":
				return "M6 4.5h9l3 3v12H6zM15 4.5v3h3"
			case "open":
				return "M14 5.5h5v5M19 5.5l-8.5 8.5M18.5 13.5v5h-13v-13h5"
			case "download":
				return "M12 4v11M7.5 10.5 12 15l4.5-4.5M5 19.5h14"
			case "x":
				return "M6 6l12 12M18 6L6 18"
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

	function getStatusId(status) {
		const normalized = normalizeStatusName(status)
		const found = statusCatalog.find(x => x.label === normalized)
		return found?.id ?? 1
	}

	function normalizeRequest(request) {
		const rawDate = resolveRequestDate(request)

		const requestNumber =
			request?.requestNumber ||
			request?.number ||
			request?.raw?.requestNumber ||
			request?.raw?.number ||
			""

		const subject =
			request?.subject ||
			request?.raw?.subject ||
			request?.title ||
			request?.raw?.title ||
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
			subject,
			rawDate,
			createdAtLabel: request?.createdAtLabel || formatDateLabel(rawDate),
			dateFilterValue: formatDateInputValue(rawDate),
			requestNumber,
			areaName,
			priorityName,
			statusName,
			statusKey: request?.statusKey || buildStatusKey(statusName),
			canEdit: Boolean(request?.canEdit ?? request?.raw?.canEdit),
			canDelete: Boolean(request?.canDelete ?? request?.raw?.canDelete),
			areaId: request?.areaId ?? request?.raw?.areaId ?? "",
			typeId: request?.typeId ?? request?.requestTypeId ?? request?.raw?.typeId ?? request?.raw?.requestTypeId ?? "",
			typeName: request?.typeName ?? request?.requestType ?? request?.raw?.typeName ?? request?.raw?.requestType ?? "",
			priorityId: request?.priorityId ?? request?.raw?.priorityId ?? "",
			description: request?.description ?? request?.raw?.description ?? "",
			rejectionReason: request?.rejectionReason ?? request?.raw?.rejectionReason ?? "",
			attachmentName: request?.attachmentName ?? request?.raw?.attachmentName ?? "",
			attachmentPath: request?.attachmentPath ?? request?.raw?.attachmentPath ?? "",
			attachmentUrl: request?.attachmentUrl ?? request?.raw?.attachmentUrl ?? request?.attachmentPath ?? request?.raw?.attachmentPath ?? ""
		}
	}

	function normalizeArea(a) {
		return {
			id: a?.id ?? a?.areaId ?? a?.Id ?? a?.AreaId ?? a?.idArea ?? "",
			name: a?.name ?? a?.nombre ?? a?.Name ?? a?.Nombre ?? ""
		}
	}

	function normalizeType(t) {
		return {
			id: t?.id ?? t?.requestTypeId ?? t?.Id ?? t?.RequestTypeId ?? t?.idTipoSolicitud ?? "",
			name: t?.name ?? t?.nombre ?? t?.Name ?? t?.Nombre ?? "",
			areaId: t?.areaId ?? t?.AreaId ?? t?.idArea ?? t?.area?.id ?? t?.area?.areaId ?? ""
		}
	}

	async function apiRequest(path, options = {}) {
		const token = getToken()

		const headers = {
			Accept: "application/json",
			...(options.headers || {})
		}

		if (token) {
			headers.Authorization = `Bearer ${token}`
		}

		const response = await fetch(`${API_BASE}${path}`, {
			...options,
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

	async function loadAreas() {
		isLoadingAreas.value = true

		try {
			const data = await apiRequest("/api/Catalogs/areas")
			const list = Array.isArray(data) ? data : data?.items || data?.data || data?.result || data?.areas || []
			areas.value = list.map(normalizeArea).filter(x => x.id !== "" && x.name !== "")
		} catch {
			areas.value = []
		} finally {
			isLoadingAreas.value = false
		}
	}

	async function loadTypesByArea(areaId) {
		if (!areaId) {
			requestTypes.value = []
			form.value.typeId = ""
			return
		}

		isLoadingTypes.value = true

		try {
			const data = await apiRequest("/api/Catalogs/request-types")
			const list = Array.isArray(data) ? data : data?.items || data?.data || data?.result || data?.requestTypes || data?.types || []
			requestTypes.value = list
				.map(normalizeType)
				.filter(x => String(x.areaId) === String(areaId))
		} catch {
			requestTypes.value = []
		} finally {
			isLoadingTypes.value = false
		}
	}

	function getPriorityId(priority) {
		const normalized = String(priority || "").trim().toLowerCase()
		const found = priorityCatalog.find(x => x.label.toLowerCase() === normalized)
		return found?.id ?? 0
	}

	function resolvePriorityLabel(priorityValue, priorityId) {
		if (typeof priorityValue === "string" && priorityValue.trim()) return priorityValue.trim()
		if (priorityValue && typeof priorityValue === "object") {
			return priorityValue.name ?? priorityValue.nombre ?? priorityValue.label ?? ""
		}
		return priorityIdToLabel[Number(priorityId)] || ""
	}

	function resolveAreaIdByName(name) {
		if (!name) return ""
		const found = areas.value.find(x => String(x.name || "").trim().toLowerCase() === String(name).trim().toLowerCase())
		return found?.id ?? ""
	}

	function resolveTypeIdByName(name, areaId) {
		if (!name) return ""
		const normalizedName = String(name).trim().toLowerCase()
		const found = requestTypes.value.find(x => {
			const sameName = String(x.name || "").trim().toLowerCase() === normalizedName
			const sameArea = areaId ? String(x.areaId) === String(areaId) : true
			return sameName && sameArea
		})
		return found?.id ?? ""
	}

	function openRequest(request) {
		if (!request?.id) return

		router.push({
			name: "RequestDetail",
			params: { id: request.id }
		})
	}

	function resetAttachmentInput() {
		if (attachmentInputRef.value) {
			attachmentInputRef.value.value = ""
		}
	}

	function resetForm() {
		form.value = createDefaultForm()
		requestTypes.value = []
		submitError.value = ""
		editingRequestId.value = null
		resetAttachmentInput()
	}

	async function openEdit(request) {
		if (!request?.id || !request?.canEdit) return

		resetForm()
		editingRequestId.value = request.id

		const resolvedAreaId = request.areaId || resolveAreaIdByName(request.areaName)
		form.value.id = String(request.id ?? "")
		form.value.areaId = resolvedAreaId ? String(resolvedAreaId) : ""

		if (form.value.areaId) {
			await loadTypesByArea(form.value.areaId)
		}

		const resolvedTypeId = request.typeId || resolveTypeIdByName(request.typeName, form.value.areaId)

		form.value.typeId = resolvedTypeId ? String(resolvedTypeId) : ""
		form.value.priority = resolvePriorityLabel(request.priorityName || request.priority, request.priorityId)
		form.value.status = normalizeStatusName(request.statusName || request.status || "Nueva")
		form.value.rejectionReason = request.rejectionReason || ""
		form.value.subject = request.subject || ""
		form.value.description = request.description || ""
		form.value.attachment = null
		form.value.attachmentName = request.attachmentName || ""
		form.value.attachmentUrl = request.attachmentPath || request.attachmentUrl || ""
		form.value.removeAttachment = false

		isEditOpen.value = true
	}

	function closeEdit() {
		isEditOpen.value = false
		resetForm()
	}

	function applyPickedFile(file) {
		if (!file) return
		form.value.attachment = file
		form.value.attachmentName = file.name
		form.value.removeAttachment = false
	}

	function onPickFile(e) {
		const file = e?.target?.files?.[0] ?? null
		applyPickedFile(file)
	}

	function removeCurrentAttachment() {
		form.value.attachment = null
		form.value.attachmentName = ""
		form.value.attachmentUrl = ""
		form.value.removeAttachment = true
		resetAttachmentInput()
	}

	function buildUpdatePayload() {
		const priorityId = getPriorityId(form.value.priority)
		const statusId = canManageStatus.value ? getStatusId(form.value.status) : 1

		const formData = new FormData()
		formData.append("AreaId", String(Number(form.value.areaId)))
		formData.append("RequestTypeId", String(Number(form.value.typeId)))
		formData.append("PriorityId", String(priorityId))
		formData.append("StatusId", String(statusId))
		formData.append("Subject", form.value.subject.trim())
		formData.append("Description", form.value.description.trim())
		formData.append("RemoveAttachment", form.value.removeAttachment ? "true" : "false")
		formData.append("RejectionReason", form.value.status === "Rechazada" ? form.value.rejectionReason.trim() : "")

		if (form.value.attachment instanceof File) {
			formData.append("Attachment", form.value.attachment)
		}

		return formData
	}

	function validateForm() {
		if (!form.value.areaId) return "Debes seleccionar un área."
		if (!form.value.typeId) return "Debes seleccionar un tipo de solicitud."
		if (!form.value.priority) return "Debes seleccionar una prioridad."
		if (!form.value.subject.trim()) return "Debes ingresar el asunto."
		if (!form.value.description.trim()) return "Debes ingresar la descripción."
		if (form.value.status === "Rechazada" && !form.value.rejectionReason.trim()) return "Debes ingresar el motivo de rechazo."
		if (form.value.attachment && form.value.attachment.size > 10 * 1024 * 1024) return "El archivo supera el máximo de 10MB."
		return ""
	}

	async function submitEdit() {
		submitError.value = ""

		const validationError = validateForm()
		if (validationError) {
			submitError.value = validationError
			return
		}

		if (!editingRequestId.value) return

		isSubmitting.value = true

		try {
			await apiRequest(`/api/ServiceRequests/${editingRequestId.value}`, {
				method: "PUT",
				body: buildUpdatePayload()
			})

			closeEdit()
			await loadRequests()
		} catch (error) {
			submitError.value = error?.message || "No se pudo actualizar la solicitud."
		} finally {
			isSubmitting.value = false
		}
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

	const displayAttachmentName = computed(() => {
		if (form.value.attachment instanceof File) return form.value.attachment.name
		return String(form.value.attachmentName || "").trim()
	})

	const displayAttachmentUrl = computed(() => {
		const raw = String(form.value.attachmentUrl || "").trim()
		if (!raw) return ""
		if (/^https?:\/\//i.test(raw)) return raw
		return `${API_BASE}${raw.startsWith("/") ? "" : "/"}${raw}`
	})

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

	watch(
		() => form.value.areaId,
		async (newAreaId, oldAreaId) => {
			if (String(newAreaId) !== String(oldAreaId)) {
				form.value.typeId = ""
			}
			if (newAreaId) {
				await loadTypesByArea(newAreaId)
			}
		}
	)

	watch(
		() => form.value.status,
		value => {
			if (normalizeStatusName(value) !== "Rechazada") {
				form.value.rejectionReason = ""
			}
		}
	)

	onMounted(async () => {
		await loadAreas()
		await loadRequests()
	})
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
						<option v-for="option in statusOptions" :key="option.value" :value="option.value">
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
						<option v-for="option in areaOptions" :key="option.value" :value="option.value">
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
								<span class="status-badge" :class="`status-badge--${request.statusKey || 'unknown'}`">
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
											@click="openEdit(request)">
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

		<div v-if="isEditOpen" class="overlay" role="dialog" aria-modal="true" aria-label="Editar Solicitud">
			<div class="modal">
				<header class="modal__head">
					<div class="modal__left">
						<button class="modalBack" type="button" aria-label="Volver" @click="closeEdit">
							<svg viewBox="0 0 24 24" aria-hidden="true">
								<path :d="iconPath('arrowLeft')" />
							</svg>
						</button>
						<div>
							<h2>Editar Solicitud</h2>
							<p>Actualiza la información de la solicitud</p>
						</div>
					</div>

					<button class="modalClose" type="button" aria-label="Cerrar" @click="closeEdit">
						<svg viewBox="0 0 24 24"><path :d="iconPath('x')" /></svg>
					</button>
				</header>

				<div class="modal__body">
					<div v-if="submitError" class="msg msg--error">{{ submitError }}</div>

					<section class="formCard">
						<div class="formCard__header">
							<div class="formCard__title">Información principal</div>
							<div class="formCard__sub">Actualiza los datos base de la solicitud.</div>
						</div>

						<div class="row2">
							<div class="field">
								<label>Área *</label>
								<div class="selectWrap">
									<select v-model="form.areaId" class="selectNative">
										<option value="" disabled>
											{{ isLoadingAreas ? "Cargando áreas..." : areas.length ? "Selecciona un área" : "No hay áreas disponibles" }}
										</option>
										<option v-for="area in areas" :key="area.id" :value="String(area.id)">
											{{ area.name }}
										</option>
									</select>
								</div>
							</div>

							<div class="field">
								<label>Tipo de Solicitud *</label>
								<div class="selectWrap">
									<select v-model="form.typeId" class="selectNative" :disabled="!form.areaId || isLoadingTypes">
										<option value="" disabled>
											{{ !form.areaId ? "Selecciona un área primero" : isLoadingTypes ? "Cargando tipos..." : requestTypes.length ? "Selecciona un tipo" : "No hay tipos disponibles" }}
										</option>
										<option v-for="type in requestTypes" :key="type.id" :value="String(type.id)">
											{{ type.name }}
										</option>
									</select>
								</div>
							</div>
						</div>

						<div class="field">
							<label>Prioridad *</label>
							<div class="prio">
								<button class="prioCard prioCard--low" :class="{ 'prioCard--active': form.priority === 'Baja' }" type="button" @click="form.priority = 'Baja'">
									<div class="prioHead">
										<div class="prioDot prioDot--low"></div>
										<div class="prioMain">Baja</div>
									</div>
									<div class="prioSub">Prioridad baja</div>
								</button>

								<button class="prioCard prioCard--mid" :class="{ 'prioCard--active': form.priority === 'Media' }" type="button" @click="form.priority = 'Media'">
									<div class="prioHead">
										<div class="prioDot prioDot--mid"></div>
										<div class="prioMain">Media</div>
									</div>
									<div class="prioSub">Prioridad media</div>
								</button>

								<button class="prioCard prioCard--high" :class="{ 'prioCard--active': form.priority === 'Alta' }" type="button" @click="form.priority = 'Alta'">
									<div class="prioHead">
										<div class="prioDot prioDot--high"></div>
										<div class="prioMain">Alta</div>
									</div>
									<div class="prioSub">Prioridad alta</div>
								</button>
							</div>
						</div>

						<div class="field field--wide">
							<label>Asunto *</label>
							<input v-model.trim="form.subject" type="text" placeholder="Describe brevemente el motivo de la solicitud." />
						</div>

						<div class="field field--wide">
							<label>Descripción Detallada *</label>
							<textarea v-model.trim="form.description" rows="5" placeholder="Proporciona todos los detalles relevantes sobre la solicitud..." />
						</div>
					</section>

					<section v-if="canManageStatus" class="formCard">
						<div class="formCard__header">
							<div class="formCard__title">Estado de la solicitud</div>
							<div class="formCard__sub">Solo Admin y SuperAdmin pueden cambiar el estado y rechazar con motivo.</div>
						</div>

						<div class="statusGridEdit">
							<button v-for="status in availableStatusOptions"
									:key="status.id"
									class="statusOptionEdit"
									:class="[
									`statusOptionEdit--${status.tone}`,
									{ 'statusOptionEdit--active': form.status === status.label }
								]"
									type="button"
									@click="form.status = status.label">
								{{ status.label }}
							</button>
						</div>

						<div v-if="form.status === 'Rechazada'" class="field field--wide">
							<label>Motivo de rechazo *</label>
							<textarea v-model.trim="form.rejectionReason"
									  rows="4"
									  placeholder="Escribe el motivo del rechazo..." />
						</div>
					</section>

					<section class="formCard">
						<div class="formCard__header">
							<div class="formCard__title">Adjunto</div>
							<div class="formCard__sub">Puedes reemplazar o quitar el archivo actual.</div>
						</div>

						<div v-if="displayAttachmentName" class="attachedFileCard">
							<div class="attachedFileCard__main">
								<div class="attachedFileCard__icon">
									<svg viewBox="0 0 24 24"><path :d="iconPath('file')" /></svg>
								</div>
								<div class="attachedFileCard__content">
									<div class="attachedFileCard__label">Archivo actual</div>
									<div class="attachedFileCard__name">{{ displayAttachmentName }}</div>
								</div>
							</div>

							<div class="attachedFileCard__actions">
								<a v-if="displayAttachmentUrl && !form.attachment"
								   class="miniBtn miniBtn--view"
								   :href="displayAttachmentUrl"
								   target="_blank"
								   rel="noopener noreferrer">
									<svg viewBox="0 0 24 24"><path :d="iconPath('open')" /></svg>
									Abrir
								</a>

								<a v-if="displayAttachmentUrl && !form.attachment"
								   class="miniBtn miniBtn--download"
								   :href="displayAttachmentUrl"
								   download
								   target="_blank"
								   rel="noopener noreferrer">
									<svg viewBox="0 0 24 24"><path :d="iconPath('download')" /></svg>
									Descargar
								</a>

								<button class="miniBtn miniBtn--danger" type="button" @click="removeCurrentAttachment">
									<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
									Quitar
								</button>
							</div>
						</div>

						<div class="field">
							<label>Reemplazar Archivo (Opcional)</label>
							<div class="drop">
								<div class="drop__icon" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('upload')" /></svg>
								</div>
								<div class="drop__txt">
									<div class="drop__title">
										{{ form.attachment ? form.attachment.name : "Selecciona un archivo para reemplazar el actual o deja el existente" }}
									</div>
									<div class="drop__sub">PDF, JPG, PNG Max: 10MB</div>
								</div>

								<input ref="attachmentInputRef" class="drop__file" type="file" accept=".pdf,.jpg,.jpeg,.png" @change="onPickFile" />
							</div>
						</div>
					</section>

					<div class="actions">
						<button class="btnGhost" type="button" @click="closeEdit">Cancelar</button>
						<button class="btnPrimary" type="button" :disabled="isSubmitting" @click="submitEdit">
							<span class="btnPrimary__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('paperPlane')" /></svg>
							</span>
							{{ isSubmitting ? "Guardando..." : "Guardar Cambios" }}
						</button>
					</div>
				</div>
			</div>
		</div>
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
		max-width: 980px;
		max-height: calc(100vh - 32px);
		border-radius: 22px;
		background: linear-gradient(180deg, rgba(255, 255, 255, 0.84), rgba(245, 242, 255, 0.84));
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 30px 80px rgba(40, 55, 95, 0.22);
		overflow: hidden;
		display: grid;
		grid-template-rows: auto 1fr;
	}

	.modal__head {
		padding: 14px 16px;
		border-bottom: 1px solid rgba(110, 102, 182, 0.08);
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
	}

	.modal__left {
		display: flex;
		gap: 10px;
		align-items: flex-start;
	}

	.modalBack,
	.modalClose {
		width: 34px;
		height: 34px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.65);
		display: grid;
		place-items: center;
		cursor: pointer;
		color: rgba(90, 82, 160, 0.9);
	}

		.modalBack svg,
		.modalClose svg {
			width: 16px;
			height: 16px;
		}

		.modalBack path,
		.modalClose path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.modal__head h2 {
		margin: 0;
		font-size: 18px;
		font-weight: 900;
		color: #232a52;
	}

	.modal__head p {
		margin: 4px 0 0;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.55);
	}

	.modal__body {
		padding: 14px;
		display: grid;
		gap: 12px;
		overflow: auto;
	}

	.formCard {
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.72);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 14px 30px rgba(40, 55, 95, 0.05);
		padding: 14px;
		display: grid;
		gap: 12px;
	}

	.formCard__header {
		display: grid;
		gap: 3px;
	}

	.formCard__title {
		font-size: 14px;
		font-weight: 900;
		color: #232a52;
	}

	.formCard__sub {
		font-size: 12px;
		color: rgba(39, 46, 86, 0.55);
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

	.row2 {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 12px;
	}

	.field {
		display: grid;
		gap: 6px;
	}

		.field label {
			display: block;
			font-size: 11px;
			font-weight: 900;
			color: rgba(39, 46, 86, 0.78);
		}

	.selectWrap {
		position: relative;
	}

	.selectNative {
		width: 100%;
		height: 42px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.14);
		background: rgba(255, 255, 255, 0.76);
		padding: 0 38px 0 12px;
		font-size: 13px;
		font-weight: 800;
		color: #4a4f78;
		outline: none;
		appearance: none;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24'%3E%3Cpath d='M7 10l5 5 5-5' fill='none' stroke='%23706f8f' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
		background-repeat: no-repeat;
		background-position: right 12px center;
		background-size: 16px;
		box-shadow: 0 10px 24px rgba(40, 55, 95, 0.05);
	}

	.statusGridEdit {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 10px;
	}

	.statusOptionEdit {
		height: 42px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.72);
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
	}

	.statusOptionEdit--active {
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.22), 0 14px 26px rgba(40, 55, 95, 0.08);
	}

	.statusOptionEdit--new {
		color: rgba(96, 73, 219, 0.98);
		background: rgba(150, 122, 255, 0.12);
	}

	.statusOptionEdit--in-progress {
		color: rgba(180, 120, 30, 0.98);
		background: rgba(255, 197, 90, 0.16);
	}

	.statusOptionEdit--resolved {
		color: rgba(35, 130, 100, 0.98);
		background: rgba(60, 196, 151, 0.12);
	}

	.statusOptionEdit--closed {
		color: rgba(72, 89, 125, 0.98);
		background: rgba(184, 194, 218, 0.18);
	}

	.statusOptionEdit--rejected {
		color: rgba(170, 40, 80, 0.98);
		background: rgba(255, 90, 130, 0.12);
	}

	.prio {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 10px;
	}

	.prioCard {
		min-height: 70px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.68);
		cursor: pointer;
		text-align: left;
		padding: 10px 12px;
		box-shadow: 0 12px 22px rgba(40, 55, 95, 0.06);
		display: grid;
		gap: 6px;
		align-content: center;
	}

	.prioCard--active {
		border-color: rgba(120, 105, 235, 0.5);
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.28), 0 14px 26px rgba(40, 55, 95, 0.08);
	}

	.prioHead {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	.prioDot {
		width: 10px;
		height: 10px;
		border-radius: 999px;
	}

	.prioMain {
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.82);
	}

	.prioSub {
		font-size: 10px;
		color: rgba(39, 46, 86, 0.45);
	}

	.prioDot--low {
		background: rgba(60, 196, 151, 0.95);
	}

	.prioDot--mid {
		background: rgba(255, 197, 90, 0.95);
	}

	.prioDot--high {
		background: rgba(255, 90, 130, 0.95);
	}

	.field--wide input,
	.field--wide textarea,
	.field input,
	.field textarea {
		box-sizing: border-box;
		display: block;
		width: 100%;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.7);
		outline: none;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
	}

	.field input {
		height: 42px;
		padding: 0 12px;
	}

	.field textarea {
		padding: 12px;
		resize: vertical;
		min-height: 120px;
		max-height: 240px;
	}

	.attachedFileCard {
		display: grid;
		gap: 12px;
		padding: 14px;
		border-radius: 16px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.62);
		box-shadow: 0 12px 24px rgba(40, 55, 95, 0.04);
	}

	.attachedFileCard__main {
		display: flex;
		align-items: center;
		gap: 12px;
		min-width: 0;
	}

	.attachedFileCard__icon {
		width: 40px;
		height: 40px;
		border-radius: 14px;
		background: rgba(120, 105, 235, 0.14);
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
		flex: 0 0 auto;
	}

		.attachedFileCard__icon svg {
			width: 18px;
			height: 18px;
		}

		.attachedFileCard__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.attachedFileCard__content {
		min-width: 0;
	}

	.attachedFileCard__label {
		font-size: 11px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.55);
	}

	.attachedFileCard__name {
		margin-top: 3px;
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.82);
		word-break: break-word;
	}

	.attachedFileCard__actions {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-wrap: wrap;
	}

	.miniBtn {
		height: 34px;
		padding: 0 12px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.74);
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		font-size: 12px;
		font-weight: 900;
		text-decoration: none;
		cursor: pointer;
		box-shadow: 0 10px 22px rgba(40, 55, 95, 0.06);
	}

		.miniBtn svg {
			width: 14px;
			height: 14px;
		}

		.miniBtn path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.miniBtn--view {
		color: rgba(60, 110, 220, 0.95);
	}

	.miniBtn--download {
		color: rgba(88, 78, 212, 0.95);
	}

	.miniBtn--danger {
		color: rgba(170, 40, 80, 0.95);
	}

	.drop {
		position: relative;
		border-radius: 14px;
		border: 1px dashed rgba(110, 102, 182, 0.22);
		background: rgba(255, 255, 255, 0.5);
		padding: 12px;
		display: flex;
		gap: 12px;
		align-items: center;
		min-height: 82px;
	}

	.drop__icon {
		width: 36px;
		height: 36px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.14);
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
		flex: 0 0 auto;
	}

		.drop__icon svg {
			width: 18px;
			height: 18px;
		}

		.drop__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.drop__title {
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.75);
		word-break: break-word;
	}

	.drop__sub {
		margin-top: 4px;
		font-size: 10px;
		color: rgba(39, 46, 86, 0.45);
	}

	.drop__file {
		position: absolute;
		inset: 0;
		opacity: 0;
		cursor: pointer;
	}

	.actions {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 12px;
	}

	.btnGhost {
		height: 42px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.66);
		font-weight: 900;
		color: rgba(39, 46, 86, 0.65);
		cursor: pointer;
	}

	.btnPrimary {
		height: 42px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: linear-gradient(90deg, rgba(90, 80, 220, 0.9), rgba(160, 86, 255, 0.86));
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
		box-shadow: 0 18px 34px rgba(88, 78, 212, 0.18);
	}

		.btnPrimary:disabled {
			opacity: 0.7;
			cursor: not-allowed;
		}

	.btnPrimary__icon svg {
		width: 16px;
		height: 16px;
	}

	.btnPrimary__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
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

		.row2,
		.prio,
		.actions,
		.statusGridEdit {
			grid-template-columns: 1fr;
		}
	}
</style>