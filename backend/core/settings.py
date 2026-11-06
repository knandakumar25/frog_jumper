import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

SECRET_KEY = 'django-insecure-change-this-in-production'

DEBUG = True

ALLOWED_HOSTS = ['*']

INSTALLED_APPS = [
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    'rest_framework',
    'rest_framework_simplejwt',
    'corsheaders',
    'accounts',
]

MIDDLEWARE = [
    'corsheaders.middleware.CorsMiddleware',
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

ROOT_URLCONF = 'core.urls'

TEMPLATES = [
    {
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [],
        'APP_DIRS': True,
        'OPTIONS': {
            'context_processors': [
                'django.template.context_processors.debug',
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
            ],
        },
    },
]

WSGI_APPLICATION = 'core.wsgi.application'

# PostgreSQL is authoritative in development, production, and isolated test databases.
# Never substitute SQLite when a test command is run.
DATABASES = {
    'default': {
        'ENGINE': 'django.db.backends.postgresql',
        'NAME': os.environ.get('DB_NAME', 'accounts_db'),
        'USER': os.environ.get('DB_USER', 'postgres'),
        'PASSWORD': os.environ.get('DB_PASSWORD', 'postgres'),
        'HOST': os.environ.get('DB_HOST', '127.0.0.1'),
        'PORT': os.environ.get('DB_PORT', '5432'),
    }
}

EPIC_CLIENT_ID = os.environ.get('EPIC_CLIENT_ID', 'xyza7891g76bMoT7LRbXiajbp1ZynDUf')
EPIC_CLIENT_SECRET = os.environ.get('EPIC_CLIENT_SECRET', 'DvsZrhk6V1YxjWFgBZ8c1EJZqigg2kyF0kxL+9Xk2RA')

DISCORD_CLIENT_ID = os.environ.get('DISCORD_CLIENT_ID', '1519971724869894197')
DISCORD_CLIENT_SECRET = os.environ.get('DISCORD_CLIENT_SECRET', 'm0Zs7KUvEW49tqisij-cQs4caSY9wEC0')
STEAM_API_KEY = os.environ.get('STEAM_API_KEY', '')


AUTH_USER_MODEL = 'accounts.User'

AUTH_PASSWORD_VALIDATORS = [
    {'NAME': 'django.contrib.auth.password_validation.UserAttributeSimilarityValidator'},
    {'NAME': 'django.contrib.auth.password_validation.MinimumLengthValidator'},
    {'NAME': 'django.contrib.auth.password_validation.CommonPasswordValidator'},
    {'NAME': 'django.contrib.auth.password_validation.NumericPasswordValidator'},
]

LANGUAGE_CODE = 'en-us'
TIME_ZONE = 'UTC'
USE_I18N = True
USE_TZ = True

STATIC_URL = 'static/'
DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

# REST Framework
REST_FRAMEWORK = {
    'DEFAULT_AUTHENTICATION_CLASSES': (
        'rest_framework_simplejwt.authentication.JWTAuthentication',
    ),
    'DEFAULT_PERMISSION_CLASSES': [
        'rest_framework.permissions.IsAuthenticated',
    ],
}

# JWT Settings
from datetime import timedelta
SIMPLE_JWT = {
    'ACCESS_TOKEN_LIFETIME': timedelta(hours=1),
    'REFRESH_TOKEN_LIFETIME': timedelta(days=7),
    'ROTATE_REFRESH_TOKENS': True,
    'BLACKLISTING': True,
    'AUTH_HEADER_TYPES': ('Bearer',),
}

# CORS Settings
CORS_ALLOW_ALL_ORIGINS = True # For development

# Frontend URL for browser login flow
FRONTEND_URL = os.environ.get('FRONTEND_URL', 'http://localhost:5173')

# Email (Resend) ------------------------------------------------------------
# The from-address must sit on a domain verified in Resend, or the API answers
# 403. The default below is Resend's shared test sender: it needs no domain, but
# it can only deliver to the address on the Resend account itself. Switch this
# to e.g. 'Axiom Force <welcome@mail.example.com>' once a domain is verified.
RESEND_API_KEY = os.environ.get('RESEND_API_KEY', '')
DEFAULT_FROM_EMAIL = os.environ.get(
    'DEFAULT_FROM_EMAIL', 'Axiom Force <onboarding@resend.dev>'
)
try:
    RESEND_TIMEOUT = float(os.environ.get('RESEND_TIMEOUT', '10'))
except ValueError:
    RESEND_TIMEOUT = 10.0

# Django 6.1 replaced EMAIL_BACKEND/EMAIL_HOST/... with MAILERS; the old names
# are removed in 7.0. Do NOT define both -- django/conf/__init__.py raises
# ImproperlyConfigured when MAILERS is set alongside any deprecated EMAIL_*
# setting, and reading settings.EMAIL_BACKEND then raises AttributeError.
#
# An empty key falls back to the console backend, so local development and
# `manage.py migrate` never require a secret. Tests need no special-casing
# here: django.test.utils.setup_test_environment() swaps the default mailer to
# the locmem backend for the whole test run.
if RESEND_API_KEY:
    MAILERS = {
        'default': {
            'BACKEND': 'core.mail_backends.ResendEmailBackend',
            'OPTIONS': {
                'api_key': RESEND_API_KEY,
                'timeout': RESEND_TIMEOUT,
            },
        },
    }
else:
    MAILERS = {
        'default': {
            'BACKEND': 'django.core.mail.backends.console.EmailBackend',
        },
    }

# Clerk (identity provider) --------------------------------------------------
# CLERK_SECRET_KEY authenticates *backend* calls to Clerk's API. It must never
# be given a VITE_ prefix: Vite inlines VITE_* into the browser bundle, which
# would publish the key.
#
# Operational dependency: onboarding refuses to mail an unverified address, so
# email verification must stay enabled in the Clerk dashboard or every signup
# answers 403 email_not_verified.
CLERK_SECRET_KEY = os.environ.get('CLERK_SECRET_KEY', '')
# Trusted verification configuration; no issuer or origin is inferred from a token.
CLERK_ISSUER = os.environ.get('CLERK_ISSUER', '').rstrip('/')
CLERK_JWKS_URL = os.environ.get('CLERK_JWKS_URL', CLERK_ISSUER + '/.well-known/jwks.json')
CLERK_AUTHORIZED_PARTIES = [origin.strip() for origin in os.environ.get('CLERK_AUTHORIZED_PARTIES', 'http://localhost:5173').split(',') if origin.strip()]
CLERK_API_BASE = os.environ.get('CLERK_API_BASE', 'https://api.clerk.com/v1')
try:
    CLERK_TIMEOUT = float(os.environ.get('CLERK_TIMEOUT', '10'))
except ValueError:
    CLERK_TIMEOUT = 10.0

# Onboarding (email-gated signup) --------------------------------------------
# Every value is overridable from the environment (export them in the shell or
# provide them to the deployment runtime). They are read here so tests can
# override them with self.settings(...) rather than patching modules.
def _env_int(name, default):
    try:
        return int(os.environ.get(name, default))
    except (TypeError, ValueError):
        return int(default)


# How long an emailed onboarding link stays usable.
ONBOARDING_TOKEN_TTL_HOURS = _env_int('ONBOARDING_TOKEN_TTL_HOURS', 24)
# Minimum gap between sends for one registration; the second request inside this
# window is answered "already_sent" rather than mailing again.
ONBOARDING_RESEND_COOLDOWN = _env_int('ONBOARDING_RESEND_COOLDOWN', 60)
# Lifetime cap per registration. Each send rotates the token and kills the
# previous link.
ONBOARDING_MAX_SENDS = _env_int('ONBOARDING_MAX_SENDS', 5)
# Per-IP and global ceilings over any rolling hour. The global one is the
# billing circuit breaker.
ONBOARDING_IP_HOURLY_LIMIT = _env_int('ONBOARDING_IP_HOURLY_LIMIT', 5)
ONBOARDING_GLOBAL_HOURLY_LIMIT = _env_int('ONBOARDING_GLOBAL_HOURLY_LIMIT', 100)
