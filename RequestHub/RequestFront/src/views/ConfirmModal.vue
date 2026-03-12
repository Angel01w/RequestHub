<script setup>
import { computed, watch } from "vue"

const props = defineProps({
	modelValue: {
		type: Boolean,
		default: false
	},
	title: {
		type: String,
		default: "Confirmar acción"
	},
	message: {
		type: String,
		default: "¿Deseas continuar?"
	},
	confirmText: {
		type: String,
		default: "Aceptar"
	},
	cancelText: {
		type: String,
		default: "Cancelar"
	},
	type: {
		type: String,
		default: "default"
	},
	loading: {
		type: Boolean,
		default: false
	},
	closeOnBackdrop: {
		type: Boolean,
		default: true
	},
	closeOnEsc: {
		type: Boolean,
		default: true
	}
})

const emit = defineEmits(["update:modelValue", "confirm", "cancel"])

const isOpen = computed(() => props.modelValue)

const iconName = computed(() => {
	if (props.type === "danger") return "danger"
	if (props.type === "warning") return "warning"
	if (props.type === "success") return "success"
	if (props.type === "info") return "info"
	return "default"
})

function closeModal() {
	if (props.loading) return
	emit("update:modelValue", false)
}

function onCancel() {
	if (props.loading) return
	emit("cancel")
	closeModal()
}

function onConfirm() {
	if (props.loading) return
	emit("confirm")
}

function onBackdrop() {
	if (!props.closeOnBackdrop) return
	onCancel()
}

function onKeydown(event) {
	if (!props.closeOnEsc) return
	if (!isOpen.value) return
	if (event.key === "Escape") {
		onCancel()
	}
}

	watch(
	() => props.modelValue,
	value => {
		if (typeof document === "undefined") return
		document.body.style.overflow = value ? "hidden" : ""
	}
)

if (typeof window !== "undefined") {
	window.addEventListener("keydown", onKeydown)
}
</script>

<template>
    <Teleport to="body">
        <transition name="confirm-modal-fade">
            <div v-if="isOpen" class="confirm-modal-overlay" @click="onBackdrop">
                <div class="confirm-modal" role="dialog" aria-modal="true" :aria-label="title" @click.stop>
                    <div class="confirm-modal__glow"></div>

                    <div class="confirm-modal__header">
                        <div class="confirm-modal__icon" :class="`confirm-modal__icon--${iconName}`">
                            <svg v-if="iconName === 'danger'" viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M12 8v5M12 16.5h.01M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z" />
                            </svg>

                            <svg v-else-if="iconName === 'warning'" viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M12 8v5M12 16.5h.01M10.3 4.8 3.9 16a2 2 0 0 0 1.7 3h12.8a2 2 0 0 0 1.7-3L13.7 4.8a2 2 0 0 0-3.4 0Z" />
                            </svg>

                            <svg v-else-if="iconName === 'success'" viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M7 12.5 10.2 15.7 17 9M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z" />
                            </svg>

                            <svg v-else-if="iconName === 'info'" viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M12 10.5v5M12 7.5h.01M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z" />
                            </svg>

                            <svg v-else viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M12 7v5l3 2M12 3.5a8.5 8.5 0 1 1 0 17 8.5 8.5 0 0 1 0-17Z" />
                            </svg>
                        </div>

                        <div class="confirm-modal__titles">
                            <h3>{{ title }}</h3>
                            <p>{{ message }}</p>
                        </div>
                    </div>

                    <div class="confirm-modal__actions">
                        <button class="confirm-modal__btn confirm-modal__btn--ghost" type="button" :disabled="loading" @click="onCancel">
                            {{ cancelText }}
                        </button>

                        <button class="confirm-modal__btn confirm-modal__btn--primary" :class="`confirm-modal__btn--${iconName}`" type="button" :disabled="loading" @click="onConfirm">
                            <span v-if="loading" class="confirm-modal__spinner"></span>
                            <span>{{ loading ? "Procesando..." : confirmText }}</span>
                        </button>
                    </div>
                </div>
            </div>
        </transition>
    </Teleport>
</template>

