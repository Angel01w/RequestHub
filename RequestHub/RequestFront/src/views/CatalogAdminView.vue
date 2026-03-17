<script setup>
	import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue"
	import { useRouter } from "vue-router"
	import { useAuthStore } from "../stores/auth"

	const router = useRouter()
	const auth = useAuthStore()

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "")

	const activeTab = ref("area")
	const search = ref("")

	const isAreaModalOpen = ref(false)
	const isTypeModalOpen = ref(false)
	const isUserModalOpen = ref(false)
	const showPassword = ref(false)

	const areas = ref([])
	const requestTypes = ref([])
	const users = ref([])

	const isLoadingAreas = ref(false)
	const isLoadingTypes = ref(false)
	const isLoadingUsers = ref(false)

	const apiErrorAreas = ref("")
	const apiErrorTypes = ref("")
	const apiErrorUsers = ref("")

	const areaForm = ref({ name: "" })
	const typeForm = ref({ name: "", areaId: "" })
	const userForm = ref({ name: "", email: "", password: "", role: "", areaId: "" })

	const editingAreaId = ref(null)
	const editingTypeId = ref(null)
	const editingUserId = ref(null)

	const roles = [
		{ value: "Solicitante", label: "Solicitante", id: 1 },
		{ value: "Gestor", label: "Gestor", id: 2 },
		{ value: "Admin", label: "Administrador", id: 3 },
		{ value: "SuperAdmin", label: "SuperAdmin", id: 4 }
	]

	const rolesWithoutArea = new Set(["Solicitante", "SuperAdmin"])

	const areaErrors = ref({ name: "" })
	const typeErrors = ref({ name: "", areaId: "" })
	const userErrors = ref({ name: "", email: "", password: "", role: "", areaId: "" })

	function normalizeRole(value) {
		return String(value ?? "").trim().toLowerCase()
	}

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

	const pageTitle = computed(() => "Administración")
	const pageSubtitle = computed(() => "Gestiona los catálogos y configuraciones del sistema")

	const normalizedRole = computed(() =>
		normalizeRole(auth.user?.role || auth.role || getStoredUser()?.role)
	)

	const isAdmin = computed(() => normalizedRole.value === "admin")
	const isSuperAdmin = computed(() => normalizedRole.value === "superadmin")

	const canViewCatalogPage = computed(() => isAdmin.value || isSuperAdmin.value)
	const canManageAreas = computed(() => isSuperAdmin.value)
	const canManageTypes = computed(() => isAdmin.value || isSuperAdmin.value)
	const canViewUsers = computed(() => isAdmin.value || isSuperAdmin.value)
	const canManageUsers = computed(() => isSuperAdmin.value)
	const canDeleteUsers = computed(() => isSuperAdmin.value)

	const isAuthorized = computed(() => canViewCatalogPage.value)

	const canCreateCurrentTab = computed(() => {
		if (activeTab.value === "area") return canManageAreas.value
		if (activeTab.value === "type") return canManageTypes.value
		if (activeTab.value === "user") return canManageUsers.value
		return false
	})

	const newButtonLabel = computed(() => {
		if (activeTab.value === "area") return "Nueva Área"
		if (activeTab.value === "type") return "Nuevo Tipo"
		return "Nuevo Usuario"
	})

	const searchPlaceholder = computed(() => {
		if (activeTab.value === "area") return "Buscar áreas..."
		if (activeTab.value === "type") return "Buscar tipos de solicitud..."
		return "Buscar usuarios..."
	})

	const apiError = computed(() => {
		if (activeTab.value === "area") return apiErrorAreas.value
		if (activeTab.value === "type") return apiErrorTypes.value
		return apiErrorUsers.value
	})

	const areaModalTitle = computed(() => editingAreaId.value ? "Editar Área" : "Agregar Nueva Área")
	const areaModalActionLabel = computed(() => editingAreaId.value ? "Guardar Cambios" : "Crear")

	const typeModalTitle = computed(() => editingTypeId.value ? "Editar Tipo de Solicitud" : "Nuevo Tipo de Solicitud")
	const typeModalActionLabel = computed(() => editingTypeId.value ? "Guardar Cambios" : "Crear Tipo")

	const userModalTitle = computed(() => editingUserId.value ? "Editar Usuario" : "Nuevo Usuario")
	const userModalActionLabel = computed(() => editingUserId.value ? "Guardar Cambios" : "Crear Usuario")

	const selectedUserRole = computed(() => toStr(userForm.value.role).trim())
	const userRoleRequiresArea = computed(() => {
		const role = selectedUserRole.value
		return !!role && !rolesWithoutArea.has(role)
	})
	const showUserAreaField = computed(() => userRoleRequiresArea.value)

	function joinUrl(base, path) {
		if (!base) return path
		if (!path) return base
		if (path.startsWith("http")) return path
		return `${base}${path.startsWith("/") ? "" : "/"}${path}`
	}

	async function readResponseError(res) {
		let msg = `Error ${res.status}`

		try {
			const data = await res.clone().json()
			msg = data?.message || data?.title || data?.error || data?.detail || msg
		} catch {
			try {
				const text = await res.clone().text()
				if (text) msg = text
			} catch {
			}
		}

		return msg
	}

	async function api(path, { method = "GET", body } = {}) {
		const token = getToken()

		const headers = {
			Accept: "application/json"
		}

		if (token) {
			headers.Authorization = `Bearer ${token}`
		}

		if (body !== undefined) {
			headers["Content-Type"] = "application/json"
		}

		const res = await fetch(joinUrl(API_BASE, path), {
			method,
			headers,
			body: body !== undefined ? JSON.stringify(body) : undefined
		})

		if (!res.ok) {
			if (res.status === 401) {
				auth.logout()
				router.replace("/login")
			}

			throw new Error(await readResponseError(res))
		}

		if (res.status === 204) return null

		const ct = res.headers.get("content-type") || ""
		if (ct.includes("application/json")) return await res.json()
		return await res.text()
	}

	function onBack() {
		router.back()
	}

	function closeModals() {
		isAreaModalOpen.value = false
		isTypeModalOpen.value = false
		isUserModalOpen.value = false
	}

	function resetAreaForm() {
		areaForm.value = { name: "" }
		areaErrors.value = { name: "" }
		editingAreaId.value = null
	}

	function resetTypeForm() {
		typeForm.value = { name: "", areaId: "" }
		typeErrors.value = { name: "", areaId: "" }
		editingTypeId.value = null
	}

	function resetUserForm() {
		userForm.value = { name: "", email: "", password: "", role: "", areaId: "" }
		userErrors.value = { name: "", email: "", password: "", role: "", areaId: "" }
		showPassword.value = false
		editingUserId.value = null
	}

	function onCancelModal() {
		closeModals()
		resetAreaForm()
		resetTypeForm()
		resetUserForm()
	}

	function openCreate() {
		if (!canCreateCurrentTab.value) return

		if (activeTab.value === "area") {
			resetAreaForm()
			isAreaModalOpen.value = true
			return
		}

		if (activeTab.value === "type") {
			resetTypeForm()
			isTypeModalOpen.value = true
			return
		}

		resetUserForm()
		isUserModalOpen.value = true
	}

	function openEditArea(area) {
		if (!canManageAreas.value) return
		editingAreaId.value = normalizeId(area.id)
		areaForm.value = { name: toStr(area.name) }
		areaErrors.value = { name: "" }
		isAreaModalOpen.value = true
	}

	function openEditType(type) {
		if (!canManageTypes.value) return
		editingTypeId.value = normalizeId(type.id)
		typeForm.value = {
			name: toStr(type.name),
			areaId: normalizeId(type.areaId) ?? ""
		}
		typeErrors.value = { name: "", areaId: "" }
		isTypeModalOpen.value = true
	}

	function openEditUser(user) {
		if (!canManageUsers.value) return
		editingUserId.value = normalizeId(user.id)
		userForm.value = {
			name: toStr(user.fullName),
			email: toStr(user.email ?? user.username),
			password: "",
			role: toStr(user.role),
			areaId: normalizeId(user.areaId) ?? ""
		}
		userErrors.value = { name: "", email: "", password: "", role: "", areaId: "" }
		showPassword.value = false
		isUserModalOpen.value = true
	}

	function validateArea() {
		areaErrors.value = { name: "" }

		const name = areaForm.value.name.trim()

		if (!name) {
			areaErrors.value.name = "El nombre es obligatorio."
			return false
		}

		if (name.length < 2) {
			areaErrors.value.name = "El nombre debe tener al menos 2 caracteres."
			return false
		}

		return true
	}

	function validateType() {
		typeErrors.value = { name: "", areaId: "" }
		let ok = true

		const name = typeForm.value.name.trim()
		const areaId = normalizeId(typeForm.value.areaId)

		if (!name) {
			typeErrors.value.name = "El nombre es obligatorio."
			ok = false
		} else if (name.length < 2) {
			typeErrors.value.name = "El nombre debe tener al menos 2 caracteres."
			ok = false
		}

		if (!areaId) {
			typeErrors.value.areaId = "Selecciona un área."
			ok = false
		}

		return ok
	}

	function validateUser() {
		userErrors.value = { name: "", email: "", password: "", role: "", areaId: "" }
		let ok = true

		const fullName = userForm.value.name.trim()
		const email = userForm.value.email.trim()
		const role = userForm.value.role.trim()
		const areaId = normalizeId(userForm.value.areaId)
		const password = userForm.value.password ?? ""
		const requiresArea = role && !rolesWithoutArea.has(role)

		if (!fullName) {
			userErrors.value.name = "El nombre es obligatorio."
			ok = false
		}

		if (!email) {
			userErrors.value.email = "El correo es obligatorio."
			ok = false
		} else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
			userErrors.value.email = "Correo inválido."
			ok = false
		}

		if (!editingUserId.value && !password) {
			userErrors.value.password = "La contraseña es obligatoria."
			ok = false
		}

		if (!role) {
			userErrors.value.role = "Selecciona un rol."
			ok = false
		}

		if (requiresArea && !areaId) {
			userErrors.value.areaId = "Selecciona un área."
			ok = false
		}

		return ok
	}

	function toStr(v) {
		return v == null ? "" : String(v)
	}

	function normalizeId(v) {
		return v == null || v === "" ? null : Number(v)
	}

	function mapArea(a) {
		const source = a?.area ?? a
		return {
			id: source?.id ?? source?.areaId ?? source?.Id ?? source?.AreaId ?? null,
			name: source?.name ?? source?.nombre ?? source?.Name ?? source?.Nombre ?? ""
		}
	}

	function mapType(t) {
		const source = t?.type ?? t
		return {
			id: source?.id ?? source?.requestTypeId ?? source?.Id ?? source?.RequestTypeId ?? source?.TipoSolicitudId ?? source?.tipoSolicitudId ?? null,
			name: source?.name ?? source?.nombre ?? source?.Name ?? source?.Nombre ?? "",
			areaId: source?.areaId ?? source?.AreaId ?? source?.idArea ?? source?.IdArea ?? null
		}
	}

	function mapUser(u) {
		const source = u?.user ?? u
		return {
			id: source?.id ?? source?.userId ?? source?.Id ?? source?.UserId ?? null,
			username: source?.username ?? source?.userName ?? source?.Username ?? source?.UserName ?? "",
			email: source?.email ?? source?.Email ?? source?.username ?? source?.Username ?? "",
			fullName: source?.fullName ?? source?.name ?? source?.FullName ?? source?.Name ?? source?.Nombre ?? "",
			role: source?.role ?? source?.rol ?? source?.Role ?? source?.Rol ?? "",
			areaId: source?.areaId ?? source?.AreaId ?? source?.idArea ?? source?.IdArea ?? null
		}
	}

	function upsertAreaInList(area) {
		const mapped = mapArea(area)

		if (mapped.id == null) return false

		const index = areas.value.findIndex(x => Number(x.id) === Number(mapped.id))

		if (index >= 0) areas.value[index] = mapped
		else areas.value.unshift(mapped)

		return true
	}

	function upsertTypeInList(type) {
		const mapped = mapType(type)

		if (mapped.id == null) return false

		const index = requestTypes.value.findIndex(x => Number(x.id) === Number(mapped.id))

		if (index >= 0) requestTypes.value[index] = mapped
		else requestTypes.value.unshift(mapped)

		return true
	}

	function upsertUserInList(user) {
		const mapped = mapUser(user)

		if (mapped.id == null) return false

		const index = users.value.findIndex(x => Number(x.id) === Number(mapped.id))

		if (index >= 0) users.value[index] = mapped
		else users.value.unshift(mapped)

		return true
	}

	function removeAreaFromList(id) {
		areas.value = areas.value.filter(x => Number(x.id) !== Number(id))
	}

	function removeTypeFromList(id) {
		requestTypes.value = requestTypes.value.filter(x => Number(x.id) !== Number(id))
	}

	function removeUserFromList(id) {
		users.value = users.value.filter(x => Number(x.id) !== Number(id))
	}

	const areaNameById = computed(() => {
		const map = new Map(areas.value.map(a => [toStr(a.id), toStr(a.name)]))
		return id => map.get(toStr(id)) ?? ""
	})

	const filteredAreas = computed(() => {
		const q = search.value.trim().toLowerCase()
		if (!q) return areas.value
		return areas.value.filter(a => toStr(a?.name).toLowerCase().includes(q))
	})

	const filteredTypes = computed(() => {
		const q = search.value.trim().toLowerCase()
		if (!q) return requestTypes.value

		return requestTypes.value.filter(t => {
			const n = toStr(t?.name).toLowerCase()
			const a = toStr(areaNameById.value(t?.areaId)).toLowerCase()
			return n.includes(q) || a.includes(q)
		})
	})

	const filteredUsers = computed(() => {
		const q = search.value.trim().toLowerCase()
		if (!q) return users.value

		return users.value.filter(u => {
			const full = toStr(u?.fullName).toLowerCase()
			const usern = toStr(u?.username).toLowerCase()
			const mail = toStr(u?.email).toLowerCase()
			const role = toStr(u?.role).toLowerCase()
			const area = toStr(areaNameById.value(u?.areaId)).toLowerCase()
			return full.includes(q) || usern.includes(q) || mail.includes(q) || role.includes(q) || area.includes(q)
		})
	})

	async function loadAreas() {
		isLoadingAreas.value = true
		apiErrorAreas.value = ""

		try {
			const data = await api("/api/Catalogs/areas")
			const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : []
			areas.value = arr.map(mapArea).filter(x => x.id != null)
		} catch (e) {
			apiErrorAreas.value = String(e?.message || e)
			areas.value = []
		} finally {
			isLoadingAreas.value = false
		}
	}

	async function loadTypes() {
		isLoadingTypes.value = true
		apiErrorTypes.value = ""

		try {
			const data = await api("/api/Catalogs/request-types")
			const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : []
			requestTypes.value = arr.map(mapType).filter(x => x.id != null)
		} catch (e) {
			apiErrorTypes.value = String(e?.message || e)
			requestTypes.value = []
		} finally {
			isLoadingTypes.value = false
		}
	}

	async function loadUsers() {
		if (!canViewUsers.value) {
			apiErrorUsers.value = "No autorizado"
			users.value = []
			isLoadingUsers.value = false
			return
		}

		isLoadingUsers.value = true
		apiErrorUsers.value = ""

		try {
			const data = await api("/api/Users")
			const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : []
			users.value = arr.map(mapUser).filter(x => x.id != null)
		} catch (e) {
			apiErrorUsers.value = String(e?.message || e)
			users.value = []
		} finally {
			isLoadingUsers.value = false
		}
	}

	async function onSaveArea() {
		if (!canManageAreas.value) {
			apiErrorAreas.value = "No autorizado"
			return
		}

		if (!validateArea()) return

		apiErrorAreas.value = ""

		const payload = {
			name: areaForm.value.name.trim()
		}

		try {
			if (editingAreaId.value != null) {
				const response = await api(`/api/Catalogs/areas/${editingAreaId.value}`, {
					method: "PUT",
					body: payload
				})

				const updated = response?.area ?? response

				if (!upsertAreaInList(updated)) {
					await loadAreas()
				}
			} else {
				const response = await api("/api/Catalogs/areas", {
					method: "POST",
					body: payload
				})

				const created = response?.area ?? response

				if (!upsertAreaInList(created)) {
					await loadAreas()
				}
			}

			closeModals()
			resetAreaForm()
		} catch (e) {
			apiErrorAreas.value = String(e?.message || e)
		}
	}

	async function onDeleteArea(area) {
		if (!canManageAreas.value) {
			apiErrorAreas.value = "No autorizado"
			return
		}

		const name = toStr(area?.name).trim()
		const id = normalizeId(area?.id)

		if (id == null) return

		const confirmed = window.confirm(`¿Deseas eliminar el área "${name}"?`)
		if (!confirmed) return

		apiErrorAreas.value = ""

		try {
			await api(`/api/Catalogs/areas/${id}`, { method: "DELETE" })
			removeAreaFromList(id)

			if (editingAreaId.value === id) {
				closeModals()
				resetAreaForm()
			}
		} catch (e) {
			apiErrorAreas.value = String(e?.message || e)
		}
	}

	async function onSaveType() {
		if (!canManageTypes.value) {
			apiErrorTypes.value = "No autorizado"
			return
		}

		if (!validateType()) return

		apiErrorTypes.value = ""

		const payload = {
			name: typeForm.value.name.trim(),
			areaId: normalizeId(typeForm.value.areaId)
		}

		try {
			if (editingTypeId.value != null) {
				const response = await api(`/api/Catalogs/request-types/${editingTypeId.value}`, {
					method: "PUT",
					body: payload
				})

				const updated = response?.type ?? response

				if (!upsertTypeInList(updated)) {
					await loadTypes()
				}
			} else {
				const response = await api("/api/Catalogs/request-types", {
					method: "POST",
					body: payload
				})

				const created = response?.type ?? response

				if (!upsertTypeInList(created)) {
					await loadTypes()
				}
			}

			closeModals()
			resetTypeForm()
		} catch (e) {
			apiErrorTypes.value = String(e?.message || e)
		}
	}

	async function onDeleteType(type) {
		if (!canManageTypes.value) {
			apiErrorTypes.value = "No autorizado"
			return
		}

		const name = toStr(type?.name).trim()
		const id = normalizeId(type?.id)

		if (id == null) return

		const confirmed = window.confirm(`¿Deseas eliminar el tipo de solicitud "${name}"?`)
		if (!confirmed) return

		apiErrorTypes.value = ""

		try {
			await api(`/api/Catalogs/request-types/${id}`, { method: "DELETE" })
			removeTypeFromList(id)

			if (editingTypeId.value === id) {
				closeModals()
				resetTypeForm()
			}
		} catch (e) {
			apiErrorTypes.value = String(e?.message || e)
		}
	}

	async function onSaveUser() {
		if (!canManageUsers.value) {
			apiErrorUsers.value = "No autorizado"
			return
		}

		if (!validateUser()) return

		apiErrorUsers.value = ""

		const normalizedSelectedRole = userForm.value.role.trim()
		const areaId = rolesWithoutArea.has(normalizedSelectedRole) ? null : normalizeId(userForm.value.areaId)

		const payload = {
			username: userForm.value.email.trim(),
			fullName: userForm.value.name.trim(),
			email: userForm.value.email.trim(),
			role: normalizedSelectedRole,
			areaId
		}

		if (!editingUserId.value) {
			payload.password = userForm.value.password
		}

		if (editingUserId.value != null) {
			try {
				const response = await api(`/api/Users/${editingUserId.value}`, {
					method: "PUT",
					body: payload
				})

				const updated = response?.user ?? response

				if (!upsertUserInList(updated)) {
					await loadUsers()
				}

				if (userForm.value.password.trim()) {
					await api(`/api/Users/${editingUserId.value}/reset-password`, {
						method: "POST",
						body: { newPassword: userForm.value.password }
					})
				}

				closeModals()
				resetUserForm()
			} catch (e) {
				apiErrorUsers.value = String(e?.message || e)
			}

			return
		}

		try {
			const response = await api("/api/Users", { method: "POST", body: payload })
			const created = response?.user ?? response

			if (!upsertUserInList(created)) {
				await loadUsers()
			}

			closeModals()
			resetUserForm()
		} catch (e) {
			apiErrorUsers.value = String(e?.message || e)
		}
	}

	async function onDeleteUser(user) {
		if (!canManageUsers.value) {
			apiErrorUsers.value = "No autorizado"
			return
		}

		const name = toStr(user?.fullName || user?.username).trim()
		const id = normalizeId(user?.id)

		if (id == null) return

		const confirmed = window.confirm(`¿Deseas eliminar el usuario "${name}"?`)
		if (!confirmed) return

		apiErrorUsers.value = ""

		try {
			await api(`/api/Users/${id}`, { method: "DELETE" })
			removeUserFromList(id)

			if (editingUserId.value === id) {
				closeModals()
				resetUserForm()
			}
		} catch (e) {
			apiErrorUsers.value = String(e?.message || e)
		}
	}

	function onKey(e) {
		if (e.key === "Escape" && (isAreaModalOpen.value || isTypeModalOpen.value || isUserModalOpen.value)) {
			onCancelModal()
		}
	}

	watch(selectedUserRole, role => {
		if (!role || rolesWithoutArea.has(role)) {
			userForm.value.areaId = ""
			userErrors.value.areaId = ""
		}
	})

	watch(activeTab, async (tab) => {
		if (tab === "area" && areas.value.length === 0 && !isLoadingAreas.value) {
			await loadAreas()
			return
		}

		if (tab === "type" && requestTypes.value.length === 0 && !isLoadingTypes.value) {
			await loadTypes()
			return
		}

		if (tab === "user" && users.value.length === 0 && !isLoadingUsers.value) {
			await loadUsers()
		}
	})

	onMounted(async () => {
		window.addEventListener("keydown", onKey)

		if (!getToken()) {
			router.replace("/login")
			return
		}

		if (!isAuthorized.value) {
			apiErrorUsers.value = "No autorizado"
			return
		}

		await Promise.all([loadAreas(), loadTypes(), loadUsers()])
	})

	onBeforeUnmount(() => {
		window.removeEventListener("keydown", onKey)
	})

	function iconPath(name) {
		switch (name) {
			case "arrowLeft":
				return "M15 18l-6-6 6-6"
			case "plus":
				return "M12 5v14M5 12h14"
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3"
			case "chev":
				return "M7 10l5 5 5-5"
			case "pencil":
				return "M4 20h4l10-10-4-4L4 16v4Zm9-13 4 4"
			case "trash":
				return "M6 7h12M9 7V5h6v2M8 7l1 13h6l1-13"
			case "x":
				return "M6 6l12 12M18 6L6 18"
			case "user":
				return "M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Zm-8 8a8 8 0 0 1 16 0"
			case "mail":
				return "M4 6h16v12H4V6Zm0 1 8 6 8-6"
			case "lock":
				return "M7 11V8a5 5 0 0 1 10 0v3M6 11h12v10H6V11Z"
			case "eye":
				return "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Zm10 3a3 3 0 1 0-3-3 3 3 0 0 0 3 3Z"
			case "paperPlane":
				return "M21 3 3 11l7 2 2 7 9-17ZM10 13l11-10"
			default:
				return ""
		}
	}
