from rest_framework import generics, permissions, status
from rest_framework.response import Response
from rest_framework.views import APIView
from django.contrib.auth import get_user_model
from django.shortcuts import redirect
from django.urls import reverse
from .serializers import UserSerializer, UserProfileSerializer
from .models import BrowserSession, TwoFactorCode
from .countries import basic_countries, load_countries
from .utils import generate_2fa_code, send_2fa_email, verify_2fa_code
from rest_framework_simplejwt.tokens import RefreshToken
import uuid
import json
import urllib.request
import urllib.error
from django.conf import settings

User = get_user_model()


class RegisterView(generics.CreateAPIView):
    queryset = User.objects.all()
    serializer_class = UserSerializer
    permission_classes = [permissions.AllowAny]

    def perform_create(self, serializer):
        try:
            serializer.save()
        except Exception as e:
            # Convert any lower-level DB errors (e.g., IntegrityError) into a readable ValidationError
            from rest_framework.exceptions import ValidationError
            raise ValidationError({'detail': str(e)})


class LoginView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        try:
            username = request.data.get('username')
            password = request.data.get('password')

            if not username or not password:
                return Response({'error': 'Username and password are required'}, status=status.HTTP_400_BAD_REQUEST)

            try:
                user = User.objects.get(username=username)
                if user.check_password(password):
                    if user.is_2fa_enabled:
                        # 2FA Flow
                        session_id = uuid.uuid4()
                        code = generate_2fa_code()

                        TwoFactorCode.objects.create(
                            user=user,
                            code=code,
                            purpose='login',
                            session_id=session_id
                        )

                        send_2fa_email(user, code, 'login')

                        return Response({
                            'status': '2fa_required',
                            'login_session_id': str(session_id)
                        })

                    # Regular Login
                    refresh = RefreshToken.for_user(user)
                    return Response({
                        'refresh': str(refresh),
                        'access': str(refresh.access_token),
                        'user': UserProfileSerializer(user).data
                    })
                else:
                    return Response({'error': 'Invalid credentials'}, status=status.HTTP_401_UNAUTHORIZED)
            except User.DoesNotExist:
                return Response({'error': 'User not found'}, status=status.HTTP_401_UNAUTHORIZED)
        except Exception as e:
            # Log error and return a generic 500 response
            from rest_framework.exceptions import APIException
            raise APIException(str(e))

class ProfileView(generics.RetrieveUpdateAPIView):
    serializer_class = UserProfileSerializer
    permission_classes = [permissions.IsAuthenticated]

    def get_object(self):
        return self.request.user

class BrowserStartView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        session = BrowserSession.objects.create()
        frontend_url = getattr(settings, 'FRONTEND_URL', 'http://localhost:5173').rstrip('/')
        auth_url = f"{frontend_url}/auth/browser-confirm?session_id={session.session_id}"
        return Response({
            'session_id': session.session_id,
            'auth_url': auth_url
        })

class BrowserConfirmView(APIView):
    permission_classes = [permissions.IsAuthenticated]

    def post(self, request):
        session_id = request.data.get('session_id')
        if not session_id:
            return Response({'error': 'session_id is required'}, status=status.HTTP_400_BAD_REQUEST)

        try:
            session = BrowserSession.objects.get(session_id=session_id)
            session.user = request.user
            session.is_authorized = True
            session.save()
            return Response({'status': 'authorized'})
        except BrowserSession.DoesNotExist:
            return Response({'error': 'Session not found'}, status=status.HTTP_404_NOT_FOUND)

class BrowserPollView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        session_id = request.query_params.get('session_id')
        if not session_id:
            return Response({'error': 'session_id is required'}, status=status.HTTP_400_BAD_REQUEST)

        try:
            session = BrowserSession.objects.get(session_id=session_id)
            if session.is_authorized and session.user:
                refresh = RefreshToken.for_user(session.user)
                # Mark session as consumed to prevent reuse
                session.delete()
                return Response({
                    'access': str(refresh.access_token),
                    'refresh': str(refresh),
                    'user': UserProfileSerializer(session.user).data
                })
            return Response({'status': 'pending'}, status=status.HTTP_200_OK)
        except BrowserSession.DoesNotExist:
            return Response({'error': 'Session not found'}, status=status.HTTP_404_NOT_FOUND)

