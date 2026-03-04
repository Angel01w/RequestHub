<script setup>
	import { computed } from "vue";
	import { useRoute, useRouter } from "vue-router";

	const router = useRouter();
	const route = useRoute();

	const menu = computed(() => [
		{ key: "dashboard", label: "Dashboard", sub: "Vista general", to: "/dashboard", icon: "grid" },
		{ key: "my", label: "Mis Solicitudes", to: "/mis-solicitudes", icon: "list" },
		{ key: "area", label: "Bandeja del Área", to: "/bandeja", icon: "inbox" },
		{ key: "admin", label: "Administración", to: "/admin/catalogos", icon: "gear" },
	]);

	const activeKey = computed(() => {
		const p = route.path;
		if (p.startsWith("/dashboard")) return "dashboard";
		if (p.startsWith("/mis-solicitudes")) return "my";
		if (p.startsWith("/bandeja")) return "area";
		if (p.startsWith("/admin")) return "admin";
		return "dashboard";
	});

	function iconPath(name) {
		switch (name) {
			case "grid":
				return "M4 4h7v7H4V4Zm9 0h7v7h-7V4ZM4 13h7v7H4v-7Zm9 0h7v7h-7v-7Z";
			case "list":
				return "M6 7h14M6 12h14M6 17h14M4 7h.01M4 12h.01M4 17h.01";
			case "inbox":
				return "M4 4h16v12H4V4Zm0 12h5l1.5 2h3L15 16h5v4H4v-4Z";
			case "gear":
				return "M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM19.4 12a7.4 7.4 0 0 0-.1-1l2-1.6-2-3.4-2.4 1a7.6 7.6 0 0 0-1.7-1l-.4-2.6H9.2L8.8 6a7.6 7.6 0 0 0-1.7 1l-2.4-1-2 3.4 2 1.6a7.4 7.4 0 0 0 0 2l-2 1.6 2 3.4 2.4-1c.5.4 1.1.7 1.7 1l.4 2.6h5.6l.4-2.6c.6-.3 1.2-.6 1.7-1l2.4 1 2-3.4-2-1.6c.1-.3.1-.6.1-1Z";
			case "bell":
				return "M18 8a6 6 0 1 0-12 0c0 7-3 7-3 7h18s-3 0-3-7Zm-8 11a2 2 0 0 0 4 0";
			case "chev":
				return "M7 10l5 5 5-5";
			case "search":
				return "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Zm6.2-1.3 4.3 4.3";
			case "calendar":
				return "M7 3v3M17 3v3M4 8h16M5 6h14v15H5V6Z";
			case "spark":
				return "M12 2l1.4 5.1L18.5 8.5l-5.1 1.4L12 15l-1.4-5.1L5.5 8.5l5.1-1.4L12 2Z";
			case "clock":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm0-11v5l3 2";
			case "done":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.2 2.2L16.8 7.8";
			case "check":
				return "M12 21a9 9 0 1 0-9-9 9 9 0 0 0 9 9Zm-4-9 2.3 2.3L16.5 8";
			default:
				return "";
		}
	}

	function go(to) {
		router.push(to);
	}
</script>

