<script setup>
	import { computed, ref } from "vue";
	import { useRouter } from "vue-router";

	const router = useRouter();

	const search = ref("");
	const hasRequests = computed(() => false); 

	function onBack() {
		router.back();
	}

	function iconPath(name) {
		switch (name) {
			case "arrowLeft":
				return "M15 18l-6-6 6-6";
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3";
			case "chev":
				return "M7 10l5 5 5-5";
			case "calendar":
				return "M7 3v3M17 3v3M4 8h16M5 6h14v15H5V6Z";
			case "clock":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm0-11v5l3 2";
			case "spark":
				return "M12 2l1.4 5.1L18.5 8.5l-5.1 1.4L12 15l-1.4-5.1L5.5 8.5l5.1-1.4L12 2Z";
			case "done":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.2 2.2L16.8 7.8";
			case "ban":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-5-5 10-10";
			case "user":
				return "M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Zm-8 9a8 8 0 0 1 16 0";
			case "eye":
				return "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Zm10 3a3 3 0 1 0-3-3 3 3 0 0 0 3 3Z";
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
					<!-- Blindaje encoding -->
					<h1>Bandeja del &Aacute;rea</h1>
					<p>Gestiona y da seguimiento a las solicitudes del &aacute;rea asignada</p>
				</div>

				<div class="headRight">
					<div class="userCard" aria-label="Usuario y área">
						<div class="userAvatar">J</div>
						<div class="userTxt">
							<div class="userName">&nbsp;</div>
							<div class="userMeta">
								<span class="pillTag">&nbsp;</span>
								<span class="sep"> </span>
								<span class="areaName">&nbsp;</span>
							</div>
						</div>
					</div>
				</div>
			</header>

			<!-- Cards -->
			<div class="cards">
				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--amber" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('spark')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">&nbsp;</div>
							<div class="card__label">Nuevas</div>
						</div>
					</div>
					<div class="card__delta">&nbsp;</div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('clock')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">&nbsp;</div>
							<div class="card__label">En Proceso</div>
						</div>
					</div>
					<div class="card__delta">&nbsp;</div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--teal" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('done')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">&nbsp;</div>
							<div class="card__label">Resueltas</div>
						</div>
					</div>
					<div class="card__delta">&nbsp;</div>
				</div>

				<div class="card">
					<div class="card__left">
						<div class="card__icon card__icon--rose" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath('ban')" /></svg>
						</div>
						<div class="card__nums">
							<div class="card__value">&nbsp;</div>
							<div class="card__label">Rechazadas</div>
						</div>
					</div>
					<div class="card__delta">&nbsp;</div>
				</div>
			</div>

			<!-- Filters -->
			<div class="filters">
				<div class="pill pill--search">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
					</span>
					<input v-model.trim="search"
						   class="pill__input"
						   type="text"
						   placeholder="Buscar por n&uacute;mero o asunto..." />
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</div>

				<button class="pill" type="button">
					<span class="pill__txt">Nuevas y En Proceso</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__txt">&nbsp;</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__txt">Alta, Media, Baja</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
					</span>
					<span class="pill__txt">&Uacute;ltima semana</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
					</span>
				</button>
			</div>

			<!-- Table -->
			<div class="tablewrap" role="region" aria-label="Tabla de solicitudes del área">
				<table class="table" v-if="hasRequests">
					<thead>
						<tr>
							<th>N°</th>
							<th>N&Uacute;MERO SOLICITUD</th>
							<th>ASUNTO</th>
							<th>TIPO</th>
							<th>PRIORIDAD</th>
							<th>ESTADO</th>
							<th>FECHA</th>
							<th class="th-actions">ACCIONES</th>
						</tr>
					</thead>
					<tbody />
				</table>

				<div v-else class="empty">
					<div class="empty__circle" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('user')" /></svg>
					</div>
					<div class="empty__title">No hay solicitudes asignadas</div>
					<div class="empty__sub">
						Cuando existan solicitudes en tu &aacute;rea, podr&aacute;s verlas aqu&iacute;<br />
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

	/* Header */
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

	/* User card */
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
		height: 14px;
	}

	.userMeta {
		display: flex;
		align-items: center;
		gap: 8px;
		margin-top: 4px;
	}

	.pillTag {
		height: 18px;
		min-width: 60px;
		border-radius: 999px;
		border: 1px solid rgba(120,105,235,.0);
		background: rgba(120,105,235,.0);
	}

	.areaName {
		min-width: 90px;
		height: 12px;
	}

	.sep {
		display: inline-block;
		width: 1px;
		height: 14px;
		background: rgba(110,102,182,.14);
	}

	/* Cards */
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

	/* Filters */
	.filters {
		margin-top: 14px;
		display: grid;
		grid-template-columns: 1.25fr .72fr .72fr .72fr .72fr;
		gap: 12px;
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

	/* Table */
	.tablewrap {
		margin-top: 14px;
		border-radius: 18px;
		background: rgba(255,255,255,.58);
		border: 1px solid rgba(110,102,182,.10);
		box-shadow: 0 22px 44px rgba(40,55,95,.12);
		overflow: hidden;
		min-height: 420px;
	}

	.table {
		width: 100%;
		border-collapse: separate;
		border-spacing: 0;
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

	.th-actions {
		text-align: right;
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

	/* Footer + help */
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

	/* Responsive */
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

	@media (max-width: 640px) {
		.userCard {
			display: none;
		}
	}
</style>