from django.contrib.auth.models import AbstractUser
from django.db import models
import uuid

class User(AbstractUser):
    # Add custom fields here if needed in the future
    # For now, we use the standard Django user fields (username, email, password)
    email = models.EmailField(unique=True)
    profile_picture = models.TextField(blank=True, default='')
    is_2fa_enabled = models.BooleanField(default=False)
    phone_number = models.CharField(max_length=20, blank=True, null=True)
    country = models.CharField(max_length=100, blank=True, null=True)

    def __str__(self):
        return self.username

class BrowserSession(models.Model):
    session_id = models.UUIDField(default=uuid.uuid4, unique=True, editable=False)
    user = models.ForeignKey(User, on_delete=models.CASCADE, null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    is_authorized = models.BooleanField(default=False)

    def __str__(self):
        return f"Session {self.session_id} - Authorized: {self.is_authorized}"

class TwoFactorCode(models.Model):
    PURPOSE_CHOICES = [
        ('opt_in', 'Opt-In'),
        ('login', 'Login'),
    ]
    user = models.ForeignKey(User, on_delete=models.CASCADE)
    code = models.CharField(max_length=6)
    created_at = models.DateTimeField(auto_now_add=True)
    is_verified = models.BooleanField(default=False)
    purpose = models.CharField(max_length=20, choices=PURPOSE_CHOICES)
    session_id = models.UUIDField(null=True, blank=True)

    def __str__(self):
        return f"2FA Code for {self.user.username} - {self.purpose}"

