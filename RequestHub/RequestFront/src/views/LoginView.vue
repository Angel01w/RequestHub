<script setup>
	import { computed, ref } from "vue"
	import { useRouter } from "vue-router"
	import { useAuthStore } from "../stores/auth"

	const router = useRouter()
	const auth = useAuthStore()

	const roles = [
		{ key: "Solicitante", label: "Solicitante" },
		{ key: "Gestor", label: "Gestor" },
		{ key: "Admin", label: "Administrador" },
		{ key: "SuperAdmin", label: "SuperAdmin" }
	]

	const role = ref("Solicitante")
	const username = ref("")
	const password = ref("")
	const showPassword = ref(false)
	const error = ref("")
	const isSubmitting = ref(false)

	const selectedRoleMap = {
		Solicitante: "Solicitante",
		Gestor: "Gestor",
		Admin: "Admin",
		SuperAdmin: "SuperAdmin"
	}

	const roleHint = computed(() => {
		if (role.value === "Solicitante") return "Crea y da seguimiento a tus solicitudes"
		if (role.value === "Gestor") return "Gestiona solicitudes del área asignada"
		if (role.value === "SuperAdmin") return "Administra usuarios, áreas y supervisa todo el sistema"
		return "Administra catálogos y supervisa las solicitudes del sistema"
	})

	const submit = async () => {
		error.value = ""

		const email = username.value.trim()
		const userPassword = password.value

		if (!email) {
			error.value = "El correo es requerido"
			return
		}

		if (!userPassword.trim()) {
			error.value = "La contraseña es requerida"
			return
		}

		isSubmitting.value = true

		try {
			await auth.login(email, userPassword)

			const expectedRole = selectedRoleMap[role.value]
			const actualRole = auth.role

			if (expectedRole !== actualRole) {
				auth.logout()
				error.value = "El rol seleccionado no coincide con su cuenta"
				return
			}

			router.push("/dashboardview")
		} catch (e) {
			error.value = e?.response?.data?.message || e?.message || "No se pudo iniciar sesión"
		} finally {
			isSubmitting.value = false
		}
	}
</script>

