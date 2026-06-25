from rest_framework import generics, permissions, status
from rest_framework.response import Response
from rest_framework.views import APIView
from django.contrib.auth import get_user_model
from .serializers import UserSerializer, UserProfileSerializer
from .models import BrowserSession
from rest_framework_simplejwt.tokens import RefreshToken
import uuid

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
        # In a real scenario, the domain would be an environment variable
        auth_url = f"http://localhost:5173/auth/browser-confirm?session_id={session.session_id}"
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
