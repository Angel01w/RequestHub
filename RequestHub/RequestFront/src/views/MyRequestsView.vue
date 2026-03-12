<script setup>
	import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
	import { useRouter } from "vue-router";
	import ConfirmModal from "./ConfirmModal.vue"

	const router = useRouter();

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "");

	const isNewOpen = ref(false);
	const isDragging = ref(false);
	const isSubmitting = ref(false);
	const isLoadingRequests = ref(false);
	const isLoadingAreas = ref(false);
	const isLoadingTypes = ref(false);
	const deletingId = ref(null);
	const takingId = ref(null);
	const modalMode = ref("create");
	const editingRequestId = ref(null);
	const activeDropdown = ref(null);

	const search = ref("");
	const requests = ref([]);
	const areas = ref([]);
	const requestTypes = ref([]);
	const submitError = ref("");
	const submitSuccess = ref("");

	const filters = ref({
		status: "",
		areaId: "",
		priority: "",
		datePreset: "all",
		dateFrom: "",
		dateTo: ""
	});

	const form = ref(createDefaultForm());

	const priorityCatalog = Object.freeze([
		{ id: 1, label: "Baja" },
		{ id: 2, label: "Media" },
		{ id: 3, label: "Alta" }
	]);

	const requestStatusCatalog = Object.freeze([
		{ id: 1, label: "Nueva", tone: "new" },
		{ id: 2, label: "En Proceso", tone: "progress" },
		{ id: 3, label: "Resuelta", tone: "done" },
		{ id: 4, label: "Cerrada", tone: "closed" },
		{ id: 5, label: "Rechazada", tone: "rejected" }
	]);

	const priorityIdToLabel = Object.freeze({
		1: "Baja",
		2: "Media",
		3: "Alta"
	});

	const statusIdToLabel = Object.freeze({
		1: "Nueva",
		2: "En Proceso",
		3: "Resuelta",
		4: "Cerrada",
		5: "Rechazada"
	});

	const authHeaders = computed(() => ({
		Accept: "application/json"
	}));

	const hasRequests = computed(() => filteredRequests.value.length > 0);
	const isEditing = computed(() => modalMode.value === "edit");
	const priorityOptions = computed(() => priorityCatalog);
	const statusOptions = computed(() => requestStatusCatalog);

	const modalTitle = computed(() => (isEditing.value ? "Editar Solicitud" : "Nueva Solicitud"));
	const modalSubtitle = computed(() => (isEditing.value ? "Actualiza la información de la solicitud" : "Completa el formulario para crear tu solicitud"));
	const submitButtonText = computed(() => {
		if (isSubmitting.value) return isEditing.value ? "Guardando..." : "Enviando...";
		return isEditing.value ? "Guardar Cambios" : "Enviar Solicitud";
	});

	const areaFilterOptions = computed(() => {
		const map = new Map();

		for (const area of areas.value) {
			const id = String(area.id || "").trim();
			const name = String(area.name || "").trim();
			if (id && name) {
				map.set(id, { id, name });
			}
		}

		for (const request of requests.value) {
			const id = String(request.areaId || "").trim();
			const name = String(request.areaName || "").trim();
			if (id && name && !map.has(id)) {
				map.set(id, { id, name });
			}
		}

		return Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name, "es"));
	});

	const statusFilterLabel = computed(() => filters.value.status || "Todos los Estados");
	const areaFilterLabel = computed(() => {
		if (!filters.value.areaId) return "Todas las Áreas";
		const area = areaFilterOptions.value.find(x => String(x.id) === String(filters.value.areaId));
		return area?.name || "Todas las Áreas";
	});
	const priorityFilterLabel = computed(() => filters.value.priority || "Todas las Prioridades");
	const dateFilterLabel = computed(() => {
		if (filters.value.dateFrom || filters.value.dateTo) return "Rango personalizado";
		switch (filters.value.datePreset) {
			case "today":
				return "Hoy";
			case "7d":
				return "Últimos 7 días";
			case "30d":
				return "Últimos 30 días";
			case "month":
				return "Este mes";
			default:
				return "Todas las fechas";
		}
	});

	const activeFilterCount = computed(() => {
		let total = 0;
		if (filters.value.status) total++;
		if (filters.value.areaId) total++;
		if (filters.value.priority) total++;
		if (filters.value.datePreset !== "all" || filters.value.dateFrom || filters.value.dateTo) total++;
		return total;
	});

	const filteredRequests = computed(() => {
		const term = search.value.trim().toLowerCase();
		const selectedStatus = normalizeStatusKey(filters.value.status);
		const selectedAreaId = String(filters.value.areaId || "").trim();
		const selectedPriority = String(filters.value.priority || "").trim().toLowerCase();
		const range = resolveDateRange(filters.value.datePreset, filters.value.dateFrom, filters.value.dateTo);

		return requests.value.filter(item => {
			const matchesSearch =
				!term ||
				String(item.requestNumber || "").toLowerCase().includes(term) ||
				String(item.subject || "").toLowerCase().includes(term);

			const matchesStatus =
				!selectedStatus ||
				normalizeStatusKey(item.status) === selectedStatus;

			const matchesArea =
				!selectedAreaId ||
				String(item.areaId || "") === selectedAreaId;

			const matchesPriority =
				!selectedPriority ||
				String(item.priority || "").trim().toLowerCase() === selectedPriority;

			const createdAtDate = parseDateValue(item.createdAt);

			const matchesDate =
				!range ||
				(createdAtDate &&
					createdAtDate.getTime() >= range.start.getTime() &&
					createdAtDate.getTime() <= range.end.getTime());

			return matchesSearch && matchesStatus && matchesArea && matchesPriority && matchesDate;
		});
	});

	const formStatusTone = computed(() => resolveStatusTone(form.value.status));
	const formCreatedAtPreview = computed(() => formatDateTime(form.value.createdAt || new Date()));

	function createDefaultForm() {
		return {
			id: "",
			areaId: "",
			typeId: "",
			priority: "",
			status: "Nueva",
			rejectionReason: "",
			createdAt: toIsoLocalDateTime(new Date()),
			subject: "",
			description: "",
			attachment: null
		};
	}

	function resetForm() {
		form.value = createDefaultForm();
		requestTypes.value = [];
		submitError.value = "";
		submitSuccess.value = "";
		editingRequestId.value = null;
		modalMode.value = "create";
	}

	function resetFilters() {
		filters.value = {
			status: "",
			areaId: "",
			priority: "",
			datePreset: "all",
			dateFrom: "",
			dateTo: ""
		};
		activeDropdown.value = null;
	}

	function openNew() {
		resetForm();
		isNewOpen.value = true;
	}

	async function openEdit(item) {
		resetForm();
		modalMode.value = "edit";
		editingRequestId.value = item.id;
		form.value.id = String(item.id ?? "");

		const resolvedAreaId = item.areaId || resolveAreaIdByName(item.areaName);
		form.value.areaId = resolvedAreaId ? String(resolvedAreaId) : "";

		if (form.value.areaId) {
			await loadTypesByArea(form.value.areaId);
		}

		const resolvedTypeId = item.typeId || resolveTypeIdByName(item.typeName, form.value.areaId);
		form.value.typeId = resolvedTypeId ? String(resolvedTypeId) : "";
		form.value.priority = resolvePriorityLabel(item.priority, item.priorityId);
		form.value.status = resolveStatusLabel(item.status, item.statusId);
		form.value.rejectionReason = item.rejectionReason || "";
		form.value.createdAt = item.createdAt ? toIsoLocalDateTime(item.createdAt) : toIsoLocalDateTime(new Date());
		form.value.subject = item.subject || "";
		form.value.description = item.description || "";
		form.value.attachment = null;

		isNewOpen.value = true;
	}

	function openDetail(item) {
		router.push({
			name: "RequestDetail",
			params: { id: item.id }
		});
	}

	function closeNew() {
		isNewOpen.value = false;
		isDragging.value = false;
		resetForm();
	}

	function onBack() {
		router.back();
	}

	function onPickFile(e) {
		if (isEditing.value) return;
		const file = e?.target?.files?.[0] ?? null;
		form.value.attachment = file;
	}

	function onDrop(e) {
		if (isEditing.value) return;
		isDragging.value = false;
		const file = e?.dataTransfer?.files?.[0] ?? null;
		form.value.attachment = file;
	}

	function toggleDropdown(name) {
		activeDropdown.value = activeDropdown.value === name ? null : name;
	}

	function closeDropdowns() {
		activeDropdown.value = null;
	}

	function selectStatus(value) {
		filters.value.status = value;
		closeDropdowns();
	}

	function selectArea(value) {
		filters.value.areaId = value;
		closeDropdowns();
	}

	function selectPriority(value) {
		filters.value.priority = value;
		closeDropdowns();
	}

	function selectDatePreset(value) {
		filters.value.datePreset = value;
		if (value !== "custom") {
			filters.value.dateFrom = "";
			filters.value.dateTo = "";
			closeDropdowns();
		}
	}

	function onGlobalClick(event) {
		const target = event.target;
		if (!(target instanceof HTMLElement)) return;
		if (!target.closest(".filterDropdown")) {
			closeDropdowns();
		}
	}

	function onEsc(event) {
		if (event.key === "Escape") {
			closeDropdowns();
		}
	}

	function buildEndOfDay(date) {
		return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999);
	}

	function parseDateValue(value) {
		if (!value) return null;
		const date = value instanceof Date ? value : new Date(value);
		if (Number.isNaN(date.getTime())) return null;
		return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0);
	}

	function resolveDateRange(datePreset, dateFrom, dateTo) {
		const now = new Date();
		const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0, 0);
		const todayEnd = buildEndOfDay(todayStart);

		if (dateFrom || dateTo) {
			const start = dateFrom ? parseDateValue(dateFrom) : new Date(2000, 0, 1);
			const endBase = dateTo ? parseDateValue(dateTo) : todayStart;
			const end = endBase ? buildEndOfDay(endBase) : null;
			if (!start || !end) return null;
			return { start, end };
		}

		switch (datePreset) {
			case "today":
				return { start: todayStart, end: todayEnd };
			case "7d": {
				const start = new Date(todayStart);
				start.setDate(start.getDate() - 6);
				return { start, end: todayEnd };
			}
			case "30d": {
				const start = new Date(todayStart);
				start.setDate(start.getDate() - 29);
				return { start, end: todayEnd };
			}
			case "month": {
				const start = new Date(now.getFullYear(), now.getMonth(), 1);
				return { start, end: todayEnd };
			}
			default:
				return null;
		}
	}

	function toIsoLocalDateTime(value) {
		const date = value instanceof Date ? value : new Date(value);
		if (Number.isNaN(date.getTime())) return "";
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, "0");
		const day = String(date.getDate()).padStart(2, "0");
		const hours = String(date.getHours()).padStart(2, "0");
		const minutes = String(date.getMinutes()).padStart(2, "0");
		return `${year}-${month}-${day}T${hours}:${minutes}`;
	}

	function formatDate(value) {
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) return "Sin fecha";
		return new Intl.DateTimeFormat("es-DO", {
			day: "2-digit",
			month: "2-digit",
			year: "numeric"
		}).format(date);
	}

	function formatDateTime(value) {
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) return "Sin fecha";
		return new Intl.DateTimeFormat("es-DO", {
			day: "2-digit",
			month: "2-digit",
			year: "numeric",
			hour: "2-digit",
			minute: "2-digit"
		}).format(date);
	}

	function normalizeStatusKey(value) {
		return String(value || "").trim().toLowerCase().replace(/\s+/g, "");
	}

	function canonicalStatusLabel(value) {
		const normalized = normalizeStatusKey(value);
		if (normalized === "nueva") return "Nueva";
		if (normalized === "enproceso") return "En Proceso";
		if (normalized === "resuelta") return "Resuelta";
		if (normalized === "cerrada") return "Cerrada";
		if (normalized === "rechazada") return "Rechazada";
		return typeof value === "string" ? value.trim() : "";
	}

	function resolvePriorityLabel(priorityValue, priorityId) {
		if (typeof priorityValue === "string" && priorityValue.trim()) return priorityValue.trim();
		if (priorityValue && typeof priorityValue === "object") {
			return priorityValue.name ?? priorityValue.nombre ?? priorityValue.label ?? "";
		}
		return priorityIdToLabel[Number(priorityId)] || "";
	}

	function resolveStatusLabel(statusValue, statusId) {
		if (typeof statusValue === "string" && statusValue.trim()) {
			return canonicalStatusLabel(statusValue);
		}
		if (statusValue && typeof statusValue === "object") {
			return canonicalStatusLabel(
				statusValue.name ??
				statusValue.nombre ??
				statusValue.label ??
				statusValue.statusName ??
				""
			);
		}
		return statusIdToLabel[Number(statusId)] || "Nueva";
	}

	function resolveStatusTone(value) {
		const normalized = normalizeStatusKey(value);
		if (normalized === "nueva") return "new";
		if (normalized === "enproceso") return "progress";
		if (normalized === "resuelta") return "done";
		if (normalized === "cerrada") return "closed";
		if (normalized === "rechazada") return "rejected";
		return "neutral";
	}

	function getPriorityId(priority) {
		const normalized = String(priority || "").trim().toLowerCase();
		const found = priorityCatalog.find(x => x.label.toLowerCase() === normalized);
		return found?.id ?? 0;
	}

	function getStatusId(status) {
		const normalized = normalizeStatusKey(status);
		const found = requestStatusCatalog.find(x => normalizeStatusKey(x.label) === normalized);
		return found?.id ?? 1;
	}

	function resolveAreaIdByName(name) {
		if (!name) return "";
		const found = areas.value.find(x => String(x.name || "").trim().toLowerCase() === String(name).trim().toLowerCase());
		return found?.id ?? "";
	}

	function resolveTypeIdByName(name, areaId) {
		if (!name) return "";
		const normalizedName = String(name).trim().toLowerCase();
		const found = requestTypes.value.find(x => {
			const sameName = String(x.name || "").trim().toLowerCase() === normalizedName;
			const sameArea = areaId ? String(x.areaId) === String(areaId) : true;
			return sameName && sameArea;
		});
		return found?.id ?? "";
	}

	function firstNonEmpty(...values) {
		for (const value of values) {
			if (value === 0) return value;
			if (value === false) return value;
			if (value == null) continue;
			if (typeof value === "string" && !value.trim()) continue;
			return value;
		}
		return "";
	}

	function normalizeStatusId(item) {
		return Number(
			firstNonEmpty(
				item.statusId,
				item.StatusId,
				item.requestStatusId,
				item.RequestStatusId,
				item.serviceRequestStatusId,
				item.ServiceRequestStatusId,
				item.idStatus,
				item.idEstado,
				item.stateId,
				item.StateId,
				item.status?.id,
				item.status?.statusId,
				item.status?.requestStatusId,
				item.requestStatus?.id,
				item.requestStatus?.statusId,
				item.requestStatus?.requestStatusId,
				item.RequestStatus?.Id,
				item.RequestStatus?.StatusId,
				item.RequestStatus?.RequestStatusId,
				item.state?.id,
				item.Status?.Id
			)
		) || 0;
	}

	function normalizeStatusText(item, statusId) {
		return resolveStatusLabel(
			firstNonEmpty(
				item.statusName,
				item.StatusName,
				item.requestStatusName,
				item.RequestStatusName,
				item.status,
				item.Status,
				item.stateName,
				item.StateName,
				item.estado,
				item.requestStatus?.name,
				item.requestStatus?.nombre,
				item.requestStatus?.label,
				item.requestStatus?.statusName,
				item.status?.name,
				item.status?.nombre,
				item.status?.label,
				item.status?.statusName,
				item.state?.name,
				item.RequestStatus?.Name,
				item.RequestStatus?.Nombre,
				item.RequestStatus?.Label,
				item.RequestStatus?.StatusName
			),
			statusId
		);
	}

	function normalizeRequest(item, index) {
		const priorityId = Number(
			firstNonEmpty(
				item.priorityId,
				item.PriorityId,
				item.idPriority,
				item.idPrioridad,
				item.priority?.id,
				item.priority?.priorityId,
				item.Priority?.Id,
				item.priority?.Id
			)
		) || 0;

		const statusId = normalizeStatusId(item);

		const createdAt = firstNonEmpty(
			item.createdAt,
			item.createdAtUtc,
			item.CreatedAt,
			item.CreatedAtUtc,
			item.creationDate,
			item.createdDate,
			item.fechaCreacion,
			item.FechaCreacion,
			item.requestDate,
			item.RequestDate,
			item.date,
			item.Date,
			new Date().toISOString()
		);

		const status = canonicalStatusLabel(normalizeStatusText(item, statusId));

		return {
			id: firstNonEmpty(item.id, item.serviceRequestId, item.requestId, index + 1),
			requestNumber: firstNonEmpty(
				item.requestNumber,
				item.number,
				item.code,
				item.Number,
				`SOL-${new Date(createdAt).getFullYear()}-${String(firstNonEmpty(item.id, item.serviceRequestId, index + 1)).padStart(4, "0")}`
			),
			subject: firstNonEmpty(item.subject, item.Subject, ""),
			description: firstNonEmpty(item.description, item.Description, ""),
			areaId: firstNonEmpty(
				item.areaId,
				item.AreaId,
				item.idArea,
				item.area?.id,
				item.area?.areaId,
				item.Area?.Id,
				""
			),
			areaName: firstNonEmpty(
				item.areaName,
				item.area?.name,
				item.area?.nombre,
				item.area,
				item.Area,
				""
			),
			typeId: firstNonEmpty(
				item.requestTypeId,
				item.typeId,
				item.RequestTypeId,
				item.idTipoSolicitud,
				item.requestType?.id,
				item.requestType?.requestTypeId,
				item.RequestType?.Id,
				""
			),
			typeName: firstNonEmpty(
				item.requestTypeName,
				item.typeName,
				item.requestType?.name,
				item.requestType?.nombre,
				item.requestType,
				item.RequestType,
				""
			),
			priorityId,
			priority: resolvePriorityLabel(firstNonEmpty(item.priority, item.priorityName, item.Priority), priorityId),
			statusId: statusId || getStatusId(status),
			status,
			statusTone: resolveStatusTone(status),
			rejectionReason: firstNonEmpty(
				item.rejectionReason,
				item.rejectReason,
				item.reason,
				item.motivoRechazo,
				item.RejectionReason,
				item.Reason,
				item.MotivoRechazo,
				""
			),
			createdAt
		};
	}

	function normalizeAreasPayload(rawData) {
		let list = [];

		if (Array.isArray(rawData)) list = rawData;
		else if (Array.isArray(rawData?.items)) list = rawData.items;
		else if (Array.isArray(rawData?.data)) list = rawData.data;
		else if (Array.isArray(rawData?.result)) list = rawData.result;
		else if (Array.isArray(rawData?.areas)) list = rawData.areas;
		else if (Array.isArray(rawData?.value)) list = rawData.value;

		return list
			.map(a => ({
				id: a.id ?? a.areaId ?? a.Id ?? a.AreaId ?? a.idArea ?? a.ID_AREA ?? "",
				name: a.name ?? a.nombre ?? a.Name ?? a.Nombre ?? a.description ?? a.descripcion ?? ""
			}))
			.filter(a => a.id !== "" && a.name !== "");
	}

	function normalizeTypesPayload(rawData) {
		let list = [];

		if (Array.isArray(rawData)) list = rawData;
		else if (Array.isArray(rawData?.items)) list = rawData.items;
		else if (Array.isArray(rawData?.data)) list = rawData.data;
		else if (Array.isArray(rawData?.result)) list = rawData.result;
		else if (Array.isArray(rawData?.requestTypes)) list = rawData.requestTypes;
		else if (Array.isArray(rawData?.types)) list = rawData.types;
		else if (Array.isArray(rawData?.value)) list = rawData.value;

		return list
			.map(t => ({
				id: t.id ?? t.requestTypeId ?? t.Id ?? t.RequestTypeId ?? t.idTipoSolicitud ?? "",
				name: t.name ?? t.nombre ?? t.Name ?? t.Nombre ?? t.description ?? t.descripcion ?? "",
				areaId: t.areaId ?? t.AreaId ?? t.idArea ?? t.area?.id ?? t.area?.areaId ?? ""
			}))
			.filter(t => t.id !== "" && t.name !== "");
	}

	async function apiRequest(url, options = {}) {
		const response = await fetch(url, {
			...options,
			headers: {
				...authHeaders.value,
				...(options.headers || {})
			}
		});

		const contentType = response.headers.get("content-type") || "";
		let payload = null;

		if (contentType.includes("application/json")) {
			payload = await response.json();
		} else {
			const text = await response.text();
			payload = text ? { message: text } : null;
		}

		if (!response.ok) {
			const message =
				payload?.message ||
				payload?.title ||
				(typeof payload === "string" ? payload : "") ||
				`HTTP ${response.status}`;
			throw new Error(message);
		}

		return payload;
	}

	async function loadRequests() {
		isLoadingRequests.value = true;
		try {
			const data = await apiRequest(`${API_BASE}/api/ServiceRequests`);
			const list = Array.isArray(data) ? data : data?.items || data?.data || data?.result || [];
			requests.value = list.map(normalizeRequest);
		} catch {
			requests.value = [];
		} finally {
			isLoadingRequests.value = false;
		}
	}

	async function loadAreas() {
		isLoadingAreas.value = true;
		try {
			const raw = await apiRequest(`${API_BASE}/api/Catalogs/areas`);
			areas.value = normalizeAreasPayload(raw);
		} catch {
			areas.value = [];
		} finally {
			isLoadingAreas.value = false;
		}
	}

	async function loadTypesByArea(areaId) {
		if (!areaId) {
			requestTypes.value = [];
			form.value.typeId = "";
			return;
		}

		isLoadingTypes.value = true;
		try {
			const raw = await apiRequest(`${API_BASE}/api/Catalogs/request-types`);
			const allTypes = normalizeTypesPayload(raw);
			requestTypes.value = allTypes.filter(t => String(t.areaId) === String(areaId));
		} catch {
			requestTypes.value = [];
		} finally {
			isLoadingTypes.value = false;
		}
	}

	function syncLocalRequestState(id, payload) {
		const index = requests.value.findIndex(x => String(x.id) === String(id));
		if (index < 0) return;
		requests.value[index] = normalizeRequest({ ...requests.value[index], ...payload }, index);
		requests.value = [...requests.value];
	}

	function buildCreatePayload() {
		const priorityId = getPriorityId(form.value.priority);
		const statusId = getStatusId(form.value.status);

		const formData = new FormData();
		formData.append("AreaId", String(Number(form.value.areaId)));
		formData.append("RequestTypeId", String(Number(form.value.typeId)));
		formData.append("PriorityId", String(priorityId));
		formData.append("StatusId", String(statusId));
		formData.append("Subject", form.value.subject.trim());
		formData.append("Description", form.value.description.trim());
		formData.append("CreatedAt", new Date(form.value.createdAt).toISOString());

		if (canonicalStatusLabel(form.value.status) === "Rechazada") {
			formData.append("RejectionReason", String(form.value.rejectionReason || "").trim());
		}

		if (form.value.attachment) {
			formData.append("attachment", form.value.attachment);
		}

		return formData;
	}

	function buildUpdatePayload() {
		const priorityId = getPriorityId(form.value.priority);
		const statusId = getStatusId(form.value.status);

		return {
			areaId: Number(form.value.areaId),
			requestTypeId: Number(form.value.typeId),
			priorityId,
			statusId,
			rejectionReason: canonicalStatusLabel(form.value.status) === "Rechazada" ? String(form.value.rejectionReason || "").trim() : null,
			subject: form.value.subject.trim(),
			description: form.value.description.trim()
		};
	}

	function validateForm() {
		if (!form.value.areaId) return "Debes seleccionar un área.";
		if (!form.value.typeId) return "Debes seleccionar un tipo de solicitud.";
		if (!form.value.priority) return "Debes seleccionar una prioridad.";
		if (!form.value.status) return "Debes seleccionar un estado.";
		if (!form.value.createdAt) return "Debes indicar la fecha de la solicitud.";
		if (!form.value.subject.trim()) return "Debes ingresar el asunto.";
		if (!form.value.description.trim()) return "Debes ingresar la descripción.";
		if (canonicalStatusLabel(form.value.status) === "Rechazada" && !String(form.value.rejectionReason || "").trim()) return "Debes indicar el motivo del rechazo.";
		if (!isEditing.value && form.value.attachment && form.value.attachment.size > 10 * 1024 * 1024) return "El archivo supera el máximo de 10MB.";
		return "";
	}

	async function submit() {
		submitError.value = "";
		submitSuccess.value = "";

		const validationError = validateForm();
		if (validationError) {
			submitError.value = validationError;
			return;
		}

		isSubmitting.value = true;

		try {
			if (isEditing.value && editingRequestId.value) {
				const updatePayload = buildUpdatePayload();

				const responsePayload = await apiRequest(`${API_BASE}/api/ServiceRequests/${editingRequestId.value}`, {
					method: "PUT",
					headers: {
						"Content-Type": "application/json"
					},
					body: JSON.stringify(updatePayload)
				});

				const responseNormalized = responsePayload ? normalizeRequest(responsePayload, 0) : null;
				const currentStatusLabel = canonicalStatusLabel(form.value.status);

				syncLocalRequestState(editingRequestId.value, {
					...updatePayload,
					status: currentStatusLabel,
					statusName: currentStatusLabel,
					requestStatusName: currentStatusLabel,
					statusId: getStatusId(currentStatusLabel),
					rejectionReason: currentStatusLabel === "Rechazada" ? form.value.rejectionReason : "",
					...(responseNormalized || {})
				});

				submitSuccess.value = "Solicitud actualizada correctamente.";
				closeNew();
				await loadRequests();
				return;
			}

			await apiRequest(`${API_BASE}/api/ServiceRequests`, {
				method: "POST",
				body: buildCreatePayload()
			});

			submitSuccess.value = "Solicitud creada correctamente.";
			closeNew();
			await loadRequests();
		} catch (error) {
			submitError.value = error?.message || `No se pudo ${isEditing.value ? "actualizar" : "crear"} la solicitud.`;
		} finally {
			isSubmitting.value = false;
		}
	}

	async function removeRequest(item) {
		const confirmed = window.confirm(`¿Seguro que deseas eliminar la solicitud ${item.requestNumber}?`);
		if (!confirmed) return;

		deletingId.value = item.id;

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${item.id}`, {
				method: "DELETE"
			});
			await loadRequests();
		} catch (error) {
			window.alert(error?.message || "No se pudo eliminar la solicitud.");
		} finally {
			deletingId.value = null;
		}
	}

	async function takeRequest(item) {
		takingId.value = item.id;
		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${item.id}/take`, {
				method: "POST"
			});
			await loadRequests();
		} catch (error) {
			window.alert(error?.message || "No se pudo tomar la solicitud.");
		} finally {
			takingId.value = null;
		}
	}

	function showRejectionReason(item) {
		const reason = String(item.rejectionReason || "").trim();
		window.alert(reason || "Esta solicitud fue rechazada sin motivo registrado.");
	}

	function iconPath(name) {
		switch (name) {
			case "arrowLeft":
				return "M15 18l-6-6 6-6";
			case "plus":
				return "M12 5v14M5 12h14";
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3";
			case "chev":
				return "M7 10l5 5 5-5";
			case "calendar":
				return "M7 3v3M17 3v3M4 8h16M5 6h14v15H5V6Z";
			case "paperPlane":
				return "M21 3 3 11l7 2 2 7 9-17ZM10 13l11-10";
			case "upload":
				return "M12 16V4m0 0 4 4M12 4 8 8M4 20h16";
			case "edit":
				return "M4 20h4l10.5-10.5a2.121 2.121 0 0 0-3-3L5 17v3Z";
			case "trash":
				return "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3";
			case "note":
				return "M9 12h6M9 16h6M9 8h6M6 4h9l3 3v13H6V4Zm9 0v3h3";
			case "eye":
				return "M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Zm10 3.2A3.2 3.2 0 1 0 12 8.8a3.2 3.2 0 0 0 0 6.4Z";
			case "take":
				return "M12 3v18M3 12h18";
			default:
				return "";
		}
	}

	watch(
		() => form.value.areaId,
		async (newAreaId, oldAreaId) => {
			if (String(newAreaId) !== String(oldAreaId)) {
				form.value.typeId = "";
			}
			await loadTypesByArea(newAreaId);
		}
	);

	watch(
		() => [filters.value.dateFrom, filters.value.dateTo],
		([from, to]) => {
			if (from || to) {
				filters.value.datePreset = "custom";
			} else if (filters.value.datePreset === "custom") {
				filters.value.datePreset = "all";
			}
		}
	);

	watch(
		() => form.value.status,
		value => {
			form.value.status = canonicalStatusLabel(value);
			if (canonicalStatusLabel(value) !== "Rechazada") {
				form.value.rejectionReason = "";
			}
		}
	);

	onMounted(async () => {
		document.addEventListener("click", onGlobalClick);
		document.addEventListener("keydown", onEsc);
		resetForm();
		await loadAreas();
		await loadRequests();
	});

	onBeforeUnmount(() => {
		document.removeEventListener("click", onGlobalClick);
		document.removeEventListener("keydown", onEsc);
	});
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
					<h1>Mis Solicitudes</h1>
					<p>Crea y da seguimiento a tus solicitudes internas</p>
				</div>

				<div class="headRight">
					<button class="newBtn" type="button" @click="openNew">
						<span class="newBtn__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('plus')" /></svg>
						</span>
						Nueva Solicitud
					</button>

					<div class="roleCard" aria-label="Rol">
						<div class="roleAvatar">S</div>
						<div class="roleTxt">
							<div class="roleLine"><b>Rol:</b> Solicitante</div>
							<div class="roleSub">Ves solo tus solicitudes</div>
						</div>
					</div>
				</div>
			</header>

			<section class="filtersSection">
				<div class="filtersLayout">
					<div class="filtersSearch">
						<div class="pill pill--search">
							<span class="pill__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
							</span>
							<input v-model.trim="search" class="pill__input" type="text" placeholder="Buscar por número o asunto..." />
						</div>
					</div>

					<div class="filtersControls">
						<div class="filterDropdown">
							<button class="pill pill--button" type="button" @click.stop="toggleDropdown('status')">
								<span class="pill__txt">{{ statusFilterLabel }}</span>
								<span class="pill__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>

							<div v-if="activeDropdown === 'status'" class="dropdownPanel dropdownPanel--status" @click.stop>
								<div class="dropdownHead">
									<div class="dropdownTitle">Estados</div>
									<button class="dropdownClear" type="button" @click="selectStatus('')">Todos</button>
								</div>

								<div class="statusGrid">
									<button v-for="status in statusOptions"
											:key="status.id"
											class="statusOption"
											:class="[ `statusOption--${status.tone}`, { 'statusOption--active': filters.status === status.label } ]"
											type="button"
											@click="selectStatus(status.label)">
										{{ status.label }}
									</button>
								</div>

								<div class="dropdownFoot dropdownFoot--hint">
									Filtra por estado actual de la solicitud.
								</div>
							</div>
						</div>

						<div class="filterDropdown">
							<button class="pill pill--button" type="button" @click.stop="toggleDropdown('area')">
								<span class="pill__txt">{{ areaFilterLabel }}</span>
								<span class="pill__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>

							<div v-if="activeDropdown === 'area'" class="dropdownPanel" @click.stop>
								<div class="dropdownHead">
									<div class="dropdownTitle">Áreas</div>
									<button class="dropdownClear" type="button" @click="selectArea('')">Todas</button>
								</div>

								<div class="optionList">
									<button class="optionItem" :class="{ 'optionItem--active': !filters.areaId }" type="button" @click="selectArea('')">
										Todas las Áreas
									</button>

									<button v-for="area in areaFilterOptions"
											:key="area.id"
											class="optionItem"
											:class="{ 'optionItem--active': String(filters.areaId) === String(area.id) }"
											type="button"
											@click="selectArea(String(area.id))">
										{{ area.name }}
									</button>
								</div>
							</div>
						</div>

						<div class="filterDropdown">
							<button class="pill pill--button" type="button" @click.stop="toggleDropdown('priority')">
								<span class="pill__txt">{{ priorityFilterLabel }}</span>
								<span class="pill__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>

							<div v-if="activeDropdown === 'priority'" class="dropdownPanel" @click.stop>
								<div class="dropdownHead">
									<div class="dropdownTitle">Prioridades</div>
									<button class="dropdownClear" type="button" @click="selectPriority('')">Todas</button>
								</div>

								<div class="optionList">
									<button class="optionItem" :class="{ 'optionItem--active': !filters.priority }" type="button" @click="selectPriority('')">
										Todas las Prioridades
									</button>

									<button v-for="priority in priorityOptions"
											:key="priority.id"
											class="optionItem"
											:class="{ 'optionItem--active': filters.priority === priority.label }"
											type="button"
											@click="selectPriority(priority.label)">
										{{ priority.label }}
									</button>
								</div>
							</div>
						</div>

						<div class="filterDropdown">
							<button class="pill pill--button" type="button" @click.stop="toggleDropdown('date')">
								<span class="pill__icon" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
								</span>
								<span class="pill__txt">{{ dateFilterLabel }}</span>
								<span class="pill__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>

							<div v-if="activeDropdown === 'date'" class="dropdownPanel dropdownPanel--date" @click.stop>
								<div class="dropdownHead">
									<div class="dropdownTitle">Fechas</div>
									<button class="dropdownClear" type="button" @click="selectDatePreset('all')">Todas</button>
								</div>

								<div class="optionList optionList--date">
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === 'all' && !filters.dateFrom && !filters.dateTo }" type="button" @click="selectDatePreset('all')">Todas las fechas</button>
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === 'today' }" type="button" @click="selectDatePreset('today')">Hoy</button>
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === '7d' }" type="button" @click="selectDatePreset('7d')">Últimos 7 días</button>
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === '30d' }" type="button" @click="selectDatePreset('30d')">Últimos 30 días</button>
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === 'month' }" type="button" @click="selectDatePreset('month')">Este mes</button>
									<button class="optionItem" :class="{ 'optionItem--active': filters.datePreset === 'custom' }" type="button" @click="selectDatePreset('custom')">Rango personalizado</button>
								</div>

								<div v-if="filters.datePreset === 'custom'" class="dateRange">
									<div class="dateField">
										<label>Desde</label>
										<input v-model="filters.dateFrom" class="dateFilter__input" type="date" />
									</div>
									<div class="dateField">
										<label>Hasta</label>
										<input v-model="filters.dateTo" class="dateFilter__input" type="date" />
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>

				<div class="filtersBar">
					<div class="filtersBar__left">
						<span class="filterChip" v-if="filters.status">{{ statusFilterLabel }}</span>
						<span class="filterChip" v-if="filters.areaId">{{ areaFilterLabel }}</span>
						<span class="filterChip" v-if="filters.priority">{{ priorityFilterLabel }}</span>
						<span class="filterChip" v-if="filters.datePreset !== 'all' || filters.dateFrom || filters.dateTo">{{ dateFilterLabel }}</span>
					</div>

					<div class="filtersBar__right">
						<span v-if="activeFilterCount" class="filtersCounter">
							{{ activeFilterCount }} filtro{{ activeFilterCount === 1 ? "" : "s" }} activo{{ activeFilterCount === 1 ? "" : "s" }}
						</span>

						<button v-if="activeFilterCount" class="clearFiltersBtn" type="button" @click="resetFilters">
							Limpiar filtros
						</button>
					</div>
				</div>
			</section>

			<div class="tablewrap" role="region" aria-label="Tabla de mis solicitudes">
				<table class="table" v-if="hasRequests">
					<colgroup>
						<col class="col-index" />
						<col class="col-number" />
						<col class="col-subject" />
						<col class="col-area" />
						<col class="col-type" />
						<col class="col-date" />
						<col class="col-priority" />
						<col class="col-status" />
						<col class="col-actions" />
					</colgroup>

					<thead>
						<tr>
							<th>N°</th>
							<th>NÚMERO SOLICITUD</th>
							<th>ASUNTO</th>
							<th>ÁREA</th>
							<th>TIPO</th>
							<th>FECHA</th>
							<th>PRIORIDAD</th>
							<th>ESTADO</th>
							<th>ACCIONES</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="(item, index) in filteredRequests" :key="item.id">
							<td>{{ index + 1 }}</td>
							<td class="requestNumberCell">{{ item.requestNumber }}</td>
							<td class="subjectCell">{{ item.subject }}</td>
							<td>{{ item.areaName }}</td>
							<td>{{ item.typeName }}</td>
							<td>{{ formatDate(item.createdAt) }}</td>
							<td class="priorityCell">
								<span class="priorityBadge" :class="`priorityBadge--${String(item.priority || '').toLowerCase()}`">
									{{ item.priority }}
								</span>
							</td>
							<td class="statusColCell">
								<div class="statusCell">
									<span class="statusBadge" :class="`statusBadge--${item.statusTone}`">
										{{ item.status || "Sin estado" }}
									</span>

									<button v-if="item.status === 'Rechazada'"
											class="reasonBtn"
											type="button"
											@click="showRejectionReason(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('note')" /></svg>
										Ver motivo
									</button>
								</div>
							</td>
							<td class="actionsCell">
								<div class="rowActions">
									<button class="iconAction iconAction--view" type="button" @click="openDetail(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('eye')" /></svg>
										Ver
									</button>
									<button class="iconAction iconAction--take" type="button" :disabled="takingId === item.id" @click="takeRequest(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('take')" /></svg>
										{{ takingId === item.id ? "Tomando..." : "Tomar" }}
									</button>
									<button class="iconAction iconAction--edit" type="button" @click="openEdit(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('edit')" /></svg>
										Editar
									</button>
									<button class="iconAction iconAction--delete" type="button" :disabled="deletingId === item.id" @click="removeRequest(item)">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
										{{ deletingId === item.id ? "Eliminando..." : "Eliminar" }}
									</button>
								</div>
							</td>
						</tr>
					</tbody>
				</table>

				<div v-else class="empty">
					<div class="empty__circle" aria-hidden="true">+</div>
					<div class="empty__title">
						{{ isLoadingRequests ? "Cargando solicitudes..." : "Aún no tienes solicitudes" }}
					</div>
					<div class="empty__sub">
						Crea una nueva solicitud para comenzar y podrás dar seguimiento<br />
						por fecha, estado, área y prioridad.
					</div>

					<div class="chips" aria-hidden="true">
						<span class="chip chip--new">Nueva</span>
						<span class="chip chip--proc">En Proceso</span>
						<span class="chip chip--done">Resuelta</span>
						<span class="chip chip--closed">Cerrada</span>
						<span class="chip chip--rejected">Rechazada</span>
					</div>
				</div>
			</div>

			<footer class="foot">
				<div>© 2026 RequestHub • Mesa de Servicios Internos</div>
				<div class="foot__sub">SOL-YYYY-0001 • JWT Auth • v1.0</div>
			</footer>
		</section>

		<button class="help" type="button" aria-label="Ayuda">?</button>

		<div v-if="isNewOpen" class="overlay" role="dialog" aria-modal="true" :aria-label="modalTitle">
			<div class="modal">
				<header class="modal__head">
					<div class="modal__left">
						<button class="modalBack" type="button" aria-label="Volver" @click="closeNew">
							<svg viewBox="0 0 24 24" aria-hidden="true">
								<path :d="iconPath('arrowLeft')" />
							</svg>
						</button>
						<div>
							<h2>{{ modalTitle }}</h2>
							<p>{{ modalSubtitle }}</p>
						</div>
					</div>
				</header>

				<div class="modal__body modal__body--layout">
					<div class="modalMain">
						<div v-if="submitError" class="msg msg--error">{{ submitError }}</div>
						<div v-if="submitSuccess" class="msg msg--ok">{{ submitSuccess }}</div>

						<section class="formCard">
							<div class="formCard__header">
								<div class="formCard__title">Información principal</div>
								<div class="formCard__sub">Completa los datos base de la solicitud.</div>
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
									<div class="helper">{{ !form.areaId ? "Selecciona un área primero." : "" }}</div>
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
								<textarea v-model.trim="form.description" rows="4" placeholder="Proporciona todos los detalles relevantes sobre tu solicitud..." />
							</div>
						</section>

						<section class="formCard">
							<div class="formCard__header">
								<div class="formCard__title">Adjunto</div>
								<div class="formCard__sub">Puedes incluir soporte visual o documental.</div>
							</div>

							<div class="field">
								<label>{{ isEditing ? "Archivo Adjunto (No disponible en edición)" : "Archivo Adjunto (Opcional)" }}</label>
								<div class="drop"
									 :class="{ 'drop--drag': isDragging, 'drop--disabled': isEditing }"
									 @dragenter.prevent="isEditing ? null : isDragging = true"
									 @dragover.prevent="isEditing ? null : isDragging = true"
									 @dragleave.prevent="isDragging = false"
									 @drop.prevent="onDrop">
									<div class="drop__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('upload')" /></svg>
									</div>
									<div class="drop__txt">
										<div class="drop__title">
											{{ isEditing ? "La edición no permite cambiar el archivo adjunto" : form.attachment ? form.attachment.name : "Arrastra y suelta un archivo aquí o haz clic para seleccionar" }}
										</div>
										<div class="drop__sub">PDF, JPG, PNG Max: 10MB</div>
									</div>

									<input class="drop__file" type="file" accept=".pdf,.jpg,.jpeg,.png" :disabled="isEditing" @change="onPickFile" />
								</div>
							</div>
						</section>

						<div class="actions">
							<button class="btnGhost" type="button" @click="closeNew">Cancelar</button>
							<button class="btnPrimary" type="button" :disabled="isSubmitting" @click="submit">
								<span class="btnPrimary__icon" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('paperPlane')" /></svg>
								</span>
								{{ submitButtonText }}
							</button>
						</div>
					</div>

					<aside class="modalSide">
						<div class="sideCard">
							<div class="sideCard__title">Estado de la solicitud</div>
							<div class="sideCard__sub">Selecciona el flujo actual antes de guardar.</div>

							<div class="statusFormGrid">
								<button v-for="status in statusOptions"
										:key="status.id"
										class="statusFormOption"
										:class="[ `statusFormOption--${status.tone}`, { 'statusFormOption--active': form.status === status.label } ]"
										type="button"
										@click="form.status = status.label">
									{{ status.label }}
								</button>
							</div>

							<div v-if="form.status === 'Rechazada'" class="field field--wide field--compact">
								<label>Motivo del rechazo *</label>
								<textarea v-model.trim="form.rejectionReason" rows="3" placeholder="Indica el motivo del rechazo..." />
							</div>
						</div>

						<div class="sideCard">
							<div class="sideCard__title">Fecha de la solicitud</div>
							<div class="sideCard__sub">Se usa para ordenar y filtrar correctamente.</div>

							<div class="field field--wide field--compact">
								<label>Fecha y hora *</label>
								<input v-model="form.createdAt" type="datetime-local" />
							</div>

							<div class="sideMeta">
								<div class="sideMeta__item">
									<span class="sideMeta__label">Vista previa</span>
									<span class="sideMeta__value">{{ formCreatedAtPreview }}</span>
								</div>
								<div class="sideMeta__item">
									<span class="sideMeta__label">Estado actual</span>
									<span class="statusBadge" :class="`statusBadge--${formStatusTone}`">{{ form.status }}</span>
								</div>
							</div>
						</div>
					</aside>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>
	.page {
		min-height: 100vh;
		padding: 24px 18px 90px;
		background: radial-gradient(980px 560px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(920px 560px at 85% 85%, rgba(179, 94, 255, 0.2), rgba(255, 255, 255, 0) 62%), linear-gradient(180deg, #f7f7ff 0%, #ece9ff 55%, #e9e7ff 100%);
		position: relative;
		overflow: hidden;
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, "Apple Color Emoji", "Segoe UI Emoji";
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
			opacity: 0.9;
		}

	.shell {
		max-width: 1320px;
		margin: 0 auto;
		border-radius: 24px;
		background: rgba(255, 255, 255, 0.56);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 28px 70px rgba(40, 55, 95, 0.12);
		backdrop-filter: blur(10px);
		padding: 20px 20px 22px;
	}

	.header {
		display: grid;
		grid-template-columns: 44px 1fr auto;
		gap: 16px;
		align-items: center;
	}

	.back {
		width: 44px;
		height: 44px;
		border-radius: 16px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.65);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		cursor: pointer;
		display: grid;
		place-items: center;
		color: rgba(90, 82, 160, 0.9);
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
		letter-spacing: -0.4px;
	}

	.titles p {
		margin: 4px 0 0;
		font-size: 13px;
		color: rgba(39, 46, 86, 0.62);
	}

	.headRight {
		display: flex;
		align-items: center;
		gap: 14px;
	}

	.newBtn {
		height: 44px;
		padding: 0 18px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(120, 105, 235, 0.92);
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 18px 34px rgba(88, 78, 212, 0.2);
		display: inline-flex;
		align-items: center;
		gap: 10px;
		white-space: nowrap;
	}

	.newBtn__icon svg {
		width: 16px;
		height: 16px;
	}

	.newBtn__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.roleCard {
		height: 44px;
		border-radius: 16px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.6);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 0 14px;
		min-width: 260px;
	}

	.roleAvatar {
		width: 34px;
		height: 34px;
		border-radius: 14px;
		background: linear-gradient(135deg, #7a67ff, #a053ff);
		color: #fff;
		font-weight: 900;
		display: grid;
		place-items: center;
		flex: 0 0 auto;
	}

	.roleLine {
		font-size: 13px;
		color: rgba(39, 46, 86, 0.82);
	}

	.roleSub {
		margin-top: 2px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.55);
	}

	.filtersSection {
		margin-top: 20px;
		display: grid;
		gap: 16px;
		padding: 18px 20px;
		border-radius: 20px;
		background: rgba(255, 255, 255, 0.42);
		border: 1px solid rgba(110, 102, 182, 0.08);
		box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.4);
	}

	.filtersLayout {
		display: grid;
		grid-template-columns: minmax(300px, 0.95fr) minmax(560px, 1.05fr);
		column-gap: 36px;
		row-gap: 16px;
		align-items: start;
	}

	.filtersSearch {
		min-width: 0;
		padding-right: 4px;
	}

	.filtersControls {
		display: grid;
		grid-template-columns: repeat(2, minmax(220px, 1fr));
		column-gap: 18px;
		row-gap: 14px;
		align-content: start;
	}

	.filterDropdown {
		position: relative;
		min-width: 0;
	}

	.pill {
		height: 50px;
		border-radius: 16px;
		background: rgba(255, 255, 255, 0.84);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.06);
		padding: 0 14px;
		display: flex;
		align-items: center;
		gap: 10px;
		color: rgba(39, 46, 86, 0.82);
		width: 100%;
		min-width: 0;
	}

	.pill--button {
		cursor: pointer;
	}

	.pill--search {
		height: 84px;
		padding: 0 18px;
		align-items: center;
		justify-content: flex-start;
	}

	.pill__icon {
		width: 34px;
		height: 34px;
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
		font-size: 14px;
		color: rgba(39, 46, 86, 0.86);
	}

		.pill__input::placeholder {
			color: rgba(39, 46, 86, 0.4);
		}

	.pill__txt {
		font-size: 13px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.78);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		min-width: 0;
		flex: 1 1 auto;
		text-align: left;
	}

	.pill__chev {
		margin-left: auto;
		color: rgba(39, 46, 86, 0.38);
		display: grid;
		place-items: center;
		flex: 0 0 auto;
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
		top: calc(100% + 12px);
		left: 0;
		z-index: 20;
		width: 100%;
		min-width: 260px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.96);
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 26px 56px rgba(40, 55, 95, 0.16);
		backdrop-filter: blur(12px);
		padding: 14px;
	}

	.dropdownPanel--status {
		min-width: 340px;
	}

	.dropdownPanel--date {
		min-width: 320px;
	}

	.dropdownHead {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 10px;
		margin-bottom: 12px;
	}

	.dropdownTitle {
		font-size: 13px;
		font-weight: 900;
		color: #232a52;
	}

	.dropdownClear {
		border: none;
		background: transparent;
		color: rgba(88, 78, 212, 0.95);
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		padding: 0;
	}

	.dropdownFoot {
		margin-top: 12px;
	}

	.dropdownFoot--hint {
		font-size: 11px;
		color: rgba(39, 46, 86, 0.5);
	}

	.statusGrid {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 10px;
	}

	.statusOption {
		height: 42px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.72);
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		transition: transform 0.16s ease, box-shadow 0.16s ease, border-color 0.16s ease;
	}

		.statusOption:hover,
		.optionItem:hover,
		.reasonBtn:hover,
		.clearFiltersBtn:hover,
		.btnGhost:hover,
		.iconAction:hover,
		.newBtn:hover,
		.statusFormOption:hover {
			transform: translateY(-1px);
		}

	.statusOption--active {
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.22), 0 14px 26px rgba(40, 55, 95, 0.08);
	}

	.statusOption--new,
	.statusFormOption--new {
		color: rgba(96, 73, 219, 0.98);
		background: rgba(150, 122, 255, 0.12);
	}

	.statusOption--progress,
	.statusFormOption--progress {
		color: rgba(180, 120, 30, 0.98);
		background: rgba(255, 197, 90, 0.16);
	}

	.statusOption--done,
	.statusFormOption--done {
		color: rgba(35, 130, 100, 0.98);
		background: rgba(60, 196, 151, 0.12);
	}

	.statusOption--closed,
	.statusFormOption--closed {
		color: rgba(72, 89, 125, 0.98);
		background: rgba(184, 194, 218, 0.18);
	}

	.statusOption--rejected,
	.statusFormOption--rejected {
		color: rgba(170, 40, 80, 0.98);
		background: rgba(255, 90, 130, 0.12);
	}

	.optionList {
		display: grid;
		gap: 8px;
		max-height: 260px;
		overflow-y: auto;
	}

	.optionItem {
		height: 40px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.76);
		font-size: 12px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.82);
		cursor: pointer;
		text-align: left;
		padding: 0 12px;
	}

	.optionItem--active {
		border-color: rgba(120, 105, 235, 0.45);
		color: rgba(88, 78, 212, 0.98);
		background: rgba(150, 122, 255, 0.1);
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.2);
	}

	.optionList--date {
		grid-template-columns: 1fr;
	}

	.dateRange {
		margin-top: 12px;
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 10px;
	}

	.dateField {
		display: grid;
		gap: 6px;
	}

		.dateField label {
			font-size: 11px;
			font-weight: 900;
			color: rgba(39, 46, 86, 0.72);
		}

	.dateFilter__input {
		height: 40px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.14);
		background: rgba(255, 255, 255, 0.76);
		padding: 0 10px;
		font-size: 12px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.82);
		outline: none;
	}

		.dateFilter__input:focus {
			border-color: rgba(120, 105, 235, 0.6);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.22);
		}

	.filtersBar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
		flex-wrap: wrap;
	}

	.filtersBar__left,
	.filtersBar__right {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-wrap: wrap;
	}

	.filterChip {
		height: 30px;
		padding: 0 12px;
		border-radius: 999px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		font-size: 12px;
		color: rgba(88, 78, 212, 0.95);
		background: rgba(150, 122, 255, 0.12);
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 10px 22px rgba(40, 55, 95, 0.05);
	}

	.filtersCounter {
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.58);
	}

	.clearFiltersBtn {
		height: 34px;
		padding: 0 12px;
		border-radius: 10px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.75);
		color: rgba(170, 40, 80, 0.95);
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 10px 22px rgba(40, 55, 95, 0.05);
	}

		.clearFiltersBtn:hover {
			background: rgba(255, 240, 246, 0.95);
		}

	.tablewrap {
		margin-top: 20px;
		border-radius: 20px;
		background: rgba(255, 255, 255, 0.6);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 22px 44px rgba(40, 55, 95, 0.12);
		overflow: auto;
		min-height: 0;
	}

	.table {
		width: 100%;
		border-collapse: separate;
		border-spacing: 0;
		min-width: 1340px;
		table-layout: fixed;
	}

	.col-index {
		width: 46px;
	}

	.col-number {
		width: 142px;
	}

	.col-subject {
		width: 190px;
	}

	.col-area {
		width: 122px;
	}

	.col-type {
		width: 138px;
	}

	.col-date {
		width: 108px;
	}

	.col-priority {
		width: 102px;
	}

	.col-status {
		width: 136px;
	}

	.col-actions {
		width: 330px;
	}

	thead th {
		padding: 16px 16px;
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.62);
		text-align: left;
		background: linear-gradient(180deg, rgba(240, 236, 255, 0.7), rgba(255, 255, 255, 0.4));
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
		white-space: nowrap;
	}

	tbody td {
		padding: 12px 16px;
		font-size: 13px;
		color: rgba(39, 46, 86, 0.82);
		border-bottom: 1px solid rgba(110, 102, 182, 0.08);
		background: rgba(255, 255, 255, 0.36);
		vertical-align: middle;
		line-height: 1.28;
	}

	.requestNumberCell {
		font-weight: 800;
		color: rgba(73, 83, 136, 0.95);
	}

	.subjectCell {
		word-break: break-word;
	}

	.priorityCell {
		padding-right: 16px;
	}

	.statusColCell {
		padding-left: 14px;
		padding-right: 14px;
	}

	.actionsCell {
		min-width: 330px;
		padding-left: 12px;
	}

	.rowActions {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-wrap: wrap;
	}

	.iconAction {
		height: 34px;
		padding: 0 10px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.14);
		background: rgba(255, 255, 255, 0.74);
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 12px 24px rgba(40, 55, 95, 0.06);
	}

		.iconAction svg {
			width: 14px;
			height: 14px;
		}

		.iconAction path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.iconAction--view {
		color: rgba(60, 110, 220, 0.95);
	}

	.iconAction--take {
		color: rgba(35, 130, 100, 0.98);
	}

	.iconAction--edit {
		color: rgba(88, 78, 212, 0.95);
	}

	.iconAction--delete {
		color: rgba(170, 40, 80, 0.95);
	}

	.iconAction:disabled {
		opacity: 0.6;
		cursor: not-allowed;
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
		border: 1px solid rgba(110, 102, 182, 0.1);
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

	.statusCell {
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 6px;
		min-width: 0;
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
		border: 1px solid transparent;
		width: fit-content;
		background: rgba(255, 255, 255, 0.8);
		white-space: nowrap;
	}

	.statusBadge--new {
		background: rgba(150, 122, 255, 0.14);
		color: rgba(96, 73, 219, 0.98);
		border-color: rgba(150, 122, 255, 0.18);
	}

	.statusBadge--progress {
		background: rgba(255, 197, 90, 0.18);
		color: rgba(180, 120, 30, 0.98);
		border-color: rgba(255, 197, 90, 0.22);
	}

	.statusBadge--done {
		background: rgba(60, 196, 151, 0.14);
		color: rgba(35, 130, 100, 0.98);
		border-color: rgba(60, 196, 151, 0.18);
	}

	.statusBadge--closed {
		background: rgba(184, 194, 218, 0.2);
		color: rgba(72, 89, 125, 0.98);
		border-color: rgba(184, 194, 218, 0.22);
	}

	.statusBadge--rejected {
		background: rgba(255, 90, 130, 0.14);
		color: rgba(170, 40, 80, 0.98);
		border-color: rgba(255, 90, 130, 0.18);
	}

	.reasonBtn {
		height: 28px;
		padding: 0 10px;
		border-radius: 10px;
		border: 1px solid rgba(255, 90, 130, 0.2);
		background: rgba(255, 245, 248, 0.96);
		color: rgba(170, 40, 80, 0.98);
		font-size: 11px;
		font-weight: 900;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		gap: 6px;
		width: fit-content;
	}

		.reasonBtn svg {
			width: 13px;
			height: 13px;
		}

		.reasonBtn path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.empty {
		height: 420px;
		display: grid;
		place-items: center;
		text-align: center;
		padding: 40px 18px;
		color: rgba(39, 46, 86, 0.72);
		position: relative;
	}

	.empty__circle {
		width: 74px;
		height: 74px;
		border-radius: 999px;
		background: rgba(120, 105, 235, 0.14);
		color: rgba(120, 105, 235, 0.9);
		display: grid;
		place-items: center;
		font-size: 40px;
		font-weight: 600;
		box-shadow: 0 18px 34px rgba(88, 78, 212, 0.12);
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
		color: rgba(39, 46, 86, 0.55);
		line-height: 1.45;
	}

	.chips {
		margin-top: 18px;
		display: flex;
		gap: 14px;
		justify-content: center;
		flex-wrap: wrap;
	}

	.chip {
		height: 30px;
		padding: 0 18px;
		border-radius: 999px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		font-size: 13px;
		border: 1px solid rgba(110, 102, 182, 0.1);
	}

	.chip--new {
		background: rgba(150, 122, 255, 0.18);
		color: rgba(120, 105, 235, 0.95);
	}

	.chip--proc {
		background: rgba(255, 197, 90, 0.2);
		color: rgba(180, 120, 30, 0.95);
	}

	.chip--done {
		background: rgba(60, 196, 151, 0.14);
		color: rgba(35, 130, 100, 0.95);
	}

	.chip--closed {
		background: rgba(184, 194, 218, 0.2);
		color: rgba(72, 89, 125, 0.95);
	}

	.chip--rejected {
		background: rgba(255, 90, 130, 0.12);
		color: rgba(170, 40, 80, 0.95);
	}

	.foot {
		margin-top: 18px;
		text-align: center;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.7);
	}

	.foot__sub {
		margin-top: 6px;
		font-size: 12px;
		font-weight: 700;
		color: rgba(39, 46, 86, 0.45);
	}

	.help {
		position: fixed;
		right: 18px;
		bottom: 18px;
		width: 44px;
		height: 44px;
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
		max-width: 1120px;
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
		padding: 12px 16px 10px;
		border-bottom: 1px solid rgba(110, 102, 182, 0.08);
	}

	.modal__left {
		display: flex;
		gap: 10px;
		align-items: flex-start;
	}

	.modalBack {
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

		.modalBack svg {
			width: 16px;
			height: 16px;
		}

		.modalBack path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.modal__head h2 {
		margin: 0;
		font-size: 16px;
		font-weight: 900;
		color: #232a52;
	}

	.modal__head p {
		margin: 4px 0 0;
		font-size: 11px;
		color: rgba(39, 46, 86, 0.55);
	}

	.modal__body {
		padding: 14px;
		display: grid;
		gap: 12px;
		overflow: auto;
	}

	.modal__body--layout {
		grid-template-columns: minmax(0, 1.7fr) minmax(280px, 0.92fr);
		align-items: start;
	}

	.modalMain,
	.modalSide {
		display: grid;
		gap: 12px;
		align-content: start;
	}

	.modalSide {
		position: sticky;
		top: 0;
	}

	.formCard,
	.sideCard {
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.72);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 14px 30px rgba(40, 55, 95, 0.05);
		padding: 14px;
		display: grid;
		gap: 12px;
	}

	.formCard__header,
	.sideCard__header {
		display: grid;
		gap: 3px;
	}

	.formCard__title,
	.sideCard__title {
		font-size: 14px;
		font-weight: 900;
		color: #232a52;
	}

	.formCard__sub,
	.sideCard__sub {
		font-size: 12px;
		color: rgba(39, 46, 86, 0.55);
	}

	.statusFormGrid {
		display: grid;
		grid-template-columns: 1fr;
		gap: 8px;
	}

	.statusFormOption {
		min-height: 40px;
		padding: 0 12px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		font-size: 12px;
		font-weight: 900;
		cursor: pointer;
		text-align: left;
	}

	.statusFormOption--active {
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.2);
		border-color: rgba(120, 105, 235, 0.45);
	}

	.sideMeta {
		display: grid;
		gap: 10px;
	}

	.sideMeta__item {
		display: grid;
		gap: 6px;
	}

	.sideMeta__label {
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.56);
	}

	.sideMeta__value {
		font-size: 13px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.86);
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

	.msg--ok {
		background: rgba(60, 196, 151, 0.12);
		color: rgba(35, 130, 100, 0.95);
		border: 1px solid rgba(60, 196, 151, 0.18);
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
		-webkit-appearance: none;
		-moz-appearance: none;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24'%3E%3Cpath d='M7 10l5 5 5-5' fill='none' stroke='%23706f8f' stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
		background-repeat: no-repeat;
		background-position: right 12px center;
		background-size: 16px;
		box-shadow: 0 10px 24px rgba(40, 55, 95, 0.05);
	}

		.selectNative:focus {
			border-color: rgba(120, 105, 235, 0.6);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.24);
		}

		.selectNative:disabled {
			opacity: 0.65;
			cursor: not-allowed;
			background-color: rgba(245, 245, 250, 0.9);
		}

		.selectNative option {
			color: #232a52;
			background: #ffffff;
		}

	.helper {
		font-size: 10px;
		color: rgba(39, 46, 86, 0.42);
		min-height: 12px;
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
		min-height: 88px;
		max-height: 180px;
	}

		.field input:focus,
		.field textarea:focus {
			border-color: rgba(120, 105, 235, 0.55);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.28);
		}

		.field input::placeholder,
		.field textarea::placeholder {
			color: rgba(39, 46, 86, 0.42);
		}

	.field--compact {
		margin-top: 0;
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

	.drop--drag {
		border-color: rgba(120, 105, 235, 0.65);
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.28);
	}

	.drop--disabled {
		opacity: 0.75;
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

	@media (max-width: 1220px) {
		.filtersLayout {
			grid-template-columns: 1fr;
			gap: 18px;
		}

		.filtersSearch {
			padding-right: 0;
		}

		.modal__body--layout {
			grid-template-columns: 1fr;
		}

		.modalSide {
			position: static;
		}

		.pill--search {
			height: 72px;
		}
	}

	@media (max-width: 1100px) {
		.header {
			grid-template-columns: 44px 1fr;
		}

		.headRight {
			grid-column: 1 / -1;
			justify-content: flex-end;
		}
	}

	@media (max-width: 980px) {
		.filtersControls {
			grid-template-columns: 1fr 1fr;
			column-gap: 14px;
			row-gap: 14px;
		}

		.dropdownPanel,
		.dropdownPanel--status,
		.dropdownPanel--date {
			width: 100%;
			min-width: 0;
		}

		.dateRange,
		.row2,
		.prio,
		.actions {
			grid-template-columns: 1fr;
		}
	}

	@media (max-width: 760px) {
		.filtersControls {
			grid-template-columns: 1fr;
			gap: 12px;
		}

		.filtersSection {
			padding: 14px;
		}

		.pill--search {
			height: 64px;
		}

		tbody td,
		thead th {
			padding-left: 14px;
			padding-right: 14px;
		}
	}

	@media (max-width: 640px) {
		.roleCard {
			display: none;
		}

		.modal {
			max-width: 440px;
			max-height: calc(100vh - 24px);
		}

		.statusGrid {
			grid-template-columns: 1fr;
		}

		.table {
			min-width: 1220px;
		}
	}
</style>