</script>

<template>
	<div class="admin">
		<div class="admin__card">
			<header class="admin__header">
				<button class="back" type="button" aria-label="Volver" @click="onBack">
					<svg viewBox="0 0 24 24" aria-hidden="true">
						<path :d="iconPath('arrowLeft')" />
					</svg>
				</button>

				<div class="titles">
					<h1>{{ pageTitle }}</h1>
					<p>{{ pageSubtitle }}</p>
				</div>
			</header>

			<div v-if="apiError" class="apiErr">{{ apiError }}</div>

			<div class="toolbar">
				<div class="tabs" role="tablist" aria-label="Catálogos">
					<button class="tab"
							:class="{ 'tab--active': activeTab === 'area' }"
							type="button"
							role="tab"
							:aria-selected="activeTab === 'area'"
							@click="activeTab = 'area'; search = ''">
						Área
					</button>
					<button class="tab"
							:class="{ 'tab--active': activeTab === 'type' }"
							type="button"
							role="tab"
							:aria-selected="activeTab === 'type'"
							@click="activeTab = 'type'; search = ''">
						Tipo de Solicitud
					</button>
					<button class="tab"
							:class="{ 'tab--active': activeTab === 'user' }"
							type="button"
							role="tab"
							:aria-selected="activeTab === 'user'"
							@click="activeTab = 'user'; search = ''">
						Usuario
					</button>
				</div>

				<button class="new"
						type="button"
						@click="openCreate"
						:disabled="!canCreateCurrentTab">
					<span class="new__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('plus')" /></svg>
					</span>
					<span>{{ newButtonLabel }}</span>
					<span class="new__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>
			</div>

			<div class="searchrow">
				<div class="search">
					<span class="search__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
					</span>
					<input v-model.trim="search" class="search__input" type="text" :placeholder="searchPlaceholder" />
				</div>

				<div class="actionsHint">ACCIONES</div>
			</div>

			<div class="tablewrap" role="region" aria-label="Tabla de administración">
				<table class="table" v-if="activeTab === 'area'">
					<thead>
						<tr>
							<th>NOMBRE</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody>
						<tr v-if="isLoadingAreas">
							<td class="loadingRow" colspan="2">Cargando...</td>
						</tr>
						<tr v-else-if="filteredAreas.length === 0">
							<td class="empty" colspan="2"></td>
						</tr>
						<tr v-else v-for="a in filteredAreas" :key="a.id">
							<td>{{ a.name }}</td>
							<td class="td-actions">
								<template v-if="canManageAreas">
									<button class="btn btn--edit" type="button" @click="openEditArea(a)">
										<span class="btn__icon" aria-hidden="true">
											<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
										</span>
										Editar
									</button>
									<button class="btn btn--del" type="button" @click="onDeleteArea(a)">
										<span class="btn__icon" aria-hidden="true">
											<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
										</span>
										Eliminar
									</button>
								</template>
								<span v-else class="noActions">Sin acciones</span>
							</td>
						</tr>
					</tbody>
				</table>

				<table class="table" v-else-if="activeTab === 'type'">
					<thead>
						<tr>
							<th>NOMBRE</th>
							<th>ÁREA</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody>
						<tr v-if="isLoadingTypes">
							<td class="loadingRow" colspan="3">Cargando...</td>
						</tr>
						<tr v-else-if="filteredTypes.length === 0">
							<td class="empty" colspan="3"></td>
						</tr>
						<tr v-else v-for="t in filteredTypes" :key="t.id">
							<td>{{ t.name }}</td>
							<td>{{ areaNameById(t.areaId) }}</td>
							<td class="td-actions">
								<button class="btn btn--edit" type="button" @click="openEditType(t)">
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
									</span>
									Editar
								</button>
								<button class="btn btn--del" type="button" @click="onDeleteType(t)">
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
									</span>
									Eliminar
								</button>
							</td>
						</tr>
					</tbody>
				</table>

				<table class="table" v-else>
					<thead>
						<tr>
							<th>NOMBRE</th>
							<th>CORREO/USUARIO</th>
							<th>ROL</th>
							<th>ÁREA</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody>
						<tr v-if="isLoadingUsers">
							<td class="loadingRow" colspan="5">Cargando...</td>
						</tr>
						<tr v-else-if="filteredUsers.length === 0">
							<td class="empty" colspan="5"></td>
						</tr>
						<tr v-else v-for="u in filteredUsers" :key="u.id">
							<td>{{ u.fullName }}</td>
							<td>{{ u.email || u.username }}</td>
							<td><span class="roleChip">{{ u.role }}</span></td>
							<td>{{ areaNameById(u.areaId) || "Todas" }}</td>
							<td class="td-actions">
								<template v-if="canManageUsers">
									<button class="btn btn--edit" type="button" @click="openEditUser(u)">
										<span class="btn__icon" aria-hidden="true">
											<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
										</span>
										Editar
									</button>
									<button class="btn btn--del" type="button" @click="onDeleteUser(u)" :disabled="!canDeleteUsers">
										<span class="btn__icon" aria-hidden="true">
											<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
										</span>
										Eliminar
									</button>
								</template>
								<span v-else class="noActions">Sin acciones</span>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>

		<div v-if="isAreaModalOpen"
			 class="modalOverlay"
			 role="dialog"
			 aria-modal="true"
			 :aria-label="areaModalTitle"
			 @mousedown.self="onCancelModal">
			<div class="modal">
				<header class="modal__head">
					<div>
						<h2>{{ areaModalTitle }}</h2>
						<p>Completa la información a continuación para crear o actualizar el área.</p>
					</div>
					<button class="modal__close" type="button" aria-label="Cerrar" @click="onCancelModal">
						<svg viewBox="0 0 24 24"><path :d="iconPath('x')" /></svg>
					</button>
				</header>

				<div class="modal__body">
					<div class="field">
						<label>Nombre</label>
						<input v-model.trim="areaForm.name" type="text" placeholder="Ingresa el nombre" />
						<div v-if="areaErrors.name" class="err">{{ areaErrors.name }}</div>
					</div>
				</div>

				<footer class="modal__foot">
					<button class="ghost" type="button" @click="onCancelModal">Cancelar</button>
					<button class="primary primary--spark" type="button" @click="onSaveArea">
						<span class="primary__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('paperPlane')" /></svg>
						</span>
						{{ areaModalActionLabel }}
					</button>
				</footer>
			</div>
		</div>

		<div v-if="isTypeModalOpen"
			 class="modalOverlay"
			 role="dialog"
			 aria-modal="true"
			 :aria-label="typeModalTitle"
			 @mousedown.self="onCancelModal">
			<div class="modal modal--user">
				<header class="modal__head">
					<div>
						<h2>{{ typeModalTitle }}</h2>
					</div>
					<button class="modal__close" type="button" aria-label="Cerrar" @click="onCancelModal">
						<svg viewBox="0 0 24 24"><path :d="iconPath('x')" /></svg>
					</button>
				</header>

				<div class="modal__body">
					<div class="field">
						<label>Nombre</label>
						<div class="control">
							<span class="control__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('user')" /></svg>
							</span>
							<input v-model.trim="typeForm.name" type="text" placeholder="Nombre" />
						</div>
						<div v-if="typeErrors.name" class="err">{{ typeErrors.name }}</div>
					</div>

					<div class="field">
						<label>Área</label>
						<div class="selectWrap">
							<select v-model="typeForm.areaId" class="nativeSelect" :disabled="areas.length === 0">
								<option value="" disabled>{{ areas.length === 0 ? "Crea un área primero" : "Selecciona un área" }}</option>
								<option v-for="a in areas" :key="a.id" :value="a.id">{{ a.name }}</option>
							</select>
							<span class="selectChev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</div>
						<div v-if="typeErrors.areaId" class="err">{{ typeErrors.areaId }}</div>
					</div>
				</div>

				<footer class="modal__foot modal__foot--right">
					<button class="ghost ghost--small" type="button" @click="onCancelModal">Cancelar</button>
					<button class="primary primary--solid" type="button" @click="onSaveType">{{ typeModalActionLabel }}</button>
				</footer>
			</div>
		</div>

		<div v-if="isUserModalOpen"
			 class="modalOverlay"
			 role="dialog"
			 aria-modal="true"
			 :aria-label="userModalTitle"
			 @mousedown.self="onCancelModal">
			<div class="modal modal--user">
				<header class="modal__head">
					<div>
						<h2>{{ userModalTitle }}</h2>
					</div>
					<button class="modal__close" type="button" aria-label="Cerrar" @click="onCancelModal">
						<svg viewBox="0 0 24 24"><path :d="iconPath('x')" /></svg>
					</button>
				</header>

				<div class="modal__body">
					<div class="field">
						<label>Nombre</label>
						<div class="control">
							<span class="control__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('user')" /></svg>
							</span>
							<input v-model.trim="userForm.name" type="text" placeholder="Nombre" />
						</div>
						<div v-if="userErrors.name" class="err">{{ userErrors.name }}</div>
					</div>

					<div class="field">
						<label>Correo/Usuario</label>
						<div class="control">
							<span class="control__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('mail')" /></svg>
							</span>
							<input v-model.trim="userForm.email" type="text" placeholder="Correo o usuario" />
						</div>
						<div v-if="userErrors.email" class="err">{{ userErrors.email }}</div>
					</div>

					<div class="field">
						<label>{{ editingUserId ? "Nueva Contraseña (Opcional)" : "Contraseña" }}</label>
						<div class="control">
							<span class="control__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('lock')" /></svg>
							</span>
							<input v-model="userForm.password" :type="showPassword ? 'text' : 'password'" placeholder="••••••••" />
							<button class="eye" type="button" aria-label="Ver contraseña" @click="showPassword = !showPassword">
								<svg viewBox="0 0 24 24"><path :d="iconPath('eye')" /></svg>
							</button>
						</div>
						<div v-if="userErrors.password" class="err">{{ userErrors.password }}</div>
					</div>

					<div class="field">
						<label>Rol</label>
						<div class="selectWrap">
							<select v-model="userForm.role" class="nativeSelect">
								<option value="" disabled>Selecciona un rol</option>
								<option v-for="r in roles" :key="r.value" :value="r.value">{{ r.label }}</option>
							</select>
							<span class="selectChev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</div>
						<div v-if="userErrors.role" class="err">{{ userErrors.role }}</div>
					</div>

					<div v-if="showUserAreaField" class="field">
						<label>Área</label>
						<div class="selectWrap">
							<select v-model="userForm.areaId" class="nativeSelect" :disabled="areas.length === 0">
								<option value="" disabled>{{ areas.length === 0 ? "Crea un área primero" : "Selecciona un área" }}</option>
								<option v-for="a in areas" :key="a.id" :value="a.id">{{ a.name }}</option>
							</select>
							<span class="selectChev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</div>
						<div v-if="userErrors.areaId" class="err">{{ userErrors.areaId }}</div>
					</div>

					<div v-else-if="selectedUserRole" class="roleHint">
						<span class="roleHint__badge">{{ selectedUserRole }}</span>
						<span>Este rol no requiere un área fija.</span>
					</div>
				</div>

				<footer class="modal__foot modal__foot--right">
					<button class="ghost ghost--small" type="button" @click="onCancelModal">Cancelar</button>
					<button class="primary primary--solid" type="button" @click="onSaveUser">{{ userModalActionLabel }}</button>
				</footer>
			</div>
		</div>
	</div>