class SocialLoginRedirectView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request, provider):
        # Redirect to allauth's provider login URL
        # allauth URLs follow the pattern: /accounts/socialaccount/login/<provider>/
        login_url = reverse('socialaccount_login', kwargs={'provider': provider})
        return redirect(login_url)

class SocialAuthCallbackView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        if request.user.is_authenticated:
            user = request.user
            refresh = RefreshToken.for_user(user)

            frontend_url = getattr(settings, 'FRONTEND_URL', 'http://localhost:5173').rstrip('/')
            target_url = f"{frontend_url}/auth-callback?access={refresh.access_token}&refresh={str(refresh)}"
            return redirect(target_url)

        return Response({'error': 'Authentication failed'}, status=status.HTTP_401_UNAUTHORIZED)
class EpicLoginRedirectView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse
        client_id = getattr(settings, 'EPIC_CLIENT_ID', None)
        if not client_id:
            return Response({'error': 'Epic Client ID is not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        redirect_uri = request.build_absolute_uri(reverse('epic_callback'))
        state = request.GET.get('redirect', '')

        # Construct authorization URL
        url = (
            f"https://www.epicgames.com/id/api/v1/oauth/authorize"
            f"?client_id={client_id}"
            f"&response_type=code"
            f"&scope=basic_profile"
            f"&redirect_uri={urllib.parse.quote(redirect_uri)}"
            f"&state={urllib.parse.quote(state)}"
        )
        return redirect(url)

class EpicLoginCallbackView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import base64
        import urllib.parse

        code = request.GET.get('code')
        state = request.GET.get('state', '')
        
        if not code:
            return Response({'error': 'Authorization code is required'}, status=status.HTTP_400_BAD_REQUEST)

        client_id = getattr(settings, 'EPIC_CLIENT_ID', None)
        client_secret = getattr(settings, 'EPIC_CLIENT_SECRET', None)
        if not client_id or not client_secret:
            return Response({'error': 'Epic credentials are not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        redirect_uri = request.build_absolute_uri(reverse('epic_callback'))

        # 1. Exchange authorization code for access token
        try:
            token_url = "https://api.epicgames.dev/auth/v1/oauth/token"
            data = urllib.parse.urlencode({
                'grant_type': 'authorization_code',
                'code': code,
                'redirect_uri': redirect_uri,
            }).encode('utf-8')

            credentials = f"{client_id}:{client_secret}"
            encoded_credentials = base64.b64encode(credentials.encode('utf-8')).decode('utf-8')

            req = urllib.request.Request(token_url, data=data)
            req.add_header('Content-Type', 'application/x-www-form-urlencoded')
            req.add_header('Authorization', f'Basic {encoded_credentials}')

            with urllib.request.urlopen(req) as response:
                token_data = json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            error_body = e.read().decode('utf-8') if e.fp else str(e)
            return Response({'error': f'Failed to exchange Epic token: {error_body}'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Epic token exchange error: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        access_token = token_data.get('access_token')
        if not access_token:
            return Response({'error': 'No access token returned by Epic'}, status=status.HTTP_400_BAD_REQUEST)

        # 2. Retrieve user information
        try:
            userinfo_url = "https://api.epicgames.dev/auth/v1/oauth/userinfo"
            userinfo_req = urllib.request.Request(userinfo_url)
            userinfo_req.add_header('Authorization', f'Bearer {access_token}')

            with urllib.request.urlopen(userinfo_req) as response:
                userinfo_data = json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            error_body = e.read().decode('utf-8') if e.fp else str(e)
            return Response({'error': f'Failed to fetch Epic userinfo: {error_body}'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Epic userinfo error: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        epic_id = userinfo_data.get('sub')
        username = userinfo_data.get('preferred_username', f"epic_{epic_id}")

        if not epic_id:
            return Response({'error': 'Invalid user info from Epic'}, status=status.HTTP_400_BAD_REQUEST)

        # 3. Create or login user
        email = f"{epic_id}@epicgames.placeholder.com"
        
        try:
            user = User.objects.get(email=email)
        except User.DoesNotExist:
            base_username = username
            final_username = base_username
            counter = 1
            while User.objects.filter(username=final_username).exists():
                final_username = f"{base_username}_{counter}"
                counter += 1

            user = User.objects.create_user(
                username=final_username,
                email=email,
                password=User.objects.make_random_password()
            )

        # 4. Generate JWT tokens
        refresh = RefreshToken.for_user(user)
        access_jwt = str(refresh.access_token)
        refresh_jwt = str(refresh)

        # 5. Redirect browser back to React frontend
        frontend_url = getattr(settings, 'FRONTEND_URL', 'http://localhost:5173').rstrip('/')
        target_url = f"{frontend_url}/auth-callback?access={access_jwt}&refresh={refresh_jwt}"
        if state:
            target_url += f"&redirect={urllib.parse.quote(state)}"

        return redirect(target_url)

class DiscordLoginRedirectView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse
        client_id = getattr(settings, 'DISCORD_CLIENT_ID', None)
        if not client_id:
            return Response({'error': 'Discord Client ID is not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        redirect_uri = request.build_absolute_uri(reverse('discord_callback'))
        state = request.GET.get('redirect', '')

        # Construct authorization URL
        url = (
            f"https://discord.com/oauth2/authorize"
            f"?client_id={client_id}"
            f"&redirect_uri={urllib.parse.quote(redirect_uri)}"
            f"&response_type=code"
            f"&scope=identify+email"
            f"&state={urllib.parse.quote(state)}"
        )
        return redirect(url)

class DiscordLoginCallbackView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse

        code = request.GET.get('code')
        state = request.GET.get('state', '')
        
        if not code:
            return Response({'error': 'Authorization code is required'}, status=status.HTTP_400_BAD_REQUEST)

        client_id = getattr(settings, 'DISCORD_CLIENT_ID', None)
        client_secret = getattr(settings, 'DISCORD_CLIENT_SECRET', None)
        if not client_id or not client_secret:
            return Response({'error': 'Discord credentials are not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        redirect_uri = request.build_absolute_uri(reverse('discord_callback'))

        # 1. Exchange authorization code for access token
        try:
            token_url = "https://discord.com/api/oauth2/token"
            data = urllib.parse.urlencode({
                'client_id': client_id,
                'client_secret': client_secret,
                'grant_type': 'authorization_code',
                'code': code,
                'redirect_uri': redirect_uri,
            }).encode('utf-8')

            req = urllib.request.Request(token_url, data=data)
            req.add_header('Content-Type', 'application/x-www-form-urlencoded')

            with urllib.request.urlopen(req) as response:
                token_data = json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            error_body = e.read().decode('utf-8') if e.fp else str(e)
            return Response({'error': f'Failed to exchange Discord token: {error_body}'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Discord token exchange error: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        access_token = token_data.get('access_token')
        if not access_token:
            return Response({'error': 'No access token returned by Discord'}, status=status.HTTP_400_BAD_REQUEST)

        # 2. Retrieve user information
        try:
            userinfo_url = "https://discord.com/api/users/@me"
            userinfo_req = urllib.request.Request(userinfo_url)
            userinfo_req.add_header('Authorization', f'Bearer {access_token}')

            with urllib.request.urlopen(userinfo_req) as response:
                userinfo_data = json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            error_body = e.read().decode('utf-8') if e.fp else str(e)
            return Response({'error': f'Failed to fetch Discord userinfo: {error_body}'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Discord userinfo error: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        discord_id = userinfo_data.get('id')
        username = userinfo_data.get('username', f"discord_{discord_id}")
        email = userinfo_data.get('email')

        if not discord_id:
            return Response({'error': 'Invalid user info from Discord'}, status=status.HTTP_400_BAD_REQUEST)

        if not email:
            email = f"{discord_id}@discord.placeholder.com"
        
        try:
            user = User.objects.get(email=email)
        except User.DoesNotExist:
            base_username = username
            final_username = base_username
            counter = 1
            while User.objects.filter(username=final_username).exists():
                final_username = f"{base_username}_{counter}"
                counter += 1

            user = User.objects.create_user(
                username=final_username,
                email=email,
                password=User.objects.make_random_password()
            )

        # 4. Generate JWT tokens
        refresh = RefreshToken.for_user(user)
        access_jwt = str(refresh.access_token)
        refresh_jwt = str(refresh)

        # 5. Redirect browser back to React frontend
        frontend_url = getattr(settings, 'FRONTEND_URL', 'http://localhost:5173').rstrip('/')
        target_url = f"{frontend_url}/auth-callback?access={access_jwt}&refresh={refresh_jwt}"
        if state:
            target_url += f"&redirect={urllib.parse.quote(state)}"

        return redirect(target_url)

class SteamLoginRedirectView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse
        redirect_uri = request.build_absolute_uri(reverse('steam_callback'))
        state = request.GET.get('redirect', '')

        if state:
            redirect_uri += f"?state={urllib.parse.quote(state)}"

        realm = request.build_absolute_uri('/')
        params = {
            'openid.ns': 'http://specs.openid.net/auth/2.0',
            'openid.mode': 'checkid_setup',
            'openid.return_to': redirect_uri,
            'openid.realm': realm,
            'openid.claimed_id': 'http://specs.openid.net/auth/2.0/identifier_select',
            'openid.identity': 'http://specs.openid.net/auth/2.0/identifier_select',
        }
        
        steam_auth_url = f"https://steamcommunity.com/openid/login?{urllib.parse.urlencode(params)}"
        return redirect(steam_auth_url)

class SteamLoginCallbackView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse
        import re

        state = request.GET.get('state', '')

        # 1. Validate the OpenID assertion by posting back to Steam
        params = {k: v for k, v in request.GET.items()}
        params['openid.mode'] = 'check_authentication'

        try:
            validate_url = "https://steamcommunity.com/openid/login"
            data = urllib.parse.urlencode(params).encode('utf-8')
            req = urllib.request.Request(validate_url, data=data)
            req.add_header('Content-Type', 'application/x-www-form-urlencoded')

            with urllib.request.urlopen(req) as response:
                validation_response = response.read().decode('utf-8')
        except Exception as e:
            return Response({'error': f'Steam OpenID validation failed to request: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        if 'is_valid:true' not in validation_response:
            return Response({'error': 'Invalid Steam OpenID assertion'}, status=status.HTTP_400_BAD_REQUEST)

        # 2. Extract Steam ID from openid.claimed_id
        claimed_id = request.GET.get('openid.claimed_id', '')
        match = re.search(r'openid/id/(\d+)', claimed_id)
        if not match:
            return Response({'error': 'Failed to extract Steam ID from openid.claimed_id'}, status=status.HTTP_400_BAD_REQUEST)

        steam_id = match.group(1)

        # 3. Retrieve user information from Steam API
        username = f"steam_{steam_id}"
        steam_api_key = getattr(settings, 'STEAM_API_KEY', '')

        if steam_api_key:
            try:
                profile_url = f"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steam_api_key}&steamids={steam_id}"
                with urllib.request.urlopen(profile_url) as response:
                    profile_data = json.loads(response.read().decode('utf-8'))
                    players = profile_data.get('response', {}).get('players', [])
                    if players:
                        username = players[0].get('personaname', username)
            except Exception as e:
                pass

        # 4. Create or login user
        email = f"{steam_id}@steam.placeholder.com"
        try:
            user = User.objects.get(email=email)
        except User.DoesNotExist:
            base_username = username
            final_username = base_username
            counter = 1
            while User.objects.filter(username=final_username).exists():
                final_username = f"{base_username}_{counter}"
                counter += 1

            user = User.objects.create_user(
                username=final_username,
                email=email,
                password=User.objects.make_random_password()
            )

        # 5. Generate JWT tokens
        refresh = RefreshToken.for_user(user)
        access_jwt = str(refresh.access_token)
        refresh_jwt = str(refresh)

        # 6. Redirect browser back to React frontend
        frontend_url = getattr(settings, 'FRONTEND_URL', 'http://localhost:5173').rstrip('/')
        target_url = f"{frontend_url}/auth-callback?access={access_jwt}&refresh={refresh_jwt}"
        if state:
            target_url += f"&redirect={urllib.parse.quote(state)}"

        return redirect(target_url)

class TwoFactorEnableView(APIView):
    permission_classes = [permissions.IsAuthenticated]

    def post(self, request):
        user = request.user
        code = generate_2fa_code()

        TwoFactorCode.objects.create(
            user=user,
            code=code,
            purpose='opt_in'
        )

        send_2fa_email(user, code, 'opt_in')
        return Response({'message': 'Verification code sent to your email.'})

class TwoFactorVerifyEnableView(APIView):
    permission_classes = [permissions.IsAuthenticated]

    def post(self, request):
        code = request.data.get('code')
        if not code:
            return Response({'error': 'Code is required'}, status=status.HTTP_400_BAD_REQUEST)

        user = request.user
        if verify_2fa_code(user, code, 'opt_in'):
            user.is_2fa_enabled = True
            user.save()
            return Response({'message': '2FA enabled successfully.'})

        return Response({'error': 'Invalid or expired code'}, status=status.HTTP_400_BAD_REQUEST)

class TwoFactorLoginVerifyView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        login_session_id = request.data.get('login_session_id')
        code = request.data.get('code')

        if not login_session_id or not code:
            return Response({'error': 'login_session_id and code are required'}, status=status.HTTP_400_BAD_REQUEST)

        try:
            # Find the code object associated with this session
            code_obj = TwoFactorCode.objects.get(session_id=login_session_id, is_verified=False)
            user = code_obj.user

            if verify_2fa_code(user, code, 'login', session_id=login_session_id):
                refresh = RefreshToken.for_user(user)
                return Response({
                    'refresh': str(refresh),
                    'access': str(refresh.access_token),
                    'user': UserProfileSerializer(user).data
                })

            return Response({'error': 'Invalid or expired code'}, status=status.HTTP_400_BAD_REQUEST)
        except TwoFactorCode.DoesNotExist:
            return Response({'error': 'Invalid or expired session'}, status=status.HTTP_400_BAD_REQUEST)

class CountryListView(APIView):
    # `AllowAny` does not stop DRF from running JWTAuthentication, which raises
    # AuthenticationFailed on a stale or malformed Bearer token -- a 401 before
    # the permission check is ever reached. An endpoint meant to be anonymous
    # needs `authentication_classes = []`. Same reasoning as the onboarding
    # views; see accounts/onboarding_views.py.
    authentication_classes = []
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        try:
            # lru_cached: this used to re-open and re-parse 788 KB per request.
            return Response(load_countries())
        except Exception as e:
            return Response({'error': f'Failed to load countries: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)


class CountryBasicListView(APIView):
    """The lean country list (cca3 + name.common + flag) for the onboarding page.

    /countries/ is left as-is because Account.jsx depends on its full shape --
    including `idd` for the dial-code selector -- and its test only asserts
    "a list with >0 entries", so a shape change would break the browser
    silently rather than the suite.
    """

    authentication_classes = []
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        return Response(basic_countries())