<template>
	<div class="dash">
		<div class="layout">
			<aside class="sidebar">
				<div class="brand">
					<div class="brand__logo">
						<span class="brand__letter">R</span>
					</div>
					<div class="brand__txt">
						<div class="brand__title">RequestHub</div>
						<div class="brand__sub">Mesa de Servicios</div>
					</div>
				</div>

				<nav class="nav">
					<button v-for="item in menu"
							:key="item.key"
							class="navitem"
							:class="{ 'navitem--active': activeKey === item.key }"
							type="button"
							@click="go(item.to)">
						<span class="navitem__icon" aria-hidden="true">
							<svg viewBox="0 0 24 24"><path :d="iconPath(item.icon)" /></svg>
						</span>
						<span class="navitem__text">
							<span class="navitem__label">{{ item.label }}</span>
							<span v-if="item.sub" class="navitem__sub">{{ item.sub }}</span>
						</span>
					</button>
				</nav>

				<div class="spacer"></div>

				<button class="logout" type="button" @click="go('/login')">
					<span class="logout__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M10 17v2H5V5h5v2M14 7l5 5-5 5M19 12H10" /></svg>
					</span>
					<span>Cerrar Sesión</span>
				</button>
			</aside>

			<div class="main">
				<header class="topbar">
					<div class="topbar__left">
						<img class="topbar__logo" src="../../img/RequestHub.png" alt="RequestHub" />
					</div>

					<div class="topbar__right">
						<button class="iconbtn" type="button" aria-label="Notificaciones">
							<span class="dot" aria-hidden="true"></span>
							<svg viewBox="0 0 24 24"><path :d="iconPath('bell')" /></svg>
						</button>

						<div class="user">
							<div class="user__meta">
								<div class="user__name">&nbsp;</div>
								<div class="user__role">&nbsp;</div>
							</div>
							<div class="avatar"> </div>
							<span class="chev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</div>
					</div>
				</header>

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

					<!-- Cards (sin números de relleno) -->
					<div class="cards">
						<div class="card">
							<div class="card__left">
								<div class="card__icon" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('check')" /></svg>
								</div>
								<div class="card__nums">
									<div class="card__value">&nbsp;</div>
									<div class="card__label">Total</div>
								</div>
							</div>
							<div class="card__delta">&nbsp;</div>
						</div>

						<div class="card">
							<div class="card__left">
								<div class="card__icon card__icon--amber" aria-hidden="true">
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
								<div class="card__icon card__icon--violet" aria-hidden="true">
									<svg viewBox="0 0 24 24"><path :d="iconPath('spark')" /></svg>
								</div>
								<div class="card__nums">
									<div class="card__value">&nbsp;</div>
									<div class="card__label">Nuevas</div>
								</div>
							</div>
							<div class="card__delta">&nbsp;</div>
						</div>
					</div>

					<!-- Filters (solo UI) -->
					<div class="filters">
						<div class="pill pill--search">
							<span class="pill__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('search')" /></svg>
							</span>
							<input class="pill__input" type="text" placeholder="Buscar por número o asunto..." />
							<span class="pill__chev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</div>

						<button class="pill" type="button">
							<span class="pill__txt">Todos los Estados</span>
							<span class="pill__chev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</button>

						<button class="pill" type="button">
							<span class="pill__txt">Todos las Áreas</span>
							<span class="pill__chev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</button>

						<button class="pill" type="button">
							<span class="pill__icon" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
							</span>
							<span class="pill__txt">Todas las fechas</span>
							<span class="pill__chev" aria-hidden="true">
								<svg viewBox="0 0 24 24"><path :d="iconPath('chev')" /></svg>
							</span>
						</button>
					</div>

					<!-- Table (sin filas de relleno) -->
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
							<tbody>
								<!-- sin rows por ahora -->
							</tbody>
						</table>
					</div>
				</section>

				<button class="help" type="button" aria-label="Ayuda">?</button>
			</div>
		</div>
	</div>
</template>

