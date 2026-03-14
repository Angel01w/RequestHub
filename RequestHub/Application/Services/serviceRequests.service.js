const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")

function toArray(payload) {
	if (Array.isArray(payload)) return payload
	if (Array.isArray(payload?.items)) return payload.items
	if (Array.isArray(payload?.data)) return payload.data
	if (Array.isArray(payload?.requests)) return payload.requests
	return []
}

function toNumber(value) {
	const parsed = Number(value)
	return Number.isFinite(parsed) ? parsed : null
}

function normalizeText(value) {
	return String(value ?? "").trim()
}

function normalizeLower(value) {
	return normalizeText(value).toLowerCase()
}

function firstNonEmpty(...values) {
	for (const value of values) {
		const text = normalizeText(value)
		if (text) return text
	}
	return ""
}

function normalizeDateValue(value) {
	if (!value) return null

	if (value instanceof Date) {
		return Number.isNaN(value.getTime()) ? null : value
	}

	if (typeof value === "string") {
		const trimmed = value.trim()
		if (!trimmed) return null

		const normalized = /^\d{4}-\d{2}-\d{2}$/.test(trimmed)
			? `${trimmed}T00:00:00`
			: trimmed

		const parsed = new Date(normalized)
		return Number.isNaN(parsed.getTime()) ? null : parsed
	}

	const parsed = new Date(value)
	return Number.isNaN(parsed.getTime()) ? null : parsed
}

function formatDate(value) {
	const date = normalizeDateValue(value)
	if (!date) return ""
	return new Intl.DateTimeFormat("es-DO", {
		day: "2-digit",
		month: "2-digit",
		year: "numeric"
	}).format(date)
}

function normalizeStatusName(raw) {
	const value = normalizeLower(raw)

	if (!value) return "Nueva"

	if (
		value === "nueva" ||
		value === "nuevo" ||
		value === "new"
	) {
		return "Nueva"
	}

	if (
		value === "en proceso" ||
		value === "en-proceso" ||
		value === "enproceso" ||
		value === "in progress" ||
		value === "in_progress" ||
		value === "inprogress" ||
		value === "proceso"
	) {
		return "En Proceso"
	}

	if (
		value === "resuelta" ||
		value === "resuelto" ||
		value === "resolved" ||
		value === "completada" ||
		value === "completado" ||
		value === "cerrada" ||
		value === "cerrado"
	) {
		return "Resuelta"
	}

	return firstNonEmpty(raw, "Nueva")
}

function buildStatusKey(statusName) {
	const value = normalizeLower(statusName)

	if (value === "nueva") return "new"
	if (value === "en proceso") return "in-progress"
	if (value === "resuelta") return "resolved"

	return value || "unknown"
}

export function normalizeServiceRequest(item = {}) {
	const id = toNumber(
		item.id ??
		item.requestId ??
		item.serviceRequestId
	)

	const requestNumber = firstNonEmpty(
		item.requestNumber,
		item.number,
		item.code,
		item.requestCode,
		id ? `SR-${id}` : ""
	)

	const subject = firstNonEmpty(
		item.subject,
		item.title,
		item.asunto
	)

	const areaName = firstNonEmpty(
		item.areaName,
		item.area?.name,
		item.area,
		item.areaNombre
	)

	const priorityName = firstNonEmpty(
		item.priorityName,
		item.priority?.name,
		item.priority,
		item.priorityLabel,
		item.prioridad
	)

	const statusRaw = firstNonEmpty(
		item.statusName,
		item.status?.name,
		item.status,
		item.estado
	)

	const statusName = normalizeStatusName(statusRaw)
	const statusKey = buildStatusKey(statusName)

	const createdAtRaw =
		item.createdAt ??
		item.createdAtUtc ??
		item.creationDate ??
		item.createdDate ??
		item.createdOn ??
		item.createdDateUtc ??
		item.requestDate ??
		item.dateCreated ??
		item.submittedAt ??
		item.date ??
		item.fechaCreacion ??
		item.fecha

	const createdAt = normalizeDateValue(createdAtRaw)
	const createdAtLabel = formatDate(createdAtRaw)

	return {
		id,
		requestNumber,
		subject,
		areaName,
		priorityName,
		statusName,
		statusKey,
		createdAt,
		createdAtLabel,
		raw: item
	}
}

export async function fetchServiceRequests(filter = {}) {
	const query = new URLSearchParams()

	if (filter.search) query.set("search", filter.search)
	if (filter.statusId) query.set("statusId", String(filter.statusId))
	if (filter.areaId) query.set("areaId", String(filter.areaId))
	if (filter.dateFrom) query.set("dateFrom", filter.dateFrom)
	if (filter.dateTo) query.set("dateTo", filter.dateTo)

	const url = `${API_BASE}/api/ServiceRequests${query.toString() ? `?${query.toString()}` : ""}`

	const response = await fetch(url, {
		method: "GET",
		headers: {
			Accept: "application/json"
		}
	})

	if (!response.ok) {
		let message = "No se pudieron cargar las solicitudes."
		try {
			const errorPayload = await response.json()
			message = firstNonEmpty(
				errorPayload?.message,
				errorPayload?.title,
				errorPayload?.error,
				message
			)
		} catch {
			message = response.statusText || message
		}
		throw new Error(message)
	}

	const payload = await response.json()
	return toArray(payload).map(normalizeServiceRequest)
}

export function buildUniqueAreaOptions(requests = []) {
	const map = new Map()

	for (const request of requests) {
		const key = normalizeLower(request.areaName)
		if (!key) continue
		if (!map.has(key)) {
			map.set(key, request.areaName)
		}
	}

	return Array.from(map.entries())
		.map(([value, label]) => ({ value, label }))
		.sort((a, b) => a.label.localeCompare(b.label, "es"))
}

export function buildUniqueStatusOptions(requests = []) {
	const map = new Map()

	for (const request of requests) {
		const key = normalizeLower(request.statusName)
		if (!key) continue
		if (!map.has(key)) {
			map.set(key, request.statusName)
		}
	}

	return Array.from(map.entries())
		.map(([value, label]) => ({ value, label }))
		.sort((a, b) => a.label.localeCompare(b.label, "es"))
}

export function filterServiceRequests(requests = [], filters = {}) {
	const search = normalizeLower(filters.search)
	const status = normalizeLower(filters.status)
	const area = normalizeLower(filters.area)
	const date = normalizeText(filters.date)

	return requests.filter(request => {
		const matchesSearch =
			!search ||
			normalizeLower(request.requestNumber).includes(search) ||
			normalizeLower(request.subject).includes(search)

		const matchesStatus =
			!status ||
			normalizeLower(request.statusName) === status

		const matchesArea =
			!area ||
			normalizeLower(request.areaName) === area

		const matchesDate =
			!date ||
			(request.createdAt
				? request.createdAt.toISOString().slice(0, 10) === date
				: false)

		return matchesSearch && matchesStatus && matchesArea && matchesDate
	})
}