from django.contrib.auth.models import AbstractUser
from django.db import models

class User(AbstractUser):
    # Add custom fields here if needed in the future
    # For now, we use the standard Django user fields (username, email, password)
    email = models.EmailField(unique=True)

    def __str__(self):
        return self.username