<style scoped>
	.dash {
		min-height: 100vh;
		background: radial-gradient(900px 520px at 20% 18%, rgba(116, 92, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(860px 520px at 85% 85%, rgba(179, 94, 255, 0.20), rgba(255, 255, 255, 0) 62%), linear-gradient(180deg, #f7f7ff 0%, #ece9ff 55%, #e9e7ff 100%);
		position: relative;
		overflow: hidden;
	}

		.dash::before {
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

	.layout {
		position: relative;
		display: grid;
		grid-template-columns: 270px 1fr;
		min-height: 100vh;
	}

	.sidebar {
		padding: 20px 16px;
		background: rgba(255, 255, 255, 0.58);
		border-right: 1px solid rgba(110, 102, 182, 0.10);
		backdrop-filter: blur(10px);
		display: flex;
		flex-direction: column;
		gap: 16px;
	}

	.brand {
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 10px 10px;
		border-radius: 18px;
	}

	.brand__logo {
		width: 44px;
		height: 44px;
		border-radius: 16px;
		background: linear-gradient(135deg, #3f58ff 0%, #9a3cff 100%);
		display: grid;
		place-items: center;
		box-shadow: 0 18px 30px rgba(88, 78, 212, 0.22);
	}

	.brand__letter {
		color: #fff;
		font-weight: 900;
		font-size: 18px;
	}

	.brand__title {
		font-weight: 900;
		color: #232a52;
		letter-spacing: -0.2px;
	}

	.brand__sub {
		font-size: 12px;
		color: rgba(39, 46, 86, 0.70);
		margin-top: 1px;
	}

	.nav {
		display: grid;
		gap: 10px;
		padding: 6px 4px;
	}

	.navitem {
		border: none;
		text-align: left;
		cursor: pointer;
		padding: 10px 10px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.42);
		border: 1px solid rgba(110, 102, 182, 0.08);
		display: grid;
		grid-template-columns: 38px 1fr;
		gap: 10px;
		align-items: center;
		color: #2a2f57;
		box-shadow: 0 12px 24px rgba(40, 55, 95, 0.06);
	}

	.navitem__icon {
		width: 34px;
		height: 34px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.12);
		display: grid;
		place-items: center;
		color: #5b4df0;
	}

		.navitem__icon svg {
			width: 18px;
			height: 18px;
		}

		.navitem__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.navitem__label {
		font-weight: 900;
		font-size: 13px;
		line-height: 1.15;
	}

	.navitem__sub {
		display: block;
		font-size: 11px;
		color: rgba(39, 46, 86, 0.62);
		margin-top: 2px;
	}

	.navitem--active {
		background: linear-gradient(135deg, rgba(99, 80, 255, 0.26), rgba(163, 88, 255, 0.18));
		border-color: rgba(120, 105, 235, 0.18);
	}

	.spacer {
		flex: 1;
	}

	.logout {
		border: none;
		cursor: pointer;
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 12px 10px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.42);
		border: 1px solid rgba(110, 102, 182, 0.08);
		color: rgba(43, 50, 92, 0.88);
		font-weight: 900;
		box-shadow: 0 12px 24px rgba(40, 55, 95, 0.06);
	}

	.logout__icon {
		width: 34px;
		height: 34px;
		border-radius: 12px;
		background: rgba(120, 105, 235, 0.12);
		display: grid;
		place-items: center;
		color: #5b4df0;
	}

		.logout__icon svg {
			width: 18px;
			height: 18px;
		}

		.logout__icon path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.main {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.topbar {
		height: 64px;
		padding: 12px 18px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		background: rgba(255, 255, 255, 0.40);
		border-bottom: 1px solid rgba(110, 102, 182, 0.10);
		backdrop-filter: blur(10px);
	}

	.topbar__logo {
		height: 28px;
		width: auto;
		opacity: 0.95;
	}

	.topbar__right {
		display: flex;
		align-items: center;
		gap: 14px;
	}

	.iconbtn {
		width: 42px;
		height: 42px;
		border-radius: 14px;
		border: 1px solid rgba(110, 102, 182, 0.12);
		background: rgba(255, 255, 255, 0.55);
		display: grid;
		place-items: center;
		cursor: pointer;
		position: relative;
		box-shadow: 0 14px 26px rgba(40, 55, 95, 0.08);
		color: #6d64d6;
	}

		.iconbtn svg {
			width: 18px;
			height: 18px;
		}

		.iconbtn path {
			fill: none;
			stroke: currentColor;
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.dot {
		position: absolute;
		top: 10px;
		right: 12px;
		width: 7px;
		height: 7px;
		border-radius: 99px;
		background: #ff6aa2;
		box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.7);
	}

	.user {
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 6px 10px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.55);
		border: 1px solid rgba(110, 102, 182, 0.12);
		box-shadow: 0 14px 26px rgba(40, 55, 95, 0.08);
		min-width: 170px;
	}

	.user__meta {
		display: grid;
		gap: 2px;
		padding-right: 6px;
		width: 110px;
	}

	.user__name {
		font-weight: 900;
		font-size: 13px;
		color: #232a52;
		line-height: 1;
	}

	.user__role {
		font-size: 11px;
		color: rgba(39, 46, 86, 0.0);
		background: rgba(120, 105, 235, 0.0);
		border: 1px solid rgba(120, 105, 235, 0.0);
		padding: 2px 8px;
		border-radius: 999px;
		width: fit-content;
	}

	.avatar {
		width: 36px;
		height: 36px;
		border-radius: 14px;
		background: linear-gradient(135deg, #7a67ff, #a053ff);
	}

	.chev svg {
		width: 16px;
		height: 16px;
		color: rgba(39, 46, 86, 0.50);
	}

	.chev path {
		fill: none;
		stroke: currentColor;
		stroke-width: 2;
		stroke-linecap: round;
		stroke-linejoin: round;
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
		border: 1px solid rgba(110, 102, 182, 0.10);
		box-shadow: 0 18px 34px rgba(40, 55, 95, 0.10);
		padding: 14px 14px;
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
		grid-template-columns: 1.45fr 0.75fr 0.75fr 0.75fr;
		gap: 10px;
	}

	.pill {
		height: 42px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.62);
		border: 1px solid rgba(110, 102, 182, 0.10);
		box-shadow: 0 16px 30px rgba(40, 55, 95, 0.08);
		padding: 0 12px;
		display: flex;
		align-items: center;
		gap: 10px;
		cursor: pointer;
		color: rgba(39, 46, 86, 0.82);
	}

	.pill--search {
		cursor: default;
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

	.pill__input {
		border: none;
		outline: none;
		background: transparent;
		flex: 1 1 auto;
		min-width: 0;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
	}

		.pill__input::placeholder {
			color: rgba(39, 46, 86, 0.40);
		}

	.pill__txt {
		font-size: 12px;
		font-weight: 900;
		color: rgba(39, 46, 86, 0.78);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.pill__chev {
		margin-left: auto;
		color: rgba(39, 46, 86, 0.38);
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
			stroke-width: 2;
			stroke-linecap: round;
			stroke-linejoin: round;
		}

	.tablewrap {
		margin-top: 12px;
		border-radius: 18px;
		background: rgba(255, 255, 255, 0.58);
		border: 1px solid rgba(110, 102, 182, 0.10);
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
		background: linear-gradient(180deg, rgba(240, 236, 255, 0.70), rgba(255, 255, 255, 0.40));
		border-bottom: 1px solid rgba(110, 102, 182, 0.10);
	}

	tbody td {
		padding: 12px 14px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		border-bottom: 1px solid rgba(110, 102, 182, 0.10);
	}

	.th-actions {
		text-align: right;
	}

	.help {
		position: fixed;
		right: 18px;
		bottom: 18px;
		width: 40px;
		height: 40px;
		border-radius: 16px;
		border: 1px solid rgba(110, 102, 182, 0.16);
		background: rgba(85, 76, 210, 0.70);
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 18px 34px rgba(40, 55, 95, 0.20);
	}

	@media (max-width: 1100px) {
		.cards {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}

		.filters {
			grid-template-columns: 1fr 1fr;
		}

		.layout {
			grid-template-columns: 250px 1fr;
		}
	}

	@media (max-width: 860px) {
		.layout {
			grid-template-columns: 1fr;
		}

		.sidebar {
			display: none;
		}

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