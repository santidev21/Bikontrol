// Spanish dictionary (default + fallback). Flat keys grouped by surface.
export const es: Record<string, string> = {
  'app.name': 'Bikontrol',

  // Common
  'common.email': 'Correo',
  'common.emailPlaceholder': 'correo@example.com',
  'common.password': 'Contraseña', // NOSONAR: i18n label, not a credential
  'common.newPassword': 'Nueva contraseña',
  'common.confirmPassword': 'Confirmar contraseña',
  'common.emailRequired': 'El correo es obligatorio.',
  'common.emailInvalid': 'Ingresa un correo válido.',
  'common.passwordRequired': 'La contraseña es obligatoria.',
  'common.passwordMin': 'Debe tener al menos 6 caracteres.',
  'common.confirmRequired': 'Debes confirmar la contraseña.',
  'common.passwordMismatch': 'Las contraseñas no coinciden.',
  'common.backToLogin': 'Volver al inicio de sesión',
  'common.help': 'Ayuda',
  'common.terms': 'Términos',
  'common.privacy': 'Privacidad',

  // Landing
  'landing.help': 'Ayuda',
  'landing.signIn': 'Ingresar',
  'landing.createAccount': 'Crear cuenta',
  'landing.hero.title': 'El mantenimiento de tu moto, bajo control.',
  'landing.hero.subtitle':
    'Registrá tus motos, seguí cada service y recibí un aviso antes de que venza. Simple, privado y gratis para dueños de motos.',
  'landing.cta.start': 'Empezar gratis',
  'landing.cta.haveAccount': 'Ya tengo cuenta',
  'landing.cta.demo': 'Probar la demo',
  'landing.cta.demoLoading': 'Cargando demo…',
  'landing.demoError': 'No se pudo iniciar la demo.',
  'landing.feature.reminders.title': 'No te olvides más del service',
  'landing.feature.reminders.desc':
    'Recordatorios por kilometraje o por tiempo, antes de que venza.',
  'landing.feature.history.title': 'Todo tu historial en un lugar',
  'landing.feature.history.desc': 'Qué le hiciste, cuándo y a qué kilometraje, siempre a mano.',
  'landing.feature.costs.title': 'Sabés cuánto gastás',
  'landing.feature.costs.desc': 'Costo por moto, por año y por kilómetro.',
  'landing.feature.data.title': 'Tus datos, tuyos',
  'landing.feature.data.desc': 'Descargá todo cuando quieras y borrá tu cuenta en un clic.',
  'landing.how.title': 'Cómo funciona',
  'landing.step.account.title': 'Creá tu cuenta',
  'landing.step.account.desc': 'En un minuto, con tu correo o con Google.',
  'landing.step.plan.title': 'Cargá tu moto y tu plan',
  'landing.step.plan.desc': 'Elegí tus mantenimientos; te sugerimos los esenciales.',
  'landing.step.alerts.title': 'Recibí los avisos',
  'landing.step.alerts.desc': 'Por email y notificaciones, antes de que toque el service.',
  'landing.trust.title': 'Pensado para que confíes tus datos',
  'landing.trust.backups': 'Backups verificados de la base de datos.',
  'landing.trust.data': 'Cuenta y datos tuyos: los descargás o los borrás cuando quieras.',
  'landing.trust.noAds': 'Sin publicidad ni venta de tus datos.',
  'landing.final.title': 'Empezá en 2 minutos',
  'landing.final.subtitle': 'Cargá tu primer moto y tu plan de mantenimiento hoy mismo.',
  'landing.final.button': 'Crear mi cuenta',
  'landing.footer.tagline': 'Bikontrol — mantenimiento de motos para dueños.',
  'landing.footer.help': 'Ayuda',
  'landing.footer.terms': 'Términos',
  'landing.footer.privacy': 'Privacidad',

  // Login
  'auth.login.forgot': '¿Olvidaste tu contraseña?',
  'auth.login.submit': 'Ingresar',
  'auth.login.orContinue': 'o continuar con',
  'auth.login.register': 'Registrarse',
  'auth.login.demo': '¿Querés probar la demo?',
  'auth.login.demoLoading': 'Cargando demo...',
  'auth.login.resend': 'Reenviar correo de confirmación',
  'auth.login.resendLoading': 'Enviando...',
  'auth.login.resendSent': 'Te enviamos un nuevo enlace de confirmación.',
  'auth.login.googleError': 'No se pudo obtener la credencial de Google.',
  'auth.login.demoError': 'No se pudo iniciar la demo.',

  // Register
  'auth.register.title': 'Crear cuenta',
  'auth.register.fullName': 'Nombre completo',
  'auth.register.fullNamePlaceholder': 'Ej: Juan Pérez',
  'auth.register.confirmSent':
    'Te enviamos un correo de confirmación. Revisá tu bandeja (y la carpeta de spam) y hacé clic en el enlace para activar tu cuenta.',
  'auth.register.submit': 'Registrarse',

  // Forgot password
  'auth.forgot.title': 'Recuperar contraseña',
  'auth.forgot.subtitle':
    'Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña.',
  'auth.forgot.submit': 'Enviar enlace',
  'auth.forgot.success':
    'Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.',

  // Reset password
  'auth.reset.title': 'Nueva contraseña',
  'auth.reset.linkInvalid':
    'El enlace de recuperación es inválido o está incompleto. Solicita uno nuevo.',
  'auth.reset.submit': 'Restablecer contraseña',
  'auth.reset.success': 'Contraseña actualizada. Ya puedes iniciar sesión.',

  // Confirm email
  'auth.confirm.title': 'Confirmar correo',
  'auth.confirm.loading': 'Confirmando tu correo...',
  'auth.confirm.linkInvalid': 'El enlace de confirmación es inválido o está incompleto.',
  'auth.confirm.success': 'Correo confirmado. Ya puedes iniciar sesión.',
  'auth.confirm.goToLogin': 'Ir al inicio de sesión',

  // App shell
  'nav.openMenu': 'Abrir menú',
  'nav.back': 'Volver',
  'nav.profileMenu': 'Abrir menú de perfil',
  'nav.myProfile': 'Mi perfil',
  'nav.logout': 'Cerrar sesión',
  'nav.menu': 'Menú',
  'nav.home': 'Inicio',
  'nav.statistics': 'Estadísticas',
  'nav.profile': 'Perfil',
};