</template>

<style scoped>
	.admin {
		min-height: 100vh;
		padding: 26px 22px;
		background: radial-gradient(900px 520px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(860px 520px at 85% 85%, rgba(179, 94, 255, 0.2), rgba(255, 255, 255, 0) 62%), linear-gradient(180deg, #f7f7ff 0%, #ece9ff 55%, #e9e7ff 100%);
		position: relative;
		overflow: hidden;
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, "Apple Color Emoji", "Segoe UI Emoji";
	}

		.admin::before {
			content: "";
			position: absolute;
			inset: 0;
			background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1200' height='700' viewBox='0 0 1200 700'%3E%3Cg fill='none' stroke='%23ffffff' stroke-opacity='.24'%3E%3Cpath d='M0 520 C 220 480 360 600 560 560 C 760 520 860 420 1200 460' stroke-width='18'/%3E%3Cpath d='M0 580 C 220 540 360 660 560 620 C 760 580 860 480 1200 520' stroke-width='14'/%3E%3Cpath d='M0 640 C 240 600 380 700 580 670 C 780 640 900 560 1200 600' stroke-width='10'/%3E%3C/g%3E%3C/svg%3E");
			background-repeat: no-repeat;
			background-position: center bottom;
			background-size: cover;
			pointer-events: none;
			opacity: 0.9;
		}

	.admin__card {
		position: relative;
		max-width: 1040px;
		margin: 0 auto;
		border-radius: 22px;
		background: rgba(255, 255, 255, 0.62);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 26px 60px rgba(40, 55, 95, 0.12);
		backdrop-filter: blur(10px);
		padding: 18px 18px 16px;
	}

	.admin__header {
		display: flex;
		align-items: flex-start;
		gap: 12px;
	}

	.back {
		width: 38px;
		height: 38px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.6);
		display: grid;
		place-items: center;
		cursor: pointer;
		color: rgba(90, 82, 160, 0.9);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
	}

		.back svg {
			width: 18px;
			height: 18px;
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
		font-size: 20px;
		font-weight: 900;
		color: #232a52;
		letter-spacing: -0.3px;
	}

	.titles p {
		margin: 4px 0 0;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.62);
	}

	.apiErr {
		margin-top: 12px;
		border-radius: 14px;
		border: 1px solid rgba(255, 84, 120, 0.25);
		background: rgba(255, 255, 255, 0.6);
		padding: 10px 12px;
		font-weight: 900;
		font-size: 12px;
		color: rgba(220, 60, 96, 0.95);
	}

	.toolbar {
		margin-top: 14px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
		padding: 12px;
		border-radius: 16px;
		background: rgba(255, 255, 255, 0.45);
		border: 1px solid rgba(110, 102, 182, 0.08);
	}

	.tabs {
		display: inline-flex;
		background: rgba(255, 255, 255, 0.55);
		border: 1px solid rgba(110, 102, 182, 0.1);
		border-radius: 14px;
		padding: 3px;
		gap: 4px;
		flex-wrap: wrap;
	}

	.tab {
		height: 30px;
		padding: 0 16px;
		border-radius: 11px;
		border: none;
		background: transparent;
		cursor: pointer;
		font-weight: 900;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.68);
	}

	.tab--active {
		background: rgba(120, 105, 235, 0.6);
		color: #fff;
		box-shadow: 0 12px 22px rgba(88, 78, 212, 0.2);
	}

	.new {
		height: 34px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(120, 105, 235, 0.72);
		color: #fff;
		font-weight: 900;
		font-size: 12px;
		cursor: pointer;
		padding: 0 12px;
		display: inline-flex;
		align-items: center;
		gap: 10px;
		box-shadow: 0 18px 28px rgba(88, 78, 212, 0.22);
	}

		.new:disabled {
			cursor: not-allowed;
			opacity: 0.7;
		}

	.new__icon svg,
	.new__chev svg {
		width: 16px;
		height: 16px;
	}

	.new__icon path,
	.new__chev path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.new__chev {
		opacity: 0.9;
	}

	.searchrow {
		margin-top: 12px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
	}

	.search {
		flex: 1 1 auto;
		height: 40px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.58);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 12px;
	}

	.search__icon {
		width: 28px;
		height: 28px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.14);
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
	}

		.search__icon svg {
			width: 16px;
			height: 16px;
		}

		.search__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.search__input {
		border: none;
		outline: none;
		background: transparent;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		flex: 1 1 auto;
		min-width: 0;
	}

		.search__input::placeholder {
			color: rgba(39, 46, 86, 0.4);
		}

	.actionsHint {
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.38);
		padding-right: 6px;
	}

	.tablewrap {
		margin-top: 10px;
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
		padding: 10px 14px;
		font-size: 10px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.55);
		text-align: left;
		background: linear-gradient(180deg, rgba(240, 236, 255, 0.7), rgba(255, 255, 255, 0.4));
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
	}

	tbody td {
		padding: 10px 14px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
	}

	tbody tr:last-child td {
		border-bottom: none;
	}

	.empty {
		height: 220px;
		background: rgba(255, 255, 255, 0);
	}

	.loadingRow {
		height: 120px;
		text-align: center;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.55);
	}

	.th-actions,
	.td-actions {
		text-align: right;
	}

	.td-actions {
		display: flex;
		gap: 10px;
		justify-content: flex-end;
		align-items: center;
	}

	.btn {
		height: 28px;
		border-radius: 8px;
		border: 1px solid rgba(120, 105, 235, 0.18);
		background: rgba(255, 255, 255, 0.7);
		color: rgba(90, 82, 160, 0.92);
		font-weight: 900;
		font-size: 12px;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		gap: 8px;
		padding: 0 10px;
		opacity: 0.92;
		transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
	}

		.btn:hover {
			transform: translateY(-1px);
			box-shadow: 0 10px 20px rgba(40, 55, 95, 0.08);
		}

		.btn[disabled] {
			cursor: not-allowed;
			opacity: 0.85;
		}

	.btn__icon svg {
		width: 14px;
		height: 14px;
	}

	.btn__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.btn--del {
		border-color: rgba(255, 84, 120, 0.2);
		color: rgba(220, 60, 96, 0.95);
	}

	.noActions {
		font-size: 12px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.45);
	}

	.roleChip {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		padding: 5px 10px;
		border-radius: 999px;
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.72);
		background: rgba(39, 46, 86, 0.08);
		border: 1px solid rgba(39, 46, 86, 0.1);
	}

	.modalOverlay {
		position: fixed;
		inset: 0;
		background: rgba(20, 18, 40, 0.18);
		backdrop-filter: blur(10px);
		display: grid;
		place-items: center;
		padding: 18px;
		z-index: 50;
	}

	.modal {
		width: 100%;
		max-width: 720px;
		border-radius: 22px;
		background: linear-gradient(180deg, rgba(255, 255, 255, 0.78), rgba(245, 242, 255, 0.78));
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 30px 80px rgba(40, 55, 95, 0.22);
		overflow: hidden;
		position: relative;
	}

		.modal:not(.modal--user) {
			max-width: 760px;
		}

	.modal--user {
		max-width: 640px;
	}

	.modal__head {
		padding: 18px 18px 12px;
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 14px;
	}

		.modal__head h2 {
			margin: 0;
			font-size: 22px;
			font-weight: 900;
			color: #232a52;
			letter-spacing: -0.3px;
		}

		.modal__head p {
			margin: 6px 0 0;
			font-size: 12px;
			color: rgba(39, 46, 86, 0.62);
		}

	.modal__close {
		width: 36px;
		height: 36px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.65);
		display: grid;
		place-items: center;
		cursor: pointer;
		color: rgba(39, 46, 86, 0.55);
	}

		.modal__close svg {
			width: 16px;
			height: 16px;
		}

		.modal__close path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.modal__body {
		padding: 4px 24px 16px;
		display: grid;
		gap: 12px;
	}

	.field label {
		display: block;
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.78);
		margin: 0 0 6px;
	}

	.field input,
	.nativeSelect {
		box-sizing: border-box;
	}

	.field input {
		width: 100%;
		height: 42px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.65);
		outline: none;
		padding: 0 12px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
	}

		.field input::placeholder {
			color: rgba(39, 46, 86, 0.42);
		}

		.field input:focus {
			border-color: rgba(120, 105, 235, 0.55);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.35);
		}

	.control {
		height: 42px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.65);
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 10px;
	}

		.control:focus-within {
			border-color: rgba(120, 105, 235, 0.55);
			box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.35);
		}

		.control input {
			height: 100%;
			border: none;
			background: transparent;
			outline: none;
			padding: 0;
		}

	.control__icon {
		width: 30px;
		height: 30px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.12);
		display: grid;
		place-items: center;
		color: rgba(120, 105, 235, 0.9);
		flex: 0 0 auto;
	}

		.control__icon svg {
			width: 16px;
			height: 16px;
		}

		.control__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.eye {
		width: 34px;
		height: 34px;
		border-radius: 999px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.55);
		cursor: pointer;
		display: grid;
		place-items: center;
		color: rgba(39, 46, 86, 0.55);
		flex: 0 0 auto;
	}

		.eye svg {
			width: 16px;
			height: 16px;
		}

		.eye path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.modal__foot {
		padding: 14px 24px 20px;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 14px;
	}

	.modal__foot--right {
		justify-content: flex-end;
	}

	.modal__foot .ghost,
	.modal__foot .primary {
		flex: 1 1 0;
		max-width: 320px;
	}

	.ghost {
		height: 44px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.55);
		font-weight: 900;
		color: rgba(39, 46, 86, 0.65);
		cursor: pointer;
	}

	.ghost--small {
		height: 36px;
		padding: 0 14px;
		border-radius: 10px;
		flex: 0 0 auto !important;
		max-width: none !important;
	}

	.primary {
		height: 44px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		font-weight: 900;
		cursor: pointer;
		color: #fff;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
	}

	.primary__icon svg {
		width: 16px;
		height: 16px;
	}

	.primary__icon path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.primary--spark {
		background: linear-gradient(90deg, rgba(90, 80, 220, 0.85), rgba(160, 86, 255, 0.82));
		position: relative;
		overflow: hidden;
		box-shadow: 0 18px 34px rgba(88, 78, 212, 0.22);
	}

		.primary--spark::after {
			content: "";
			position: absolute;
			inset: 0;
			background: radial-gradient(circle at 30% 30%, rgba(255, 255, 255, 0.22), rgba(255, 255, 255, 0) 55%);
			pointer-events: none;
		}

	.primary--solid {
		height: 36px;
		padding: 0 16px;
		border-radius: 10px;
		background: rgba(120, 105, 235, 0.85);
		box-shadow: 0 16px 30px rgba(88, 78, 212, 0.18);
		flex: 0 0 auto !important;
		max-width: none !important;
	}

	.err {
		margin-top: 8px;
		font-size: 12px;
		font-weight: 900;
		color: rgba(220, 60, 96, 0.95);
	}

	.selectWrap {
		position: relative;
	}

	.nativeSelect {
		width: 100%;
		height: 42px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.65);
		font-weight: 900;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.78);
		padding: 0 38px 0 12px;
		outline: none;
		appearance: none;
		cursor: pointer;
	}

		.nativeSelect:disabled {
			cursor: not-allowed;
			opacity: 0.7;
		}

	.selectChev {
		position: absolute;
		right: 10px;
		top: 50%;
		transform: translateY(-50%);
		pointer-events: none;
		opacity: 0.8;
	}

		.selectChev svg {
			width: 16px;
			height: 16px;
		}

		.selectChev path {
			fill: none;
			stroke: rgba(39, 46, 86, 0.45);
			stroke-width: 2.2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.roleHint {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 8px;
		padding: 12px 14px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.6);
		font-size: 12px;
		font-weight: 800;
		color: rgba(39, 46, 86, 0.72);
	}

	.roleHint__badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		padding: 5px 10px;
		border-radius: 999px;
		font-size: 11px;
		font-weight: 900;
		color: #fff;
		background: rgba(120, 105, 235, 0.82);
	}

	@media (max-width: 820px) {
		.admin {
			padding: 18px 12px;
		}

		.modal__foot {
			flex-direction: column;
			align-items: stretch;
		}

		.modal__foot--right {
			justify-content: stretch;
		}

		.modal__foot .ghost,
		.modal__foot .primary,
		.ghost--small,
		.primary--solid {
			flex: 1 1 auto !important;
			max-width: none !important;
			width: 100%;
		}
	}
</style>