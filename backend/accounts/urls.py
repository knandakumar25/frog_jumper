from django.urls import path
from rest_framework_simplejwt.views import TokenRefreshView
from .views import RegisterView, LoginView, ProfileView, BrowserStartView, BrowserConfirmView, BrowserPollView, SocialLoginRedirectView, SocialAuthCallbackView, EpicLoginRedirectView, EpicLoginCallbackView

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
    path('auth/epic/login/', EpicLoginRedirectView.as_view(), name='epic_login'),
    path('auth/epic/callback/', EpicLoginCallbackView.as_view(), name='epic_callback'),
]
