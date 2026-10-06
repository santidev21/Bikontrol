// English dictionary. Missing keys fall back to Spanish, then to the key itself.
export const en: Record<string, string> = {
  'app.name': 'Bikontrol',

  // Common
  'common.email': 'Email',
  'common.emailPlaceholder': 'you@example.com',
  'common.password': 'Password',
  'common.newPassword': 'New password',
  'common.confirmPassword': 'Confirm password',
  'common.emailRequired': 'Email is required.',
  'common.emailInvalid': 'Enter a valid email.',
  'common.passwordRequired': 'Password is required.',
  'common.passwordMin': 'Must be at least 6 characters.',
  'common.confirmRequired': 'Please confirm the password.',
  'common.passwordMismatch': 'Passwords do not match.',
  'common.backToLogin': 'Back to sign in',
  'common.help': 'Help',
  'common.terms': 'Terms',
  'common.privacy': 'Privacy',

  // Landing
  'landing.help': 'Help',
  'landing.signIn': 'Sign in',
  'landing.createAccount': 'Create account',
  'landing.hero.title': 'Your motorcycle maintenance, under control.',
  'landing.hero.subtitle':
    'Add your motorcycles, track every service and get a heads-up before it is due. Simple, private and free for motorcycle owners.',
  'landing.cta.start': 'Start for free',
  'landing.cta.haveAccount': 'I already have an account',
  'landing.cta.demo': 'Try the demo',
  'landing.cta.demoLoading': 'Loading demo…',
  'landing.demoError': 'Could not start the demo.',
  'landing.feature.reminders.title': 'Never forget a service again',
  'landing.feature.reminders.desc': 'Reminders by mileage or time, before it is due.',
  'landing.feature.history.title': 'All your history in one place',
  'landing.feature.history.desc': 'What you did, when, and at what mileage — always at hand.',
  'landing.feature.costs.title': 'Know what you spend',
  'landing.feature.costs.desc': 'Cost per motorcycle, per year and per kilometre.',
  'landing.feature.data.title': 'Your data, yours',
  'landing.feature.data.desc': 'Download everything anytime and delete your account in one click.',
  'landing.how.title': 'How it works',
  'landing.step.account.title': 'Create your account',
  'landing.step.account.desc': 'In a minute, with your email or Google.',
  'landing.step.plan.title': 'Add your motorcycle and plan',
  'landing.step.plan.desc': 'Pick your maintenance items; we suggest the essentials.',
  'landing.step.alerts.title': 'Get the alerts',
  'landing.step.alerts.desc': 'By email and notifications, before the service is due.',
  'landing.trust.title': 'Built so you can trust your data with us',
  'landing.trust.backups': 'Verified database backups.',
  'landing.trust.data': 'Your account and data: download or delete them whenever you want.',
  'landing.trust.noAds': 'No ads and we never sell your data.',
  'landing.final.title': 'Get started in 2 minutes',
  'landing.final.subtitle': 'Add your first motorcycle and maintenance plan today.',
  'landing.final.button': 'Create my account',
  'landing.footer.tagline': 'Bikontrol — motorcycle maintenance for owners.',
  'landing.footer.help': 'Help',
  'landing.footer.terms': 'Terms',
  'landing.footer.privacy': 'Privacy',

  // Login
  'auth.login.forgot': 'Forgot your password?',
  'auth.login.submit': 'Sign in',
  'auth.login.orContinue': 'or continue with',
  'auth.login.register': 'Sign up',
  'auth.login.demo': 'Want to try the demo?',
  'auth.login.demoLoading': 'Loading demo...',
  'auth.login.resend': 'Resend confirmation email',
  'auth.login.resendLoading': 'Sending...',
  'auth.login.resendSent': 'We sent you a new confirmation link.',
  'auth.login.googleError': 'Could not get the Google credential.',
  'auth.login.demoError': 'Could not start the demo.',

  // Register
  'auth.register.title': 'Create account',
  'auth.register.fullName': 'Full name',
  'auth.register.fullNamePlaceholder': 'e.g. John Doe',
  'auth.register.confirmSent':
    'We emailed you a confirmation link. Check your inbox (and spam folder) and click the link to activate your account.',
  'auth.register.submit': 'Sign up',

  // Forgot password
  'auth.forgot.title': 'Reset password',
  'auth.forgot.subtitle': 'Enter your email and we will send you a link to reset your password.',
  'auth.forgot.submit': 'Send link',
  'auth.forgot.success':
    'If the email is registered, you will receive a link to reset your password.',

  // Reset password
  'auth.reset.title': 'New password',
  'auth.reset.linkInvalid': 'The recovery link is invalid or incomplete. Request a new one.',
  'auth.reset.submit': 'Reset password',
  'auth.reset.success': 'Password updated. You can now sign in.',

  // Confirm email
  'auth.confirm.title': 'Confirm email',
  'auth.confirm.loading': 'Confirming your email...',
  'auth.confirm.linkInvalid': 'The confirmation link is invalid or incomplete.',
  'auth.confirm.success': 'Email confirmed. You can now sign in.',
  'auth.confirm.goToLogin': 'Go to sign in',

  // App shell
  'nav.openMenu': 'Open menu',
  'nav.back': 'Back',
  'nav.profileMenu': 'Open profile menu',
  'nav.myProfile': 'My profile',
  'nav.logout': 'Sign out',
  'nav.menu': 'Menu',
  'nav.home': 'Home',
  'nav.statistics': 'Statistics',
  'nav.profile': 'Profile',
};
