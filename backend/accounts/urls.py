from django.urls import path
from rest_framework_simplejwt.views import TokenRefreshView
from .views import (
    RegisterView, LoginView, ProfileView, BrowserStartView, BrowserConfirmView, BrowserPollView,
    SocialLoginRedirectView, SocialAuthCallbackView,
    EpicLoginRedirectView, EpicLoginCallbackView,
    DiscordLoginRedirectView, DiscordLoginCallbackView,
    SteamLoginRedirectView, SteamLoginCallbackView,
    TwoFactorEnableView, TwoFactorVerifyEnableView, TwoFactorLoginVerifyView,
    CountryListView, CountryBasicListView
)
from .onboarding_views import (
    OnboardingStartView, OnboardingValidateTokenView, OnboardingCompleteView
)

urlpatterns = [
    path('register/', RegisterView.as_view(), name='register'),
    path('login/', LoginView.as_view(), name='login'),
    path('token/refresh/', TokenRefreshView.as_view(), name='token_refresh'),
    path('profile/', ProfileView.as_view(), name='profile'),
    path('browser-start/', BrowserStartView.as_view(), name='browser_start'),
    path('browser-confirm/', BrowserConfirmView.as_view(), name='browser_confirm'),
    path('browser-poll/', BrowserPollView.as_view(), name='browser_poll'),
    path('social/login/<str:provider>/', SocialLoginRedirectView.as_view(), name='social_login'),
    path('social/callback/', SocialAuthCallbackView.as_view(), name='social_callback'),
    path('epic/login/', EpicLoginRedirectView.as_view(), name='epic_login'),
    path('epic/callback/', EpicLoginCallbackView.as_view(), name='epic_callback'),
    path('discord/login/', DiscordLoginRedirectView.as_view(), name='discord_login'),
    path('discord/callback/', DiscordLoginCallbackView.as_view(), name='discord_callback'),
    path('steam/login/', SteamLoginRedirectView.as_view(), name='steam_login'),
    path('steam/callback/', SteamLoginCallbackView.as_view(), name='steam_callback'),
    path('2fa/enable/', TwoFactorEnableView.as_view(), name='two_factor_enable'),
    path('2fa/verify-enable/', TwoFactorVerifyEnableView.as_view(), name='two_factor_verify_enable'),
    path('2fa/verify-login/', TwoFactorLoginVerifyView.as_view(), name='two_factor_verify_login'),
    path('countries/', CountryListView.as_view(), name='country_list'),
    path('countries/basic/', CountryBasicListView.as_view(), name='country_list_basic'),
    # Email-gated onboarding. Anonymous on purpose: /start/ runs on a Clerk
    # session, and /validate-token/ + /complete/ run on the emailed token alone,
    # which is what lets the link open in a browser with no Clerk session.
    path('onboarding/start/', OnboardingStartView.as_view(), name='onboarding_start'),
    path('onboarding/validate-token/', OnboardingValidateTokenView.as_view(), name='onboarding_validate_token'),
    path('onboarding/complete/', OnboardingCompleteView.as_view(), name='onboarding_complete'),
]
