<!-- src/views/DashboardView.vue -->
<script setup>
	function iconPath(name) {
		switch (name) {
			case "grid":
				return "M4 4h7v7H4V4Zm9 0h7v7h-7V4ZM4 13h7v7H4v-7Zm9 0h7v7h-7v-7Z";
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
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</div>

				<button class="pill" type="button">
					<span class="pill__txt">Todos los Estados</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__txt">Todos las Áreas</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
					</span>
				</button>

				<button class="pill" type="button">
					<span class="pill__icon" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path :d="iconPath('calendar')" /></svg>
					</span>
					<span class="pill__txt">Todas las fechas</span>
					<span class="pill__chev" aria-hidden="true">
						<svg viewBox="0 0 24 24"><path d="M7 10l5 5 5-5" /></svg>
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
					<tbody />
				</table>
			</div>
		</section>

		<button class="help" type="button" aria-label="Ayuda">?</button>
	</div>
</template>

<style scoped>
	/* Contenedor del view dentro del layout de App.vue */
	.dash-content {
		min-height: 100vh;
		position: relative;
	}

	/* Igual que antes (contenido) */
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
		border: 1px solid rgba(110, 102, 182, 0.1);
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
			color: rgba(39, 46, 86, 0.4);
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
		padding: 12px 14px;
		font-size: 12px;
		color: rgba(39, 46, 86, 0.86);
		border-bottom: 1px solid rgba(110, 102, 182, 0.1);
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
		background: rgba(85, 76, 210, 0.7);
		color: #fff;
		font-weight: 900;
		cursor: pointer;
		box-shadow: 0 18px 34px rgba(40, 55, 95, 0.2);
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
	}
</style>