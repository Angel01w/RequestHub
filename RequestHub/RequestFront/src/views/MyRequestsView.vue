<!-- src/views/MyRequestsView.vue -->
<script setup>
	import { computed, ref } from "vue";
	import { useRouter } from "vue-router";

	const router = useRouter();

	const isNewOpen = ref(false);

	const search = ref("");
	const hasRequests = computed(() => false); // sin info por ahora

	const form = ref({
		areaId: "",
		typeId: "",
		priority: "", // "Baja" | "Media" | "Alta"
		subject: "",
		description: "",
		attachment: null,
	});

	const isDragging = ref(false);

	function openNew() {
		isNewOpen.value = true;
	}

	function closeNew() {
		isNewOpen.value = false;
		isDragging.value = false;
		form.value = {
			areaId: "",
			typeId: "",
			priority: "",
			subject: "",
			description: "",
			attachment: null,
		};
	}

	function onBack() {
		router.back();
	}

	function onPickFile(e) {
		const file = e?.target?.files?.[0] ?? null;
		form.value.attachment = file;
	}

	function onDrop(e) {
		isDragging.value = false;
		const file = e?.dataTransfer?.files?.[0] ?? null;
		form.value.attachment = file;
	}

	function submit() {
		// UI only por ahora
		closeNew();
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
			default:
				return "";
		}
	}
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

			<div class="filters">
				<div class="pill pill--search">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
					</span>
					<input v-model.trim="search" class="pill__input" type="text" placeholder="Buscar por número o asunto..." />
				</div>

				<button class="pill" type="button">
					<span class="pill__txt">Todos los Estados</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__txt">Todas las Áreas</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__txt">Todas las Prioridades</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill pill--date" type="button">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
					</span>
					<span class="pill__txt">Todas las fechas</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>
			</div>

			<div class="tablewrap" role="region" aria-label="Tabla de mis solicitudes">
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
						</tr>
					</thead>
					<tbody>
						<!-- sin filas por ahora -->
					</tbody>
				</table>

				<div v-else class="empty">
					<div class="empty__circle" aria-hidden="true">+</div>
					<div class="empty__title">Aún no tienes solicitudes</div>
					<div class="empty__sub">
						Crea una nueva solicitud para comenzar y podrás dar seguimiento<br />
						por estado, área y prioridad.
					</div>

					<div class="chips" aria-hidden="true">
						<span class="chip chip--new">Nueva</span>
						<span class="chip chip--proc">En Proceso</span>
						<span class="chip chip--done">Resuelta</span>
					</div>
				</div>
			</div>

			<footer class="foot">
				<div>© 2026 RequestHub • Mesa de Servicios Internos</div>
				<div class="foot__sub">SOL-YYYY-0001 • JWT Auth • v1.0</div>
			</footer>
		</section>

		<button class="help" type="button" aria-label="Ayuda">?</button>

		<!-- Modal / Panel: Nueva Solicitud -->
		<div v-if="isNewOpen" class="overlay" role="dialog" aria-modal="true" aria-label="Nueva Solicitud">
			<div class="modal">
				<header class="modal__head">
					<div class="modal__left">
						<button class="modalBack" type="button" aria-label="Volver" @click="closeNew">
							<svg viewBox="0 0 24 24" aria-hidden="true">
								<path :d="iconPath('arrowLeft')" />
							</svg>
						</button>
						<div>
							<h2>Nueva Solicitud</h2>
							<p>Completa el formulario para crear tu solicitud</p>
						</div>
					</div>
				</header>

				<div class="modal__body">
					<div class="row2">
						<div class="field">
							<label>Área</label>
							<button class="select" type="button">
								<span class="select__txt">Selecciona un área</span>
								<span class="select__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>
						</div>

						<div class="field">
							<label>Tipo de Solicitud</label>
							<button class="select" type="button">
								<span class="select__txt">Selecciona un tipo</span>
								<span class="select__chev" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
								</span>
							</button>
							<div class="helper">Selecciona un área primero.</div>
						</div>
					</div>

					<div class="field">
						<label>Prioridad *</label>
						<div class="prio">
							<button class="prioCard prioCard--low"
									:class="{ 'prioCard--active': form.priority === 'Baja' }"
									type="button"
									@click="form.priority = 'Baja'">
								<div class="prioDot prioDot--low"></div>
								<div class="prioMain">Baja</div>
								<div class="prioSub">Prioridad baja</div>
							</button>

							<button class="prioCard prioCard--mid"
									:class="{ 'prioCard--active': form.priority === 'Media' }"
									type="button"
									@click="form.priority = 'Media'">
								<div class="prioDot prioDot--mid"></div>
								<div class="prioMain">Media</div>
								<div class="prioSub">Prioridad media</div>
							</button>

							<button class="prioCard prioCard--high"
									:class="{ 'prioCard--active': form.priority === 'Alta' }"
									type="button"
									@click="form.priority = 'Alta'">
								<div class="prioDot prioDot--high"></div>
								<div class="prioMain">Alta</div>
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
						<textarea v-model.trim="form.description"
								  rows="3"
								  placeholder="Proporciona todos los detalles relevantes sobre tu solicitud..." />
						<div class="helper">* Opcional.</div>
					</div>

					<div class="field">
						<label>Archivo Adjunto (Opcional)</label>
						<div class="drop"
							 :class="{ 'drop--drag': isDragging }"
							 @dragenter.prevent="isDragging = true"
							 @dragover.prevent="isDragging = true"
							 @dragleave.prevent="isDragging = false"
							 @drop.prevent="onDrop">
							<div class="drop__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('upload')" /></svg>
							</div>
							<div class="drop__txt">
								<div class="drop__title">Arrastra y suelta un archivo aquí o haz clic para seleccionar</div>
								<div class="drop__sub">PDF, JPG, PNG Max: 10MB</div>
							</div>

							<input class="drop__file" type="file" @change="onPickFile" />
						</div>
					</div>

					<div class="actions">
						<button class="btnGhost" type="button" @click="closeNew">Cancelar</button>
						<button class="btnPrimary" type="button" @click="submit">
							<span class="btnPrimary__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('paperPlane')" /></svg>
							</span>
							Enviar Solicitud
						</button>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>
	.page {
		min-height: 100vh;
		padding: 22px 18px 90px;
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
		max-width: 1180px;
		margin: 0 auto;
		border-radius: 22px;
		background: rgba(255, 255, 255, 0.55);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 28px 70px rgba(40, 55, 95, 0.12);
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
		background: rgba(120, 105, 235, 0.9);
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

	/* ✅ FIX: filtros centrados y responsivos */
	.filters {
		margin-top: 14px;
		display: grid;
		gap: 12px;
		align-items: center; /* centra verticalmente las pills */
		grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
	}

	/* Si quieres que el search sea un poquito más ancho cuando haya espacio */
	.pill--search {
		grid-column: span 2;
	}

	/* Evita que en pantallas medianas se rompa feo */
	@media (max-width: 980px) {
		.pill--search {
			grid-column: span 1;
		}
	}

	.pill {
		height: 44px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.62);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		padding: 0 12px;
		display: flex;
		align-items: center; /* ✅ */
		justify-content: flex-start;
		gap: 10px;
		cursor: pointer;
		color: rgba(39, 46, 86, 0.82);
		min-width: 0; /* ✅ evita overflow raro */
	}

	.pill--search {
		cursor: default;
	}

	.pill__icon {
		width: 30px;
		height: 30px;
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
		font-size: 13px;
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

	.tablewrap {
		margin-top: 14px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.58);
		border: 1px solid rgba(110, 102, 182, 0.1);
		box-shadow: 0 22px 44px rgba(40, 55, 95, 0.12);
		overflow: hidden;
		min-height: 420px;
	}

	.table {
		width: 100%;
		border-collapse: separate;
		border-spacing: 0;
	}

	thead th {
		padding: 14px 14px;
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.62);
		text-align: left;
		background: linear-gradient(180deg, rgba(240, 236, 255, 0.7), rgba(255, 255, 255, 0.4));
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
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

	/* -------- Modal -------- */
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(20, 18, 40, 0.16);
		backdrop-filter: blur(10px);
		display: grid;
		place-items: center;
		padding: 18px;
		z-index: 50;
	}

	.modal {
		width: 100%;
		max-width: 360px;
		border-radius: 18px;
		background: linear-gradient(180deg, rgba(255, 255, 255, 0.78), rgba(245, 242, 255, 0.78));
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 30px 80px rgba(40, 55, 95, 0.22);
		overflow: hidden;
	}

	.modal__head {
		padding: 12px 14px 10px;
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
		padding: 10px 14px 14px;
		display: grid;
		gap: 10px;
	}

	.row2 {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 10px;
	}

	.field label {
		display: block;
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.78);
		margin: 0 0 6px;
	}

	.select {
		width: 100%;
		height: 36px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.62);
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 10px;
		cursor: pointer;
	}

	.select__txt {
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.55);
	}

	.select__chev svg {
		width: 14px;
		height: 14px;
		color: rgba(39, 46, 86, 0.38);
	}

	.select__chev path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2.2;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.helper {
		margin-top: 6px;
		font-size: 10px;
		color: rgba(39, 46, 86, 0.42);
	}

	.prio {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 10px;
	}

	.prioCard {
		height: 62px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.62);
		cursor: pointer;
		text-align: left;
		padding: 10px 10px 8px;
		box-shadow: 0 14px 26px rgba(40, 55, 95, 0.08);
	}

	.prioCard--active {
		border-color: rgba(120, 105, 235, 0.5);
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.28), 0 14px 26px rgba(40, 55, 95, 0.08);
	}

	.prioDot {
		width: 10px;
		height: 10px;
		border-radius: 999px;
		margin-bottom: 8px;
	}

	.prioMain {
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.82);
	}

	.prioSub {
		margin-top: 4px;
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
		margin: 0 auto;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: rgba(255, 255, 255, 0.62);
		outline: none;
		font-size: 11px;
		color: rgba(39, 46, 86, 0.86);
	}

	.field input {
		height: 36px;
		padding: 0 10px;
	}

	.field textarea {
		padding: 10px 10px;
		resize: none;
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

	.drop {
		position: relative;
		border-radius: 12px;
		border: 1px dashed rgba(110, 102, 182, 0.22);
		background: rgba(255, 255, 255, 0.5);
		padding: 12px 12px;
		display: flex;
		gap: 10px;
		align-items: center;
	}

	.drop--drag {
		border-color: rgba(120, 105, 235, 0.65);
		box-shadow: 0 0 0 4px rgba(190, 182, 255, 0.28);
	}

	.drop__icon {
		width: 34px;
		height: 34px;
		border-radius: 14px;
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
		font-size: 11px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.75);
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
		margin-top: 6px;
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 10px;
	}

	.btnGhost {
		height: 38px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.6);
		font-weight: 900;
		color: rgba(39, 46, 86, 0.65);
		cursor: pointer;
	}

	.btnPrimary {
		height: 38px;
		border-radius: 12px;
		border: 1px solid rgba(110, 102, 182, 0.1);
		background: linear-gradient(90deg, rgba(90, 80, 220, 0.85), rgba(160, 86, 255, 0.82));
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
		box-shadow: 0 18px 34px rgba(88, 78, 212, 0.18);
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
		.header {
			grid-template-columns: 44px 1fr;
		}

		.headRight {
			grid-column: 1 / -1;
			justify-content: flex-end;
		}
	}

	@media (max-width: 640px) {
		.roleCard {
			display: none;
		}

		.row2 {
			grid-template-columns: 1fr;
		}

		.modal {
			max-width: 420px;
		}
	}
</style>