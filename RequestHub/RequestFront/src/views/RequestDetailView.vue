<script setup>
	import { computed, onMounted, ref } from "vue"
	import { useRoute, useRouter } from "vue-router"
	import { useAuthStore } from "../stores/auth"

	const route = useRoute()
	const router = useRouter()
	const auth = useAuthStore()

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "https://localhost:7299").replace(/\/+$/, "")

	const isLoading = ref(false)
	const loadError = ref("")
	const request = ref(null)

	const newComment = ref("")
	const isSubmittingComment = ref(false)
	const commentError = ref("")

	const editingCommentId = ref(null)
	const editingCommentText = ref("")
	const isUpdatingComment = ref(false)
	const isDeletingCommentId = ref(null)

	const requestId = computed(() => route.params.id)

	function getStoredUser() {
		try {
			return JSON.parse(localStorage.getItem("rh_user") || localStorage.getItem("user") || "null")
		} catch {
			return null
		}
	}

	function getToken() {
		return auth.token || localStorage.getItem("rh_token") || localStorage.getItem("token") || ""
	}

	const currentUser = computed(() => auth.user || getStoredUser() || null)

	const currentUserId = computed(() => {
		const raw =
			currentUser.value?.id ??
			currentUser.value?.userId ??
			currentUser.value?.Id ??
			currentUser.value?.UserId ??
			null

		const parsed = Number(raw)
		return Number.isFinite(parsed) && parsed > 0 ? parsed : null
	})

	const currentRole = computed(() =>
		String(auth.user?.role || auth.role || getStoredUser()?.role || "").trim().toLowerCase()
	)

	const canAccessDetail = computed(() =>
		["superadmin", "admin", "gestor", "solicitante"].includes(currentRole.value)
	)

	const requestTitle = computed(() => {
		const subject = cleanDisplayText(request.value?.subject || "")
		return subject || "Solicitud"
	})

	const requestNumber = computed(() => {
		return cleanDisplayText(request.value?.requestNumber || request.value?.number || "")
	})

	const requestStatus = computed(() => {
		return canonicalStatusLabel(request.value?.status || request.value?.statusName || "")
	})

	const statusTone = computed(() => resolveStatusTone(requestStatus.value))

	const requestDescription = computed(() => {
		return cleanDisplayText(request.value?.description || "")
	})

	const requestComments = computed(() => {
		return Array.isArray(request.value?.comments) ? request.value.comments : []
	})

	const attachmentName = computed(() => {
		const raw =
			request.value?.attachmentName ||
			request.value?.fileName ||
			request.value?.originalFileName ||
			request.value?.attachmentStoredFileName ||
			extractFileNameFromPath(request.value?.attachmentPath || request.value?.attachmentUrl || "") ||
			""
		return cleanDisplayText(raw)
	})

	const attachmentUrl = computed(() => {
		return buildAttachmentUrl(
			request.value?.attachmentUrl ||
			request.value?.attachmentPath ||
			request.value?.fileUrl ||
			request.value?.filePath ||
			""
		)
	})

	const hasAttachment = computed(() => {
		return !!attachmentUrl.value
	})

	const attachmentExtension = computed(() => {
		const source = `${attachmentName.value} ${request.value?.attachmentPath || ""} ${attachmentUrl.value}`.toLowerCase()
		const match = source.match(/\.([a-z0-9]+)(?:[\?#].*)?$/i)
		return match?.[1] || ""
	})

	const isImageAttachment = computed(() => {
		return ["jpg", "jpeg", "png", "gif", "webp", "bmp", "svg"].includes(attachmentExtension.value)
	})

	const isPdfAttachment = computed(() => {
		return attachmentExtension.value === "pdf"
	})

	const metadataItems = computed(() => {
		const items = [
			{
				key: "area",
				label: "Área",
				value: cleanDisplayText(request.value?.areaName || request.value?.area)
			},
			{
				key: "type",
				label: "Tipo",
				value: cleanDisplayText(request.value?.typeName || request.value?.requestType)
			},
			{
				key: "priority",
				label: "Prioridad",
				value: cleanDisplayText(request.value?.priority)
			},
			{
				key: "date",
				label: "Fecha de Creación",
				value: formatDateTime(request.value?.createdAt)
			},
			{
				key: "createdBy",
				label: "Creado por",
				value: cleanDisplayText(request.value?.createdByName || request.value?.createdBy || request.value?.createdByUserName)
			},
			{
				key: "assignedTo",
				label: "Asignado a",
				value: cleanDisplayText(request.value?.assignedToName || request.value?.assignedTo || request.value?.assignedUserName)
			}
		]

		return items.filter(item => item.value)
	})

	function cleanDisplayText(value) {
		return String(value ?? "")
			.normalize("NFC")
			.replace(/\uFFFD/g, "")
			.replace(/[^\S\r\n]+/g, " ")
			.trim()
	}

	function cleanInputText(value) {
		return String(value ?? "").trim()
	}

	function normalizeStatusKey(value) {
		return cleanDisplayText(value).toLowerCase().replace(/\s+/g, "")
	}

	function canonicalStatusLabel(value) {
		const normalized = normalizeStatusKey(value)
		if (normalized === "nueva") return "Nueva"
		if (normalized === "enproceso") return "En Proceso"
		if (normalized === "resuelta") return "Resuelta"
		if (normalized === "cerrada") return "Cerrada"
		if (normalized === "rechazada") return "Rechazada"
		return cleanDisplayText(value)
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

	function normalizeComment(item, index) {
		return {
			id: item?.id ?? `comment-${index}`,
			text: cleanDisplayText(item?.text),
			createdAt: item?.createdAtUtc ?? item?.createdAt ?? item?.date ?? null,
			userName: cleanDisplayText(item?.userName || item?.user || item?.author || item?.createdByName)
		}
	}

	function extractFileNameFromPath(value) {
		const path = cleanDisplayText(value)
		if (!path) return ""
		const noQuery = path.split("?")[0].split("#")[0]
		const segments = noQuery.split("/").filter(Boolean)
		return decodeURIComponent(segments[segments.length - 1] || "")
	}

	function ensureLeadingSlash(value) {
		const normalized = cleanDisplayText(value).replace(/\\/g, "/")
		if (!normalized) return ""
		return normalized.startsWith("/") ? normalized : `/${normalized}`
	}

	function normalizeAttachmentPath(value) {
		const normalized = ensureLeadingSlash(value)
		if (!normalized) return ""
		if (/^\/https?:\/\//i.test(normalized)) {
			return normalized.replace(/^\//, "")
		}
		return normalized
	}

	function buildAttachmentUrl(rawValue) {
		const raw = cleanDisplayText(rawValue)
		if (!raw) return ""

		if (/^https?:\/\//i.test(raw)) {
			return raw
		}

		const normalized = normalizeAttachmentPath(raw)
		if (!normalized) return ""

		if (/^https?:\/\//i.test(normalized)) {
			return normalized
		}

		return `${API_BASE}${normalized}`
	}

	function normalizeRequest(raw) {
		if (!raw || typeof raw !== "object") return null

		const subject = cleanDisplayText(raw.subject || raw.Subject)
		const number = cleanDisplayText(raw.number || raw.requestNumber || raw.Number)
		const areaName = cleanDisplayText(raw.area || raw.Area || raw.areaName)
		const typeName = cleanDisplayText(raw.requestType || raw.RequestType || raw.typeName)
		const priority = cleanDisplayText(raw.priority || raw.Priority)
		const assignedToName = cleanDisplayText(raw.assignedToName || raw.assignedTo || raw.assignedUserName || raw.assignedUser)
		const createdByName = cleanDisplayText(raw.createdByName || raw.createdBy || raw.createdByUserName || raw.createdByUser)
		const commentsRaw = Array.isArray(raw.comments) ? raw.comments : Array.isArray(raw.Comments) ? raw.Comments : []

		const attachmentPath = normalizeAttachmentPath(
			raw.attachmentPath ||
			raw.AttachmentPath ||
			raw.filePath ||
			raw.FilePath ||
			raw.fileUrl ||
			raw.FileUrl ||
			raw.attachmentUrl ||
			raw.AttachmentUrl
		)

		const normalizedAttachmentName = cleanDisplayText(
			raw.attachmentName ||
			raw.AttachmentName ||
			raw.fileName ||
			raw.FileName ||
			raw.originalFileName ||
			raw.OriginalFileName ||
			raw.attachmentStoredFileName ||
			raw.AttachmentStoredFileName ||
			extractFileNameFromPath(attachmentPath)
		)

		const assignedToUserIdRaw =
			raw.assignedToUserId ??
			raw.AssignedToUserId ??
			null

		const createdByUserIdRaw =
			raw.createdByUserId ??
			raw.CreatedByUserId ??
			null

		const assignedToUserIdParsed = Number(assignedToUserIdRaw)
		const createdByUserIdParsed = Number(createdByUserIdRaw)

		const assignedToUserId = Number.isFinite(assignedToUserIdParsed) && assignedToUserIdParsed > 0
			? assignedToUserIdParsed
			: null

		const createdByUserId = Number.isFinite(createdByUserIdParsed) && createdByUserIdParsed > 0
			? createdByUserIdParsed
			: null

		const canTake = Boolean(raw.canTake ?? raw.CanTake ?? false)
		const canEdit = Boolean(raw.canEdit ?? raw.CanEdit ?? false)
		const canDelete = Boolean(raw.canDelete ?? raw.CanDelete ?? false)
		const canComment = Boolean(raw.canComment ?? raw.CanComment ?? false)
		const canChangeStatus = Boolean(raw.canChangeStatus ?? raw.CanChangeStatus ?? false)
		const canClose = Boolean(raw.canClose ?? raw.CanClose ?? false)

		return {
			id: raw.id ?? raw.Id ?? raw.serviceRequestId ?? raw.requestId ?? null,
			subject,
			number,
			requestNumber: number,
			status: canonicalStatusLabel(raw.status || raw.Status || raw.statusName || raw.StatusName),
			statusId: raw.statusId ?? raw.StatusId ?? null,
			description: cleanDisplayText(raw.description || raw.Description),
			areaName,
			area: areaName,
			typeName,
			requestType: typeName,
			priority,
			createdAt: raw.createdAtUtc ?? raw.createdAt ?? raw.CreatedAtUtc ?? raw.CreatedAt ?? null,
			createdByName,
			createdByUserId,
			assignedToName,
			assignedToUserId,
			isTaken: assignedToUserId !== null,
			isTakenByCurrentUser: assignedToUserId !== null && currentUserId.value !== null && assignedToUserId === currentUserId.value,
			attachmentPath,
			attachmentUrl: buildAttachmentUrl(attachmentPath),
			attachmentName: normalizedAttachmentName,
			canTake,
			canEdit,
			canDelete,
			canComment,
			canChangeStatus,
			canClose,
			comments: commentsRaw.map(normalizeComment)
		}
	}

	function getMetaIcon(key) {
		switch (key) {
			case "area":
				return "M4 6.5h16v11H4zM2.5 18.5h19M8 21.5h8M9 10.5h6M7.5 3.5h9"
			case "type":
				return "M6 4.5h9l3 3v12H6zM15 4.5v3h3"
			case "priority":
				return "M12 8v5M12 16.5h.01M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z"
			case "date":
				return "M7 3.5v3M17 3.5v3M4 8.5h16M5 6.5h14v14H5z"
			case "createdBy":
			case "assignedTo":
				return "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 8a7 7 0 0 1 14 0"
			default:
				return "M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z"
		}
	}

	function iconPath(name) {
		switch (name) {
			case "arrowLeft":
				return "M15 18l-6-6 6-6"
			case "pulse":
				return "M3 12h4l2-4 4 10 3-6h5"
			case "comment":
				return "M5 6.5h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H10l-5 3v-3H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2Z"
			case "send":
				return "M21 3 3 11l7 2 2 7 9-17ZM10 13l11-10"
			case "clock":
				return "M12 7v5l3 2M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z"
			case "edit":
				return "M4 20h4l10.5-10.5a2.121 2.121 0 0 0-3-3L5 17v3Z"
			case "trash":
				return "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"
			case "save":
				return "M5 4.5h11l3 3v12H5zM8 4.5v5h8v-5M9 19.5v-6h6v6"
			case "close":
				return "M6 6l12 12M18 6 6 18"
			case "attachment":
				return "M9 12.5 14.5 7a3.182 3.182 0 1 1 4.5 4.5l-7.5 7.5a5 5 0 1 1-7.07-7.07l8-8"
			case "download":
				return "M12 4v11M7.5 10.5 12 15l4.5-4.5M5 19.5h14"
			case "open":
				return "M14 5.5h5v5M19 5.5l-8.5 8.5M18.5 13.5v5h-13v-13h5"
			case "file":
				return "M6 4.5h9l3 3v12H6zM15 4.5v3h3"
			default:
				return ""
		}
	}

	function getInitials(name) {
		const parts = cleanDisplayText(name).split(/\s+/).filter(Boolean)
		if (!parts.length) return "?"
		return parts.slice(0, 2).map(x => x[0]?.toUpperCase() || "").join("")
	}

	function formatDateTime(value) {
		if (!value) return ""
		const date = new Date(value)
		if (Number.isNaN(date.getTime())) return ""
		return new Intl.DateTimeFormat("es-DO", {
			day: "numeric",
			month: "long",
			year: "numeric",
			hour: "2-digit",
			minute: "2-digit"
		}).format(date)
	}

	async function apiRequest(url, options = {}) {
		const token = getToken()

		const headers = {
			Accept: "application/json",
			...(options.headers || {})
		}

		if (token) {
			headers.Authorization = `Bearer ${token}`
		}

		const response = await fetch(url, {
			...options,
			headers
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
			if (response.status === 401) {
				auth.logout()
				router.replace("/login")
			}

			const message =
				payload?.message ||
				payload?.title ||
				(typeof payload === "string" ? payload : "") ||
				`HTTP ${response.status}`

			const error = new Error(message)
			error.status = response.status
			throw error
		}

		return payload
	}

	async function loadRequestDetail() {
		isLoading.value = true
		loadError.value = ""

		try {
			const data = await apiRequest(`${API_BASE}/api/ServiceRequests/${requestId.value}`)
			request.value = normalizeRequest(data)
		} catch (error) {
			request.value = null
			loadError.value = error?.message || "No se pudo cargar la solicitud."
		} finally {
			isLoading.value = false
		}
	}

	async function submitComment() {
		commentError.value = ""
		const text = cleanInputText(newComment.value)
		if (!text) return

		isSubmittingComment.value = true

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${requestId.value}/comments`, {
				method: "POST",
				headers: {
					"Content-Type": "application/json"
				},
				body: JSON.stringify({ text })
			})

			newComment.value = ""
			await loadRequestDetail()
		} catch (error) {
			commentError.value = error?.message || "No se pudo agregar el comentario."
		} finally {
			isSubmittingComment.value = false
		}
	}

	function startEditingComment(comment) {
		editingCommentId.value = comment.id
		editingCommentText.value = comment.text
		commentError.value = ""
	}

	function cancelEditingComment() {
		editingCommentId.value = null
		editingCommentText.value = ""
	}

	async function saveEditedComment(comment) {
		commentError.value = ""
		const text = cleanInputText(editingCommentText.value)

		if (!text) {
			commentError.value = "El comentario no puede estar vacío."
			return
		}

		isUpdatingComment.value = true

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${requestId.value}/comments/${comment.id}`, {
				method: "PUT",
				headers: {
					"Content-Type": "application/json"
				},
				body: JSON.stringify({ text })
			})

			cancelEditingComment()
			await loadRequestDetail()
		} catch (error) {
			if (error?.status === 404) {
				commentError.value = "El backend aún no tiene habilitado editar comentarios."
			} else {
				commentError.value = error?.message || "No se pudo editar el comentario."
			}
		} finally {
			isUpdatingComment.value = false
		}
	}

	async function deleteComment(comment) {
		const confirmed = window.confirm("¿Seguro que deseas eliminar este comentario?")
		if (!confirmed) return

		commentError.value = ""
		isDeletingCommentId.value = comment.id

		try {
			await apiRequest(`${API_BASE}/api/ServiceRequests/${requestId.value}/comments/${comment.id}`, {
				method: "DELETE"
			})

			if (editingCommentId.value === comment.id) {
				cancelEditingComment()
			}

			await loadRequestDetail()
		} catch (error) {
			if (error?.status === 404) {
				commentError.value = "El backend aún no tiene habilitado eliminar comentarios."
			} else {
				commentError.value = error?.message || "No se pudo eliminar el comentario."
			}
		} finally {
			isDeletingCommentId.value = null
		}
	}

	function onBack() {
		router.back()
	}

	onMounted(async () => {
		if (!getToken()) {
			router.replace("/login")
			return
		}

		if (!canAccessDetail.value) {
			router.replace("/login")
			return
		}

		await loadRequestDetail()
	})
</script>

<template>
	<div class="detail-page">
		<section class="detail-shell">
			<header class="detail-header">
				<div class="detail-header__left">
					<button class="back-btn" type="button" aria-label="Volver" @click="onBack">
						<svg viewBox="0 0 24 24" aria-hidden="true">
							<path :d="iconPath('arrowLeft')" />
						</svg>
					</button>

					<div class="detail-heading">
						<h1>{{ requestTitle }}</h1>
						<p v-if="requestNumber">{{ requestNumber }}</p>
					</div>
				</div>

				<div v-if="requestStatus" class="status-chip" :class="`status-chip--${statusTone}`">
					<svg viewBox="0 0 24 24" aria-hidden="true">
						<path :d="iconPath('clock')" />
					</svg>
					<span>{{ requestStatus }}</span>
				</div>
			</header>

			<div v-if="isLoading" class="state-card">
				<div class="state-title">Cargando solicitud...</div>
			</div>

			<div v-else-if="loadError" class="state-card state-card--error">
				<div class="state-title">No se pudo cargar la solicitud</div>
				<div class="state-text">{{ loadError }}</div>
			</div>

			<div v-else-if="request" class="detail-layout">
				<div class="detail-main">
					<section class="glass-card info-card">
						<div class="card-title">
							<span class="card-title__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path :d="iconPath('pulse')" />
								</svg>
							</span>
							<h2>Detalles de la Solicitud</h2>
						</div>

						<div v-if="requestDescription" class="detail-copy">
							<div class="detail-copy__label">Descripción</div>
							<p>{{ requestDescription }}</p>
						</div>

						<div v-else class="empty-inline">
							No hay descripción disponible.
						</div>
					</section>

					<section class="glass-card attachment-card">
						<div class="card-title">
							<span class="card-title__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path :d="iconPath('attachment')" />
								</svg>
							</span>
							<h2>Archivo adjunto</h2>
						</div>

						<div v-if="hasAttachment" class="attachment-block">
							<div class="attachment-toolbar">
								<div class="attachment-file">
									<div class="attachment-file__icon">
										<svg viewBox="0 0 24 24">
											<path :d="iconPath('file')" />
										</svg>
									</div>
									<div class="attachment-file__content">
										<div class="attachment-file__label">Archivo cargado</div>
										<div class="attachment-file__name">
											{{ attachmentName || "Adjunto disponible" }}
										</div>
										<div class="attachment-file__path">
											{{ attachmentUrl }}
										</div>
									</div>
								</div>

								<div class="attachment-actions">
									<a class="attachment-action"
									   :href="attachmentUrl"
									   target="_blank"
									   rel="noopener noreferrer">
										<svg viewBox="0 0 24 24" aria-hidden="true">
											<path :d="iconPath('open')" />
										</svg>
										<span>Abrir</span>
									</a>

									<a class="attachment-action attachment-action--ghost"
									   :href="attachmentUrl"
									   :download="attachmentName || 'archivo.pdf'">
										<svg viewBox="0 0 24 24" aria-hidden="true">
											<path :d="iconPath('download')" />
										</svg>
										<span>Descargar</span>
									</a>
								</div>
							</div>

							<div v-if="isImageAttachment" class="attachment-preview attachment-preview--image">
								<img :src="attachmentUrl" :alt="attachmentName || 'Archivo adjunto'" />
							</div>

							<div v-else-if="isPdfAttachment" class="attachment-preview attachment-preview--pdf">
								<iframe :src="attachmentUrl" title="Vista previa del archivo adjunto"></iframe>
							</div>

							<div v-else class="attachment-fallback">
								No hay vista previa embebida para este archivo. Usa “Abrir” o “Descargar”.
							</div>
						</div>

						<div v-else class="empty-inline">
							No hay archivo adjunto en esta solicitud.
						</div>
					</section>

					<section class="glass-card comments-card">
						<div class="card-title">
							<span class="card-title__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path :d="iconPath('comment')" />
								</svg>
							</span>
							<h2>Comentarios <span>({{ requestComments.length }})</span></h2>
						</div>

						<div v-if="requestComments.length" class="comments-list">
							<article v-for="comment in requestComments" :key="comment.id" class="comment-item">
								<div class="comment-avatar">
									{{ getInitials(comment.userName) }}
								</div>

								<div class="comment-body">
									<div class="comment-meta">
										<strong v-if="comment.userName">{{ comment.userName }}</strong>
										<span v-if="comment.createdAt">{{ formatDateTime(comment.createdAt) }}</span>
									</div>

									<div v-if="editingCommentId === comment.id" class="comment-edit">
										<textarea v-model.trim="editingCommentText"
												  class="comment-edit__input"
												  rows="3"
												  placeholder="Edita el comentario..." />

										<div class="comment-actions">
											<button class="comment-action comment-action--save"
													type="button"
													:disabled="isUpdatingComment"
													@click="saveEditedComment(comment)">
												<svg viewBox="0 0 24 24" aria-hidden="true">
													<path :d="iconPath('save')" />
												</svg>
												{{ isUpdatingComment ? "Guardando..." : "Guardar" }}
											</button>

											<button class="comment-action comment-action--cancel"
													type="button"
													:disabled="isUpdatingComment"
													@click="cancelEditingComment">
												<svg viewBox="0 0 24 24" aria-hidden="true">
													<path :d="iconPath('close')" />
												</svg>
												Cancelar
											</button>
										</div>
									</div>

									<div v-else>
										<p v-if="comment.text">{{ comment.text }}</p>

										<div class="comment-actions">
											<button class="comment-action comment-action--edit"
													type="button"
													@click="startEditingComment(comment)">
												<svg viewBox="0 0 24 24" aria-hidden="true">
													<path :d="iconPath('edit')" />
												</svg>
												Editar
											</button>

											<button class="comment-action comment-action--delete"
													type="button"
													:disabled="isDeletingCommentId === comment.id"
													@click="deleteComment(comment)">
												<svg viewBox="0 0 24 24" aria-hidden="true">
													<path :d="iconPath('trash')" />
												</svg>
												{{ isDeletingCommentId === comment.id ? "Eliminando..." : "Eliminar" }}
											</button>
										</div>
									</div>
								</div>
							</article>
						</div>

						<div v-else class="empty-inline empty-inline--comments">
							No hay comentarios registrados.
						</div>

						<div class="comment-box" :class="{ 'comment-box--error': commentError }">
							<span class="comment-box__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path :d="iconPath('comment')" />
								</svg>
							</span>

							<input v-model.trim="newComment"
								   class="comment-box__input"
								   type="text"
								   placeholder="Agregar comentario"
								   :disabled="isSubmittingComment"
								   @keydown.enter.prevent="submitComment" />

							<button class="comment-box__send"
									type="button"
									:disabled="isSubmittingComment || !newComment.trim()"
									@click="submitComment">
								<svg viewBox="0 0 24 24" aria-hidden="true">
									<path :d="iconPath('send')" />
								</svg>
							</button>
						</div>

						<div v-if="commentError" class="comment-error">
							{{ commentError }}
						</div>
					</section>
				</div>

				<aside class="detail-side">
					<section class="glass-card side-card">
						<h2 class="side-card__title">Información</h2>

						<div class="meta-list">
							<div v-for="item in metadataItems" :key="item.key" class="meta-item">
								<div class="meta-item__icon" :class="`meta-item__icon--${item.key}`" aria-hidden="true">
									<svg viewBox="0 0 24 24">
										<path :d="getMetaIcon(item.key)" />
									</svg>
								</div>

								<div class="meta-item__content">
									<div class="meta-item__label">{{ item.label }}</div>
									<div v-if="item.key === 'priority'"
										 class="meta-priority"
										 :class="`meta-priority--${String(item.value).toLowerCase()}`">
										<span class="meta-priority__dot"></span>
										<span>{{ item.value }}</span>
									</div>
									<div v-else class="meta-item__value">{{ item.value }}</div>
								</div>
							</div>
						</div>
					</section>
				</aside>
			</div>
		</section>
	</div>
</template>

<style scoped>
	.detail-page {
		min-height: 100vh;
		padding: 14px;
		background: radial-gradient(980px 560px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(920px 560px at 85% 85%, rgba(179, 94, 255, 0.2), rgba(255, 255, 255, 0) 62%), linear-gradient(180deg, #f7f5ff 0%, #eee8ff 55%, #ebe8ff 100%);
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
	}

	.detail-shell {
		max-width: 1420px;
		margin: 0 auto;
		padding: 16px;
		border-radius: 28px;
		background: rgba(255, 255, 255, 0.34);
		border: 1px solid rgba(147, 126, 235, 0.12);
		box-shadow: 0 24px 60px rgba(87, 81, 153, 0.12);
		backdrop-filter: blur(14px);
	}

	.detail-header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 18px;
		margin-bottom: 18px;
	}

	.detail-header__left {
		display: flex;
		align-items: flex-start;
		gap: 14px;
		min-width: 0;
	}

	.back-btn {
		width: 44px;
		height: 44px;
		border: none;
		border-radius: 14px;
		background: transparent;
		color: #675f99;
		display: grid;
		place-items: center;
		cursor: pointer;
		flex: 0 0 auto;
	}

		.back-btn svg {
			width: 20px;
			height: 20px;
		}

		.back-btn path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.detail-heading {
		min-width: 0;
	}

		.detail-heading h1 {
			margin: 0;
			font-size: clamp(26px, 3vw, 42px);
			line-height: 1.05;
			font-weight: 900;
			color: #4a427a;
			letter-spacing: -0.03em;
			word-break: break-word;
		}

		.detail-heading p {
			margin: 8px 0 0;
			font-size: 15px;
			font-weight: 700;
			color: rgba(88, 83, 132, 0.72);
		}

	.status-chip {
		min-height: 46px;
		padding: 0 18px;
		border-radius: 18px;
		display: inline-flex;
		align-items: center;
		gap: 8px;
		font-size: 15px;
		font-weight: 800;
		white-space: nowrap;
		border: 1px solid transparent;
		box-shadow: 0 10px 26px rgba(87, 81, 153, 0.1);
	}

		.status-chip svg {
			width: 18px;
			height: 18px;
		}

		.status-chip path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.1;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.status-chip--new {
		background: rgba(150, 122, 255, 0.14);
		color: rgba(96, 73, 219, 0.98);
		border-color: rgba(150, 122, 255, 0.18);
	}

	.status-chip--progress {
		background: rgba(255, 197, 90, 0.2);
		color: rgba(180, 120, 30, 0.98);
		border-color: rgba(255, 197, 90, 0.25);
	}

	.status-chip--done {
		background: rgba(60, 196, 151, 0.16);
		color: rgba(35, 130, 100, 0.98);
		border-color: rgba(60, 196, 151, 0.2);
	}

	.status-chip--closed {
		background: rgba(184, 194, 218, 0.24);
		color: rgba(72, 89, 125, 0.98);
		border-color: rgba(184, 194, 218, 0.26);
	}

	.status-chip--rejected {
		background: rgba(255, 90, 130, 0.14);
		color: rgba(170, 40, 80, 0.98);
		border-color: rgba(255, 90, 130, 0.2);
	}

	.status-chip--neutral {
		background: rgba(255, 255, 255, 0.72);
		color: rgba(74, 66, 122, 0.9);
		border-color: rgba(147, 126, 235, 0.12);
	}

	.detail-layout {
		display: grid;
		grid-template-columns: minmax(0, 1.35fr) minmax(280px, 0.62fr);
		gap: 16px;
		align-items: start;
	}

	.detail-main,
	.detail-side {
		display: grid;
		gap: 16px;
	}

	.glass-card,
	.state-card {
		border-radius: 22px;
		background: rgba(255, 255, 255, 0.5);
		border: 1px solid rgba(147, 126, 235, 0.1);
		box-shadow: 0 18px 36px rgba(87, 81, 153, 0.08);
		backdrop-filter: blur(10px);
	}

	.info-card {
		padding: 18px 20px 22px;
		min-height: 160px;
	}

	.attachment-card {
		padding: 18px 20px 22px;
	}

	.comments-card {
		padding: 18px 20px 20px;
	}

	.side-card {
		padding: 18px 18px 20px;
	}

	.side-card__title {
		margin: 0 0 14px;
		font-size: 19px;
		font-weight: 900;
		color: #4b447c;
	}

	.card-title {
		display: flex;
		align-items: center;
		gap: 10px;
		margin-bottom: 14px;
	}

		.card-title h2 {
			margin: 0;
			font-size: 16px;
			font-weight: 900;
			color: #564d8a;
			letter-spacing: -0.02em;
		}

			.card-title h2 span {
				color: rgba(86, 77, 138, 0.7);
			}

	.card-title__icon {
		width: 22px;
		height: 22px;
		color: #9d92dd;
		display: grid;
		place-items: center;
		flex: 0 0 auto;
	}

		.card-title__icon svg {
			width: 22px;
			height: 22px;
		}

		.card-title__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.detail-copy__label {
		font-size: 15px;
		font-weight: 500;
		color: rgba(86, 77, 138, 0.72);
		margin-bottom: 8px;
	}

	.detail-copy p {
		margin: 0;
		font-size: 17px;
		line-height: 1.6;
		color: #635a93;
	}

	.attachment-block {
		display: grid;
		gap: 14px;
	}

	.attachment-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 14px;
		flex-wrap: wrap;
		padding: 14px 16px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.42);
		border: 1px solid rgba(147, 126, 235, 0.12);
	}

	.attachment-file {
		display: flex;
		align-items: center;
		gap: 12px;
		min-width: 0;
	}

	.attachment-file__icon {
		width: 42px;
		height: 42px;
		border-radius: 14px;
		background: rgba(150, 122, 255, 0.14);
		color: rgba(96, 73, 219, 0.98);
		display: grid;
		place-items: center;
		flex: 0 0 auto;
	}

		.attachment-file__icon svg {
			width: 20px;
			height: 20px;
		}

		.attachment-file__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.1;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.attachment-file__content {
		min-width: 0;
	}

	.attachment-file__label {
		font-size: 13px;
		color: rgba(86, 77, 138, 0.68);
		margin-bottom: 2px;
	}

	.attachment-file__name {
		font-size: 15px;
		font-weight: 800;
		color: #564d8a;
		word-break: break-word;
	}

	.attachment-file__path {
		margin-top: 4px;
		font-size: 12px;
		color: rgba(86, 77, 138, 0.52);
		word-break: break-all;
	}

	.attachment-actions {
		display: flex;
		align-items: center;
		gap: 10px;
		flex-wrap: wrap;
	}

	.attachment-action {
		height: 38px;
		padding: 0 14px;
		border-radius: 12px;
		border: 1px solid rgba(147, 126, 235, 0.16);
		background: rgba(120, 105, 235, 0.9);
		color: #fff;
		text-decoration: none;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		font-size: 13px;
		font-weight: 800;
		box-shadow: 0 14px 28px rgba(88, 78, 212, 0.14);
	}

		.attachment-action svg {
			width: 15px;
			height: 15px;
		}

		.attachment-action path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.1;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.attachment-action--ghost {
		background: rgba(255, 255, 255, 0.76);
		color: rgba(88, 78, 212, 0.95);
	}

	.attachment-preview {
		border-radius: 20px;
		overflow: hidden;
		border: 1px solid rgba(147, 126, 235, 0.12);
		background: rgba(255, 255, 255, 0.44);
		box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.55);
	}

	.attachment-preview--image {
		padding: 12px;
	}

		.attachment-preview--image img {
			display: block;
			max-width: 100%;
			border-radius: 14px;
		}

	.attachment-preview--pdf {
		height: 620px;
	}

		.attachment-preview--pdf iframe {
			width: 100%;
			height: 100%;
			border: none;
			background: #fff;
		}

	.attachment-fallback {
		padding: 18px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.42);
		border: 1px solid rgba(147, 126, 235, 0.12);
		font-size: 14px;
		color: rgba(101, 93, 149, 0.72);
	}

	.comments-list {
		display: grid;
		gap: 14px;
		margin-bottom: 14px;
	}

	.comment-item {
		display: grid;
		grid-template-columns: 44px minmax(0, 1fr);
		gap: 14px;
		padding: 16px 18px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.38);
		border: 1px solid rgba(147, 126, 235, 0.12);
		box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.55);
	}

	.comment-avatar {
		width: 44px;
		height: 44px;
		border-radius: 16px;
		background: linear-gradient(135deg, #8365ff, #a46fff);
		color: #fff;
		display: grid;
		place-items: center;
		font-weight: 900;
		font-size: 20px;
		box-shadow: 0 12px 26px rgba(124, 96, 255, 0.25);
	}

	.comment-body {
		min-width: 0;
	}

	.comment-meta {
		display: flex;
		align-items: center;
		gap: 12px;
		flex-wrap: wrap;
		margin-bottom: 6px;
	}

		.comment-meta strong {
			font-size: 15px;
			font-weight: 800;
			color: #564d8a;
		}

		.comment-meta span {
			font-size: 14px;
			color: rgba(86, 77, 138, 0.62);
		}

	.comment-body p {
		margin: 0;
		font-size: 15px;
		line-height: 1.55;
		color: #6a6199;
		word-break: break-word;
	}

	.comment-edit {
		display: grid;
		gap: 10px;
	}

	.comment-edit__input {
		width: 100%;
		border: 1px solid rgba(147, 126, 235, 0.18);
		border-radius: 12px;
		background: rgba(255, 255, 255, 0.72);
		padding: 10px 12px;
		font-size: 14px;
		color: #655d95;
		outline: none;
		resize: vertical;
		min-height: 84px;
		box-sizing: border-box;
	}

		.comment-edit__input:focus {
			border-color: rgba(120, 105, 235, 0.48);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.22);
		}

	.comment-actions {
		display: flex;
		align-items: center;
		gap: 10px;
		flex-wrap: wrap;
		margin-top: 10px;
	}

	.comment-action {
		height: 32px;
		padding: 0 12px;
		border-radius: 10px;
		border: 1px solid rgba(147, 126, 235, 0.14);
		background: rgba(255, 255, 255, 0.74);
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 6px;
		font-size: 12px;
		font-weight: 800;
		cursor: pointer;
		box-shadow: 0 10px 22px rgba(87, 81, 153, 0.06);
	}

		.comment-action svg {
			width: 13px;
			height: 13px;
		}

		.comment-action path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.1;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.comment-action--edit {
		color: rgba(88, 78, 212, 0.95);
	}

	.comment-action--save {
		color: rgba(35, 130, 100, 0.98);
	}

	.comment-action--cancel {
		color: rgba(72, 89, 125, 0.98);
	}

	.comment-action--delete {
		color: rgba(170, 40, 80, 0.98);
	}

	.comment-action:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}

	.comment-box {
		display: grid;
		grid-template-columns: 28px minmax(0, 1fr) 36px;
		align-items: center;
		gap: 10px;
		padding: 0 12px 0 14px;
		min-height: 46px;
		border-radius: 16px;
		background: rgba(255, 255, 255, 0.52);
		border: 1px solid rgba(147, 126, 235, 0.14);
	}

	.comment-box--error {
		border-color: rgba(255, 90, 130, 0.24);
	}

	.comment-box__icon {
		color: #b0a5e7;
		display: grid;
		place-items: center;
	}

		.comment-box__icon svg,
		.comment-box__send svg {
			width: 18px;
			height: 18px;
		}

		.comment-box__icon path,
		.comment-box__send path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.1;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.comment-box__input {
		border: none;
		outline: none;
		background: transparent;
		font-size: 15px;
		color: #655d95;
		width: 100%;
		min-width: 0;
	}

		.comment-box__input::placeholder {
			color: rgba(101, 93, 149, 0.6);
		}

	.comment-box__send {
		width: 36px;
		height: 36px;
		border: none;
		border-radius: 12px;
		background: transparent;
		color: #a88df4;
		display: grid;
		place-items: center;
		cursor: pointer;
	}

		.comment-box__send:disabled {
			opacity: 0.45;
			cursor: not-allowed;
		}

	.comment-error {
		margin-top: 8px;
		font-size: 13px;
		font-weight: 700;
		color: rgba(170, 40, 80, 0.98);
	}

	.meta-list {
		display: grid;
		gap: 16px;
	}

	.meta-item {
		display: grid;
		grid-template-columns: 40px minmax(0, 1fr);
		gap: 12px;
		align-items: start;
	}

	.meta-item__icon {
		width: 40px;
		height: 40px;
		border-radius: 14px;
		display: grid;
		place-items: center;
		background: rgba(255, 255, 255, 0.42);
		color: #9f95df;
	}

		.meta-item__icon svg {
			width: 20px;
			height: 20px;
		}

		.meta-item__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.meta-item__icon--priority {
		color: #ff6688;
	}

	.meta-item__icon--createdBy,
	.meta-item__icon--assignedTo {
		color: #8b6af0;
	}

	.meta-item__label {
		font-size: 14px;
		color: rgba(86, 77, 138, 0.68);
		margin-bottom: 2px;
	}

	.meta-item__value {
		font-size: 16px;
		font-weight: 700;
		color: #564d8a;
		line-height: 1.4;
		word-break: break-word;
	}

	.meta-priority {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		min-height: 30px;
		padding: 0 12px;
		border-radius: 999px;
		border: 1px solid transparent;
		font-size: 14px;
		font-weight: 800;
		width: fit-content;
	}

	.meta-priority__dot {
		width: 10px;
		height: 10px;
		border-radius: 999px;
		background: currentColor;
	}

	.meta-priority--baja {
		background: rgba(60, 196, 151, 0.14);
		color: rgba(35, 130, 100, 0.98);
		border-color: rgba(60, 196, 151, 0.18);
	}

	.meta-priority--media {
		background: rgba(255, 197, 90, 0.18);
		color: rgba(180, 120, 30, 0.98);
		border-color: rgba(255, 197, 90, 0.22);
	}

	.meta-priority--alta {
		background: rgba(255, 90, 130, 0.12);
		color: rgba(170, 40, 80, 0.98);
		border-color: rgba(255, 90, 130, 0.18);
	}

	.empty-inline {
		font-size: 15px;
		color: rgba(101, 93, 149, 0.66);
	}

	.empty-inline--comments {
		margin-bottom: 14px;
	}

	.state-card {
		padding: 24px;
	}

	.state-card--error {
		border-color: rgba(255, 90, 130, 0.2);
	}

	.state-title {
		font-size: 18px;
		font-weight: 900;
		color: #524a83;
	}

	.state-text {
		margin-top: 8px;
		font-size: 15px;
		color: rgba(82, 74, 131, 0.7);
	}

	@media (max-width: 1100px) {
		.detail-layout {
			grid-template-columns: 1fr;
		}

		.detail-side {
			order: -1;
		}

		.attachment-preview--pdf {
			height: 480px;
		}
	}

	@media (max-width: 760px) {
		.detail-shell {
			padding: 14px;
			border-radius: 22px;
		}

		.detail-header {
			flex-direction: column;
			align-items: stretch;
		}

		.status-chip {
			align-self: flex-start;
		}

		.info-card,
		.attachment-card,
		.comments-card,
		.side-card {
			padding: 16px;
		}

		.detail-copy p {
			font-size: 15px;
		}

		.comment-item {
			grid-template-columns: 1fr;
		}

		.attachment-toolbar {
			align-items: stretch;
		}
	}

	@media (max-width: 560px) {
		.detail-page {
			padding: 10px;
		}

		.detail-heading h1 {
			font-size: 24px;
		}

		.detail-heading p {
			font-size: 13px;
		}

		.comment-box {
			grid-template-columns: 24px minmax(0, 1fr) 34px;
			padding-inline: 10px;
		}

		.attachment-actions {
			width: 100%;
		}

		.attachment-action {
			flex: 1 1 0;
		}
	}
</style>