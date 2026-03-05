<!-- src/views/CatalogAdminView.vue -->
<script setup>
	import { computed, onBeforeUnmount, onMounted, ref } from "vue";
	import { useRouter } from "vue-router";

	const router = useRouter();

	const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").replace(/\/+$/, "");
	const TOKEN_KEY = "token";

	const activeTab = ref("area");
	const search = ref("");

	const isAreaModalOpen = ref(false);
	const isTypeModalOpen = ref(false);
	const isUserModalOpen = ref(false);
	const showPassword = ref(false);

	const areas = ref([]);
	const requestTypes = ref([]);
	const users = ref([]);

	const isLoadingAreas = ref(false);
	const isLoadingTypes = ref(false);
	const isLoadingUsers = ref(false);

	const apiErrorAreas = ref("");
	const apiErrorTypes = ref("");
	const apiErrorUsers = ref("");

	const areaForm = ref({ name: "" });
	const typeForm = ref({ name: "", areaId: "" });

	const roles = [
		{ value: "Solicitante", label: "Solicitante", id: 1 },
		{ value: "Gestor", label: "Gestor", id: 2 },
		{ value: "Admin", label: "Administrador", id: 3 },
	];

	const userForm = ref({ name: "", email: "", password: "", role: "", areaId: "" });

	const areaErrors = ref({ name: "" });
	const typeErrors = ref({ name: "", areaId: "" });
	const userErrors = ref({ name: "", email: "", password: "", role: "", areaId: "" });

	const pageTitle = computed(() => "Administración");
	const pageSubtitle = computed(() => "Gestiona los catálogos y configuraciones del sistema");

	const newButtonLabel = computed(() => {
		if (activeTab.value === "area") return "Nueva Área";
		if (activeTab.value === "type") return "Nuevo Tipo";
		return "Nuevo Usuario";
	});

	const searchPlaceholder = computed(() => {
		if (activeTab.value === "area") return "Buscar áreas...";
		if (activeTab.value === "type") return "Buscar tipos de solicitud...";
		return "Buscar usuarios...";
	});

	const apiError = computed(() => {
		if (activeTab.value === "area") return apiErrorAreas.value;
		if (activeTab.value === "type") return apiErrorTypes.value;
		return apiErrorUsers.value;
	});

	function clearTokens() {
		const keys = [
			"token",
			"access_token",
			"accessToken",
			"jwt",
			"jwt_token",
			"rh_token",
			"sm_token",
		];
		for (const k of keys) {
			localStorage.removeItem(k);
			sessionStorage.removeItem(k);
		}
	}

	function getToken() {
		const keys = [
			TOKEN_KEY,
			"access_token",
			"accessToken",
			"jwt",
			"jwt_token",
			"rh_token",
			"sm_token",
		];

		for (const k of keys) {
			const v = localStorage.getItem(k) || sessionStorage.getItem(k);
			if (v) return String(v).replace(/^"+|"+$/g, "");
		}

		const objKeys = ["user", "rh_user", "sm_user", "auth", "auth_user"];

		for (const k of objKeys) {
			try {
				const raw = localStorage.getItem(k) || sessionStorage.getItem(k);
				if (!raw) continue;
				const o = JSON.parse(raw);
				const t = o?.token || o?.accessToken || o?.jwt;
				if (t) return String(t).replace(/^"+|"+$/g, "");
			} catch { }
		}

		return "";
	}

	function joinUrl(base, path) {
		if (!base) return path;
		if (!path) return base;
		if (path.startsWith("http")) return path;
		return `${base}${path.startsWith("/") ? "" : "/"}${path}`;
	}

	async function api(path, { method = "GET", body } = {}) {
		const headers = { Accept: "application/json" };
		const token = getToken();
		if (token) headers.Authorization = `Bearer ${token}`;
		if (body !== undefined) headers["Content-Type"] = "application/json";

		const res = await fetch(joinUrl(API_BASE, path), {
			method,
			headers,
			body: body !== undefined ? JSON.stringify(body) : undefined,
		});

		if (res.status === 401) {
			clearTokens();
			throw new Error("No autorizado. Inicia sesión y vuelve a intentar.");
		}

		if (!res.ok) {
			let msg = `Error ${res.status}`;
			try {
				const data = await res.json();
				msg = data?.message || data?.title || data?.error || data?.detail || msg;
			} catch {
				try {
					const text = await res.text();
					if (text) msg = text;
				} catch { }
			}
			throw new Error(msg);
		}

		if (res.status === 204) return null;
		const ct = res.headers.get("content-type") || "";
		if (ct.includes("application/json")) return await res.json();
		return await res.text();
	}

	function onBack() {
		router.back();
	}

	function closeModals() {
		isAreaModalOpen.value = false;
		isTypeModalOpen.value = false;
		isUserModalOpen.value = false;
	}

	function resetAreaForm() {
		areaForm.value = { name: "" };
		areaErrors.value = { name: "" };
	}

	function resetTypeForm() {
		typeForm.value = { name: "", areaId: "" };
		typeErrors.value = { name: "", areaId: "" };
	}

	function resetUserForm() {
		userForm.value = { name: "", email: "", password: "", role: "", areaId: "" };
		userErrors.value = { name: "", email: "", password: "", role: "", areaId: "" };
		showPassword.value = false;
	}

	function onCancelModal() {
		closeModals();
		resetAreaForm();
		resetTypeForm();
		resetUserForm();
	}

	function openCreate() {
		if (activeTab.value === "area") {
			resetAreaForm();
			isAreaModalOpen.value = true;
			return;
		}
		if (activeTab.value === "type") {
			resetTypeForm();
			isTypeModalOpen.value = true;
			return;
		}
		resetUserForm();
		isUserModalOpen.value = true;
	}

	function validateArea() {
		areaErrors.value = { name: "" };
		if (!areaForm.value.name.trim()) {
			areaErrors.value.name = "El nombre es obligatorio.";
			return false;
		}
		return true;
	}

	function validateType() {
		typeErrors.value = { name: "", areaId: "" };
		let ok = true;

		if (!typeForm.value.name.trim()) {
			typeErrors.value.name = "El nombre es obligatorio.";
			ok = false;
		}
		if (!typeForm.value.areaId) {
			typeErrors.value.areaId = "Selecciona un área.";
			ok = false;
		}
		return ok;
	}

	function validateUser() {
		userErrors.value = { name: "", email: "", password: "", role: "", areaId: "" };
		let ok = true;

		if (!userForm.value.name.trim()) {
			userErrors.value.name = "El nombre es obligatorio.";
			ok = false;
		}

		const emailOrUser = userForm.value.email.trim();
		if (!emailOrUser) {
			userErrors.value.email = "El usuario/correo es obligatorio.";
			ok = false;
		} else if (emailOrUser.includes("@") && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailOrUser)) {
			userErrors.value.email = "Correo inválido.";
			ok = false;
		} else if (!emailOrUser.includes("@") && emailOrUser.length < 3) {
			userErrors.value.email = "Usuario inválido.";
			ok = false;
		}

		if (!userForm.value.password) {
			userErrors.value.password = "La contraseña es obligatoria.";
			ok = false;
		}

		if (!userForm.value.role) {
			userErrors.value.role = "Selecciona un rol.";
			ok = false;
		}

		if (!userForm.value.areaId) {
			userErrors.value.areaId = "Selecciona un área.";
			ok = false;
		}

		return ok;
	}

	function toStr(v) {
		return v == null ? "" : String(v);
	}

	function mapArea(a) {
		return {
			id: a.id ?? a.areaId ?? a.Id ?? a.AreaId,
			name: a.name ?? a.nombre ?? a.Name ?? a.Nombre,
		};
	}

	function mapType(t) {
		return {
			id: t.id ?? t.requestTypeId ?? t.Id ?? t.RequestTypeId ?? t.TipoSolicitudId ?? t.tipoSolicitudId,
			name: t.name ?? t.nombre ?? t.Name ?? t.Nombre,
			areaId: t.areaId ?? t.AreaId ?? t.idArea ?? t.IdArea,
		};
	}

	function mapUser(u) {
		return {
			id: u.id ?? u.userId ?? u.Id ?? u.UserId,
			username: u.username ?? u.userName ?? u.email ?? u.Username ?? u.UserName ?? u.Email,
			fullName: u.fullName ?? u.name ?? u.FullName ?? u.Name ?? u.Nombre ?? "",
			role: u.role ?? u.rol ?? u.Role ?? u.Rol ?? "",
			areaId: u.areaId ?? u.AreaId ?? u.idArea ?? u.IdArea ?? null,
		};
	}

	const areaNameById = computed(() => {
		const map = new Map(areas.value.map((a) => [toStr(a.id), toStr(a.name)]));
		return (id) => map.get(toStr(id)) ?? "";
	});

	const filteredAreas = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return areas.value;
		return areas.value.filter((a) => toStr(a?.name).toLowerCase().includes(q));
	});

	const filteredTypes = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return requestTypes.value;
		return requestTypes.value.filter((t) => {
			const n = toStr(t?.name).toLowerCase();
			const a = toStr(areaNameById.value(t?.areaId)).toLowerCase();
			return n.includes(q) || a.includes(q);
		});
	});

	const filteredUsers = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return users.value;
		return users.value.filter((u) => {
			const full = toStr(u?.fullName).toLowerCase();
			const usern = toStr(u?.username).toLowerCase();
			const role = toStr(u?.role).toLowerCase();
			const area = toStr(areaNameById.value(u?.areaId)).toLowerCase();
			return full.includes(q) || usern.includes(q) || role.includes(q) || area.includes(q);
		});
	});

	async function loadAreas() {
		isLoadingAreas.value = true;
		apiErrorAreas.value = "";
		try {
			const data = await api("/api/Catalogs/areas");
			const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : [];
			areas.value = arr.map(mapArea).filter((x) => x.id != null);
		} catch (e) {
			apiErrorAreas.value = String(e?.message || e);
			areas.value = [];
		} finally {
			isLoadingAreas.value = false;
		}
	}

	async function loadTypes() {
		isLoadingTypes.value = true;
		apiErrorTypes.value = "";
		try {
			const data = await api("/api/Catalogs/request-types");
			const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : [];
			requestTypes.value = arr.map(mapType).filter((x) => x.id != null);
		} catch (e) {
			apiErrorTypes.value = String(e?.message || e);
			requestTypes.value = [];
		} finally {
			isLoadingTypes.value = false;
		}
	}

	async function loadUsers() {
		isLoadingUsers.value = true;
		apiErrorUsers.value = "";
		try {
			users.value = [];
			apiErrorUsers.value = "Este módulo requiere un endpoint de usuarios. La API actual solo expone /api/Auth/login.";
		} finally {
			isLoadingUsers.value = false;
		}
	}

	async function onCreateArea() {
		if (!validateArea()) return;

		apiErrorAreas.value = "";
		const payload = { name: areaForm.value.name.trim() };

		try {
			const created = await api("/api/Catalogs/areas", { method: "POST", body: payload });
			const mapped = mapArea(created || payload);
			if (mapped?.id == null) await loadAreas();
			else areas.value.unshift(mapped);
			closeModals();
			resetAreaForm();
		} catch (e) {
			apiErrorAreas.value = String(e?.message || e);
			if (String(e?.message || e).toLowerCase().includes("no autorizado")) router.push("/login");
		}
	}

	async function onCreateType() {
		if (!validateType()) return;

		apiErrorTypes.value = "";
		const payload = { name: typeForm.value.name.trim(), areaId: typeForm.value.areaId };

		try {
			const created = await api("/api/Catalogs/request-types", { method: "POST", body: payload });
			const mapped = mapType(created || payload);
			if (mapped?.id == null) await loadTypes();
			else requestTypes.value.unshift(mapped);
			closeModals();
			resetTypeForm();
		} catch (e) {
			apiErrorTypes.value = String(e?.message || e);
			if (String(e?.message || e).toLowerCase().includes("no autorizado")) router.push("/login");
		}
	}

	async function onCreateUser() {
		if (!validateUser()) return;
		apiErrorUsers.value = "No hay API para crear usuarios. Implementa un endpoint (por ejemplo /api/Users) o crea usuarios por seed/BD.";
	}

	function onKey(e) {
		if (e.key === "Escape" && (isAreaModalOpen.value || isTypeModalOpen.value || isUserModalOpen.value)) onCancelModal();
	}

	onMounted(async () => {
		window.addEventListener("keydown", onKey);
		await loadAreas();
		await loadTypes();
		await loadUsers();
	});

	onBeforeUnmount(() => window.removeEventListener("keydown", onKey));

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
			case "pencil":
				return "M4 20h4l10-10-4-4L4 16v4Zm9-13 4 4";
			case "trash":
				return "M6 7h12M9 7V5h6v2M8 7l1 13h6l1-13";
			case "x":
				return "M6 6l12 12M18 6L6 18";
			case "user":
				return "M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Zm-8 8a8 8 0 0 1 16 0";
			case "mail":
				return "M4 6h16v12H4V6Zm0 1 8 6 8-6";
			case "lock":
				return "M7 11V8a5 5 0 0 1 10 0v3M6 11h12v10H6V11Z";
			case "eye":
				return "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Zm10 3a3 3 0 1 0-3-3 3 3 0 0 0 3 3Z";
			case "paperPlane":
				return "M21 3 3 11l7 2 2 7 9-17ZM10 13l11-10";
			default:
				return "";
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

				<button class="new" type="button" @click="openCreate">
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
							<td class="empty" colspan="2"> </td>
						</tr>
						<tr v-else v-for="a in filteredAreas" :key="a.id">
							<td>{{ a.name }}</td>
							<td class="td-actions">
								<button class="btn btn--edit" type="button" disabled>
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
									</span>
									Editar
								</button>
								<button class="btn btn--del" type="button" disabled>
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
									</span>
									Eliminar
								</button>
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
							<td class="empty" colspan="3"> </td>
						</tr>
						<tr v-else v-for="t in filteredTypes" :key="t.id">
							<td>{{ t.name }}</td>
							<td>{{ areaNameById(t.areaId) }}</td>
							<td class="td-actions">
								<button class="btn btn--edit" type="button" disabled>
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
									</span>
									Editar
								</button>
								<button class="btn btn--del" type="button" disabled>
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
							<td class="empty" colspan="5"> </td>
						</tr>
						<tr v-else v-for="u in filteredUsers" :key="u.id">
							<td>{{ u.fullName }}</td>
							<td>{{ u.username }}</td>
							<td><span class="roleChip">{{ u.role }}</span></td>
							<td>{{ areaNameById(u.areaId) }}</td>
							<td class="td-actions">
								<button class="btn btn--edit" type="button" disabled>
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('pencil')" /></svg>
									</span>
									Editar
								</button>
								<button class="btn btn--del" type="button" disabled>
									<span class="btn__icon" aria-hidden="true">
										<svg viewBox="0 0 24 24"><path :d="iconPath('trash')" /></svg>
									</span>
									Eliminar
								</button>
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
			 aria-label="Agregar Nueva Área"
			 @mousedown.self="onCancelModal">
			<div class="modal">
				<header class="modal__head">
					<div>
						<h2>Agregar Nueva Área</h2>
						<p>Completa la información a continuación para crear una nueva entrada.</p>
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
					<button class="primary primary--spark" type="button" @click="onCreateArea">
						<span class="primary__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('paperPlane')" /></svg>
						</span>
						Crear
					</button>
				</footer>
			</div>
		</div>

		<div v-if="isTypeModalOpen"
			 class="modalOverlay"
			 role="dialog"
			 aria-modal="true"
			 aria-label="Agregar Tipo de Solicitud"
			 @mousedown.self="onCancelModal">
			<div class="modal modal--user">
				<header class="modal__head">
					<div>
						<h2>Nuevo Tipo de Solicitud</h2>
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
					<button class="primary primary--solid" type="button" @click="onCreateType">Crear Tipo</button>
				</footer>
			</div>
		</div>

		<div v-if="isUserModalOpen"
			 class="modalOverlay"
			 role="dialog"
			 aria-modal="true"
			 aria-label="Nuevo Usuario"
			 @mousedown.self="onCancelModal">
			<div class="modal modal--user">
				<header class="modal__head">
					<div>
						<h2>Nuevo Usuario</h2>
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
						<label>Contraseña</label>
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

					<div class="field">
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
				</div>

				<footer class="modal__foot modal__foot--right">
					<button class="ghost ghost--small" type="button" @click="onCancelModal">Cancelar</button>
					<button class="primary primary--solid" type="button" @click="onCreateUser">Crear Usuario</button>
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
		padding: 4px 18px 16px;
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
		padding: 14px 18px 18px;
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 12px;
	}

	.modal__foot--right {
		grid-template-columns: auto auto;
		justify-content: end;
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

	@media (max-width: 820px) {
		.admin {
			padding: 18px 12px;
		}

		.modal__foot {
			grid-template-columns: 1fr;
		}

		.modal__foot--right {
			grid-template-columns: 1fr;
			justify-content: stretch;
		}
	}
</style>