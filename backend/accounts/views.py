from rest_framework import generics, permissions, status
from rest_framework.response import Response
from rest_framework.views import APIView
from django.contrib.auth import get_user_model
from django.shortcuts import redirect
from django.urls import reverse
from .serializers import UserSerializer, UserProfileSerializer
from .models import BrowserSession
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

class LoginView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        username = request.data.get('username')
        password = request.data.get('password')

        if not username or not password:
            return Response({'error': 'Username and password are required'}, status=status.HTTP_400_BAD_REQUEST)

        try:
            user = User.objects.get(username=username)
            if user.check_password(password):
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

class GoogleLoginView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        token = request.data.get('token')
        if not token:
            return Response({'error': 'Token is required'}, status=status.HTTP_400_BAD_REQUEST)

        # 1. Verify token with Google's tokeninfo API
        try:
            url = f"https://oauth2.googleapis.com/tokeninfo?id_token={token}"
            req = urllib.request.Request(url)
            with urllib.request.urlopen(req) as response:
                payload = json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            return Response({'error': 'Invalid Google token'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Failed to verify token: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        # 2. Validate token audience and issuer
        aud = payload.get('aud')
        iss = payload.get('iss')
        google_client_id = getattr(settings, 'GOOGLE_CLIENT_ID', None)

        if not google_client_id:
            return Response({'error': 'Google Client ID is not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        if aud != google_client_id:
            return Response({'error': 'Token audience mismatch'}, status=status.HTTP_400_BAD_REQUEST)

        if iss not in ['accounts.google.com', 'https://accounts.google.com']:
            return Response({'error': 'Invalid token issuer'}, status=status.HTTP_400_BAD_REQUEST)

        # 3. Check if email is verified
        if not payload.get('email_verified') or payload.get('email_verified') == 'false':
            return Response({'error': 'Google email is not verified'}, status=status.HTTP_400_BAD_REQUEST)

        email = payload.get('email')
        name = payload.get('name', '')
        first_name = payload.get('given_name', '')
        last_name = payload.get('family_name', '')
        picture = payload.get('picture', '')

        # 4. Get or create user
        try:
            user = User.objects.get(email=email)
            # Update user profile picture and name if they are empty
            updated = False
            if not user.profile_picture and picture:
                user.profile_picture = picture
                updated = True
            if not user.first_name and first_name:
                user.first_name = first_name
                updated = True
            if not user.last_name and last_name:
                user.last_name = last_name
                updated = True
            if updated:
                user.save()
        except User.DoesNotExist:
            # Generate a unique username from email
            base_username = email.split('@')[0]
            username = base_username
            counter = 1
            while User.objects.filter(username=username).exists():
                username = f"{base_username}{counter}"
                counter += 1

            # Create new user
            user = User.objects.create_user(
                username=username,
                email=email,
                password=User.objects.make_random_password(),
                first_name=first_name,
                last_name=last_name,
                profile_picture=picture
            )

        # 5. Generate SimpleJWT tokens
        refresh = RefreshToken.for_user(user)
        return Response({
            'refresh': str(refresh),
            'access': str(refresh.access_token),
            'user': UserProfileSerializer(user).data
        }, status=status.HTTP_200_OK)

class FacebookLoginView(APIView):
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        token = request.data.get('token')
        if not token:
            return Response({'error': 'Token is required'}, status=status.HTTP_400_BAD_REQUEST)

        # 1. Verify token and get app info to prevent cross-app token reuse
        facebook_app_id = getattr(settings, 'FACEBOOK_APP_ID', None)
        if not facebook_app_id:
            return Response({'error': 'Facebook App ID is not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        try:
            app_url = f"https://graph.facebook.com/app?access_token={token}"
            app_req = urllib.request.Request(app_url)
            with urllib.request.urlopen(app_req) as app_response:
                app_payload = json.loads(app_response.read().decode('utf-8'))
            
            if str(app_payload.get('id')) != str(facebook_app_id):
                return Response({'error': 'Token was not generated for this application'}, status=status.HTTP_400_BAD_REQUEST)
        except urllib.error.HTTPError as e:
            return Response({'error': 'Invalid Facebook token or app verification failed'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Failed to verify Facebook app info: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        # 2. Fetch user information from Facebook Graph API
        try:
            user_url = f"https://graph.facebook.com/v19.0/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={token}"
            user_req = urllib.request.Request(user_url)
            with urllib.request.urlopen(user_req) as user_response:
                payload = json.loads(user_response.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            return Response({'error': 'Failed to fetch user data from Facebook'}, status=status.HTTP_400_BAD_REQUEST)
        except Exception as e:
            return Response({'error': f'Facebook Graph API error: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        # 3. Process user data
        email = payload.get('email')
        facebook_id = payload.get('id')
        if not email:
            email = f"{facebook_id}@facebook.placeholder.com"

        first_name = payload.get('first_name', '')
        last_name = payload.get('last_name', '')
        
        # Get picture URL
        picture = ''
        picture_data = payload.get('picture', {}).get('data', {})
        if picture_data and not picture_data.get('is_silhouette', False):
            picture = picture_data.get('url', '')

        # 4. Get or create user
        try:
            user = User.objects.get(email=email)
            # Update user profile picture and name if they are empty
            updated = False
            if not user.profile_picture and picture:
                user.profile_picture = picture
                updated = True
            if not user.first_name and first_name:
                user.first_name = first_name
                updated = True
            if not user.last_name and last_name:
                user.last_name = last_name
                updated = True
            if updated:
                user.save()
        except User.DoesNotExist:
            # Generate a unique username
            base_username = f"fb_{facebook_id}"
            username = base_username
            counter = 1
            while User.objects.filter(username=username).exists():
                username = f"{base_username}_{counter}"
                counter += 1

            # Create new user
            user = User.objects.create_user(
                username=username,
                email=email,
                password=User.objects.make_random_password(),
                first_name=first_name,
                last_name=last_name,
                profile_picture=picture
            )

        # 5. Generate SimpleJWT tokens
        refresh = RefreshToken.for_user(user)
        return Response({
            'refresh': str(refresh),
            'access': str(refresh.access_token),
            'user': UserProfileSerializer(user).data
        }, status=status.HTTP_200_OK)

class EpicLoginRedirectView(APIView):
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        import urllib.parse
        client_id = getattr(settings, 'EPIC_CLIENT_ID', None)
        if not client_id:
            return Response({'error': 'Epic Client ID is not configured on the backend'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        redirect_uri = 'http://localhost:8000/api/auth/epic/callback/'
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

        redirect_uri = 'http://localhost:8000/api/auth/epic/callback/'

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