<style scoped>
    .confirm-modal-overlay {
        position: fixed;
        inset: 0;
        z-index: 3000;
        display: grid;
        place-items: center;
        padding: 18px;
        background: rgba(20, 18, 40, 0.18);
        backdrop-filter: blur(10px);
    }

    .confirm-modal {
        position: relative;
        width: 100%;
        max-width: 460px;
        border-radius: 24px;
        background: linear-gradient(180deg, rgba(255, 255, 255, 0.88), rgba(245, 242, 255, 0.88));
        border: 1px solid rgba(110, 102, 182, 0.12);
        box-shadow: 0 30px 80px rgba(40, 55, 95, 0.22);
        backdrop-filter: blur(14px);
        padding: 22px;
        overflow: hidden;
    }

    .confirm-modal__glow {
        position: absolute;
        inset: -40% auto auto -10%;
        width: 240px;
        height: 240px;
        background: radial-gradient(circle, rgba(140, 120, 255, 0.18) 0%, rgba(140, 120, 255, 0) 70%);
        pointer-events: none;
    }

    .confirm-modal__header {
        position: relative;
        display: grid;
        grid-template-columns: 52px minmax(0, 1fr);
        gap: 14px;
        align-items: start;
    }

    .confirm-modal__icon {
        width: 52px;
        height: 52px;
        border-radius: 18px;
        display: grid;
        place-items: center;
        border: 1px solid rgba(110, 102, 182, 0.12);
        box-shadow: 0 14px 30px rgba(40, 55, 95, 0.08);
        background: rgba(255, 255, 255, 0.76);
        color: #7a67ff;
    }

        .confirm-modal__icon svg {
            width: 24px;
            height: 24px;
        }

        .confirm-modal__icon path {
            fill: none;
            stroke: currentColor;
            stroke-width: 2.1;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

    .confirm-modal__icon--danger {
        color: rgba(170, 40, 80, 0.98);
        background: rgba(255, 90, 130, 0.12);
    }

    .confirm-modal__icon--warning {
        color: rgba(180, 120, 30, 0.98);
        background: rgba(255, 197, 90, 0.16);
    }

    .confirm-modal__icon--success {
        color: rgba(35, 130, 100, 0.98);
        background: rgba(60, 196, 151, 0.12);
    }

    .confirm-modal__icon--info {
        color: rgba(60, 110, 220, 0.95);
        background: rgba(90, 140, 255, 0.12);
    }

    .confirm-modal__titles {
        min-width: 0;
        padding-top: 2px;
    }

        .confirm-modal__titles h3 {
            margin: 0;
            font-size: 22px;
            font-weight: 900;
            color: #232a52;
            letter-spacing: -0.02em;
            line-height: 1.1;
        }

        .confirm-modal__titles p {
            margin: 8px 0 0;
            font-size: 14px;
            line-height: 1.55;
            color: rgba(39, 46, 86, 0.72);
            word-break: break-word;
        }

    .confirm-modal__actions {
        position: relative;
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 22px;
        flex-wrap: wrap;
    }

    .confirm-modal__btn {
        height: 42px;
        padding: 0 16px;
        border-radius: 14px;
        border: 1px solid rgba(110, 102, 182, 0.12);
        font-size: 13px;
        font-weight: 900;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        transition: transform 0.16s ease, box-shadow 0.16s ease, opacity 0.16s ease;
    }

        .confirm-modal__btn:hover:not(:disabled) {
            transform: translateY(-1px);
        }

        .confirm-modal__btn:disabled {
            opacity: 0.7;
            cursor: not-allowed;
        }

    .confirm-modal__btn--ghost {
        background: rgba(255, 255, 255, 0.72);
        color: rgba(39, 46, 86, 0.72);
        box-shadow: 0 10px 22px rgba(40, 55, 95, 0.05);
    }

    .confirm-modal__btn--primary {
        color: #fff;
        background: linear-gradient(90deg, rgba(90, 80, 220, 0.9), rgba(160, 86, 255, 0.86));
        box-shadow: 0 18px 34px rgba(88, 78, 212, 0.18);
        border-color: rgba(110, 102, 182, 0.1);
    }

    .confirm-modal__btn--danger {
        background: linear-gradient(90deg, rgba(176, 43, 86, 0.95), rgba(220, 78, 118, 0.92));
    }

    .confirm-modal__btn--warning {
        background: linear-gradient(90deg, rgba(198, 136, 35, 0.95), rgba(232, 171, 60, 0.92));
    }

    .confirm-modal__btn--success {
        background: linear-gradient(90deg, rgba(35, 130, 100, 0.95), rgba(60, 196, 151, 0.9));
    }

    .confirm-modal__btn--info {
        background: linear-gradient(90deg, rgba(60, 110, 220, 0.95), rgba(105, 145, 245, 0.9));
    }

    .confirm-modal__spinner {
        width: 14px;
        height: 14px;
        border-radius: 999px;
        border: 2px solid rgba(255, 255, 255, 0.35);
        border-top-color: #fff;
        animation: confirm-modal-spin 0.8s linear infinite;
    }

    .confirm-modal-fade-enter-active,
    .confirm-modal-fade-leave-active {
        transition: opacity 0.18s ease;
    }

        .confirm-modal-fade-enter-active .confirm-modal,
        .confirm-modal-fade-leave-active .confirm-modal {
            transition: transform 0.18s ease, opacity 0.18s ease;
        }

    .confirm-modal-fade-enter-from,
    .confirm-modal-fade-leave-to {
        opacity: 0;
    }

        .confirm-modal-fade-enter-from .confirm-modal,
        .confirm-modal-fade-leave-to .confirm-modal {
            transform: translateY(8px) scale(0.98);
            opacity: 0;
        }

    @keyframes confirm-modal-spin {
        to {
            transform: rotate(360deg);
        }
    }

    @media (max-width: 560px) {
        .confirm-modal {
            padding: 18px;
            border-radius: 20px;
        }

        .confirm-modal__header {
            grid-template-columns: 44px minmax(0, 1fr);
            gap: 12px;
        }

        .confirm-modal__icon {
            width: 44px;
            height: 44px;
            border-radius: 14px;
        }

        .confirm-modal__titles h3 {
            font-size: 19px;
        }

        .confirm-modal__titles p {
            font-size: 13px;
        }

        .confirm-modal__actions {
            display: grid;
            grid-template-columns: 1fr 1fr;
        }

        .confirm-modal__btn {
            width: 100%;
        }
    }
</style>