<template>
	<div class="login-screen">
		<div class="login-wrap">
			<header class="brand">
				<div class="brand-mark" aria-hidden="true">
					<img src="../../img/RequestHub.png" width="90" height="60" />
					<svg class="brand-svg brand-svg--hidden" viewBox="0 0 64 64" aria-hidden="true">
						<defs>
							<linearGradient id="gBrand" x1="0" y1="0" x2="1" y2="1">
								<stop offset="0" stop-color="#3E67FF" />
								<stop offset="1" stop-color="#9B3CFF" />
							</linearGradient>
						</defs>
						<path fill="url(#gBrand)"
							  d="M12 10c0-4.4 3.6-8 8-8h16c10.5 0 19 8.5 19 19 0 8.2-5.2 15.2-12.6 17.8L55 58c1.6 2.4.4 5.7-2.3 6.6-1.9.7-3.9 0-5-1.5L32.6 41H20c-4.4 0-8-3.6-8-8V10Zm8 0v23h17.1c5.5 0 9.9-4.4 9.9-9.9S42.6 13.2 37.1 13.2H20Z"
							  opacity="0.95" />
						<path fill="url(#gBrand)"
							  d="M32 34h11.5c3.6 0 6.5 2.9 6.5 6.5V41c0 3.6-2.9 6.5-6.5 6.5H36l8.7 12.1c.9 1.3.6 3.1-.7 4.1-1.3.9-3.1.6-4.1-.7L28.8 48.1c-1-1.4 0-3.4 1.7-3.4H32V34Z"
							  opacity="0.85" />
					</svg>
				</div>

				<h1 class="brand-title">RequestHub</h1>
			</header>

			<p class="subtitle">Sistema de Gestión de Solicitudes Internas</p>

			<section class="panel" aria-label="Login">
				<div class="section-title">
					<h2>Seleccionar Rol</h2>
					<span class="section-line" aria-hidden="true"></span>
				</div>

				<div class="roles" role="tablist" aria-label="Roles">
					<button v-for="r in roles"
							:key="r.key"
							type="button"
							class="role-card"
							:class="{ active: role === r.key }"
							role="tab"
							:aria-selected="role === r.key"
							@click="role = r.key">
						<span class="role-icon" aria-hidden="true">
							<svg v-if="r.key === 'Solicitante'" viewBox="0 0 24 24">
								<path d="M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Z"
									  fill="none"
									  stroke="currentColor"
									  stroke-width="2"
									  stroke-linecap="round"
									  stroke-linejoin="round" />
								<path d="M4 20a8 8 0 0 1 16 0"
									  fill="none"
									  stroke="currentColor"
									  stroke-width="2"
									  stroke-linecap="round" />
							</svg>

							<svg v-else-if="r.key === 'Gestor'" viewBox="0 0 24 24">
								<path d="M9 5h6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
								<path d="M9 3h6a2 2 0 0 1 2 2v16H7V5a2 2 0 0 1 2-2Z"
									  fill="none"
									  stroke="currentColor"
									  stroke-width="2"
									  stroke-linejoin="round" />
								<path d="M9 11h6M9 15h6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
							</svg>

							<svg v-else viewBox="0 0 24 24">
								<path d="M12 3 19 6v7c0 5-3 8-7 9-4-1-7-4-7-9V6l7-3Z"
									  fill="none"
									  stroke="currentColor"
									  stroke-width="2"
									  stroke-linejoin="round" />
							</svg>
						</span>

						<span class="role-label">{{ r.label }}</span>
					</button>
				</div>

				<p class="hint">{{ roleHint }}</p>

				<form class="form" @submit.prevent="submit">
					<div class="field">
						<label class="label" for="email">Correo Electrónico</label>

						<div class="control">
							<span class="control-icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path d="M4 6h16v12H4z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
									<path d="m4 7 8 6 8-6"
										  fill="none"
										  stroke="currentColor"
										  stroke-width="2"
										  stroke-linecap="round"
										  stroke-linejoin="round" />
								</svg>
							</span>

							<input id="email"
								   class="control-input"
								   v-model.trim="username"
								   type="email"
								   inputmode="email"
								   autocomplete="username"
								   placeholder="usuario@empresa.com"
								   :disabled="isSubmitting" />
						</div>
					</div>

					<div class="field">
						<label class="label" for="password">Contraseña</label>

						<div class="control">
							<span class="control-icon" aria-hidden="true">
								<svg viewBox="0 0 24 24">
									<path d="M7 11V8a5 5 0 0 1 10 0v3"
										  fill="none"
										  stroke="currentColor"
										  stroke-width="2"
										  stroke-linecap="round" />
									<path d="M6 11h12v10H6z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
								</svg>
							</span>

							<input id="password"
								   class="control-input"
								   v-model="password"
								   :type="showPassword ? 'text' : 'password'"
								   autocomplete="current-password"
								   placeholder="••••••••"
								   :disabled="isSubmitting" />

							<button class="eye"
									type="button"
									:aria-label="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
									@click="showPassword = !showPassword"
									:disabled="isSubmitting">
								<svg v-if="!showPassword" viewBox="0 0 24 24" aria-hidden="true">
									<path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"
										  fill="none"
										  stroke="currentColor"
										  stroke-width="2"
										  stroke-linejoin="round" />
									<path d="M12 15a3 3 0 1 0-3-3 3 3 0 0 0 3 3Z" fill="none" stroke="currentColor" stroke-width="2" />
								</svg>
								<svg v-else viewBox="0 0 24 24" aria-hidden="true">
									<path d="M3 3l18 18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
									<path d="M6.2 6.2C3.9 7.7 2 12 2 12s4 7 10 7c2 0 3.8-.6 5.3-1.5"
										  fill="none"
										  stroke="currentColor"
										  stroke-width="2"
										  stroke-linecap="round"
										  stroke-linejoin="round" />
									<path d="M14.6 9.4A3 3 0 0 1 15 12a3 3 0 0 1-3 3 3 3 0 0 1-2.6-1.4"
										  fill="none"
										  stroke="currentColor"
										  stroke-width="2"
										  stroke-linecap="round"
										  stroke-linejoin="round" />
								</svg>
							</button>
						</div>
					</div>

					<button class="submit" type="submit" :disabled="isSubmitting">
						<span>{{ isSubmitting ? "Ingresando..." : "Iniciar Sesión" }}</span>
					</button>

					<p v-if="error" class="error" role="alert">{{ error }}</p>
				</form>

				<p class="footnote">
					Selecciona un rol y haz clic en <a href="#" @click.prevent>Iniciar Sesión</a>
				</p>
			</section>
		</div>
	</div>
