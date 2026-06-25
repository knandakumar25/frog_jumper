from django.contrib.auth.models import AbstractUser
from django.db import models
import uuid

class User(AbstractUser):
    # Add custom fields here if needed in the future
    # For now, we use the standard Django user fields (username, email, password)
    email = models.EmailField(unique=True)

    def __str__(self):
        return self.username

class BrowserSession(models.Model):
    session_id = models.UUIDField(default=uuid.uuid4, unique=True, editable=False)
    user = models.ForeignKey(User, on_delete=models.CASCADE, null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    is_authorized = models.BooleanField(default=False)

    def __str__(self):
        return f"Session {self.session_id} - Authorized: {self.is_authorized}"