</template>

<style scoped>
	.login-screen {
		min-height: 100vh;
		display: grid;
		place-items: center;
		padding: 26px 16px;
		background: radial-gradient(780px 420px at 18% 12%, rgba(80, 120, 255, 0.22), rgba(255, 255, 255, 0) 62%), radial-gradient(820px 520px at 82% 86%, rgba(170, 75, 255, 0.18), rgba(255, 255, 255, 0) 62%), linear-gradient(160deg, #f7f9ff 0%, #e8eeff 100%);
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, "Apple Color Emoji", "Segoe UI Emoji";
	}

	.login-wrap {
		width: 100%;
		max-width: 380px;
	}

	.brand {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
		margin-bottom: 10px;
	}

	.brand-mark {
		width: 56px;
		height: 56px;
		display: grid;
		place-items: center;
	}

	.brand-svg {
		width: 56px;
		height: 56px;
		filter: drop-shadow(0 16px 18px rgba(65, 87, 255, 0.18));
	}

	.brand-svg--hidden {
		display: none;
	}

	.brand-img {
		width: 56px;
		height: 56px;
		object-fit: contain;
		filter: drop-shadow(0 16px 18px rgba(65, 87, 255, 0.18));
	}

	.brand-title {
		margin: 0;
		font-size: 30px;
		font-weight: 900;
		letter-spacing: -0.4px;
		color: #273251;
	}

	.subtitle {
		margin: 0 0 18px;
		text-align: center;
		font-size: 12px;
		color: #6d7897;
	}

	.panel {
		position: relative;
		border-radius: 26px;
		padding: 18px 18px 16px;
		background: linear-gradient(180deg, rgba(255, 255, 255, 0.95), rgba(246, 249, 255, 0.95));
		border: 1px solid rgba(207, 218, 247, 0.95);
		box-shadow: 0 34px 80px rgba(40, 55, 95, 0.18), 0 1px 0 rgba(255, 255, 255, 0.85) inset;
		overflow: hidden;
	}

		.panel::before {
			content: "";
			position: absolute;
			inset: -40px -60px auto auto;
			width: 380px;
			height: 260px;
			background: linear-gradient(135deg, rgba(63, 88, 255, 0.1), rgba(154, 60, 255, 0.08), rgba(255, 255, 255, 0) 60%);
			transform: rotate(-8deg);
			pointer-events: none;
		}

	.section-title {
		display: flex;
		align-items: center;
		gap: 10px;
		margin: 4px 0 12px;
	}

		.section-title h2 {
			margin: 0;
			font-size: 11px;
			font-weight: 900;
			color: #3e4a6c;
		}

	.section-line {
		height: 1px;
		flex: 1;
		background: rgba(220, 228, 255, 0.95);
	}

	.roles {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 10px;
	}

	.role-card {
		height: 66px;
		border-radius: 14px;
		border: 1px solid rgba(210, 221, 248, 0.95);
		background: linear-gradient(180deg, #fbfcff, #f3f6ff);
		display: grid;
		place-items: center;
		gap: 6px;
		padding: 10px 8px 8px;
		cursor: pointer;
		color: #5d6a8a;
		box-shadow: 0 8px 18px rgba(36, 52, 92, 0.06);
		transition: transform 120ms ease, box-shadow 120ms ease, border-color 120ms ease;
	}

		.role-card:active {
			transform: translateY(1px);
		}

	.role-icon svg {
		width: 22px;
		height: 22px;
		color: #6c7897;
	}

	.role-label {
		font-size: 11px;
		font-weight: 900;
		line-height: 1;
	}

	.role-card.active {
		border-color: transparent;
		color: #fff;
		background: radial-gradient(circle at 20% 15%, rgba(255, 255, 255, 0.22), rgba(255, 255, 255, 0) 52%), linear-gradient(180deg, #6b54f3 0%, #2f25cf 100%);
		box-shadow: 0 18px 28px rgba(88, 78, 212, 0.38);
		position: relative;
		overflow: hidden;
	}

		.role-card.active::after {
			content: "";
			position: absolute;
			inset: 0;
			background: linear-gradient(135deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0) 48%);
			pointer-events: none;
		}

		.role-card.active .role-icon svg {
			color: #fff;
		}

	.hint {
		margin: 12px 0 14px;
		text-align: center;
		font-size: 11px;
		color: #6f7aa0;
	}

	.form {
		display: grid;
		gap: 12px;
	}

	.field {
		display: grid;
		gap: 6px;
	}

	.label {
		font-size: 12px;
		font-weight: 900;
		color: #3f4b6c;
	}

	.control {
		height: 44px;
		border-radius: 14px;
		border: 1px solid rgba(214, 223, 248, 0.95);
		background: rgba(255, 255, 255, 0.98);
		box-shadow: 0 10px 20px rgba(36, 52, 92, 0.06);
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 12px 0 10px;
	}

		.control:focus-within {
			border-color: rgba(120, 140, 255, 0.95);
			box-shadow: 0 0 0 4px rgba(220, 226, 255, 0.95), 0 10px 20px rgba(36, 52, 92, 0.06);
		}

	.control-icon {
		width: 30px;
		height: 30px;
		border-radius: 10px;
		border: 1px solid rgba(214, 223, 248, 0.95);
		background: rgba(255, 255, 255, 0.96);
		display: grid;
		place-items: center;
		color: #7a86a6;
		flex: 0 0 auto;
	}

		.control-icon svg {
			width: 16px;
			height: 16px;
		}

	.control-input {
		border: none;
		background: transparent;
		outline: none;
		height: 100%;
		flex: 1 1 auto;
		min-width: 0;
		font-size: 12px;
		color: #2c3857;
		padding: 0;
		line-height: 44px;
	}

		.control-input::placeholder {
			color: #9aa6c2;
		}

	.eye {
		width: 30px;
		height: 30px;
		border-radius: 999px;
		border: 1px solid rgba(214, 223, 248, 0.95);
		background: rgba(255, 255, 255, 0.96);
		display: grid;
		place-items: center;
		cursor: pointer;
		color: #7a86a6;
		flex: 0 0 auto;
	}

		.eye svg {
			width: 16px;
			height: 16px;
		}

	.submit {
		margin-top: 4px;
		height: 44px;
		border-radius: 14px;
		border: none;
		cursor: pointer;
		color: #fff;
		font-weight: 900;
		font-size: 13px;
		background: linear-gradient(90deg, #3e53f0 0%, #9130f0 100%);
		box-shadow: 0 18px 28px rgba(70, 83, 240, 0.22);
		position: relative;
		overflow: hidden;
	}

		.submit::after {
			content: "";
			position: absolute;
			inset: 0;
			background: linear-gradient(135deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0) 55%);
			pointer-events: none;
		}

		.submit:active {
			transform: translateY(1px);
		}

		.submit:disabled,
		.eye:disabled,
		.control-input:disabled,
		.role-card:disabled {
			cursor: not-allowed;
			opacity: 0.7;
		}

	.error {
		margin: 6px 0 0;
		text-align: center;
		color: #d3204c;
		font-size: 12px;
		font-weight: 900;
	}

	.footnote {
		margin: 12px 0 0;
		text-align: center;
		font-size: 11px;
		color: #7a86a6;
	}

		.footnote a {
			color: #3f58ff;
			font-weight: 900;
			text-decoration: none;
		}

			.footnote a:hover {
				text-decoration: underline;
			}
</style>