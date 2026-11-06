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
    # Links this row to a Clerk user, who owns the credentials. Nullable because
    # every existing row predates Clerk, and the OAuth callback paths (Epic,
    # Discord, Steam) create users with placeholder addresses and no Clerk id.
    # `unique` still holds among the rows that do have one.
    clerk_user_id = models.CharField(max_length=64, unique=True, null=True, blank=True)
    date_of_birth = models.DateField(null=True, blank=True)

    def __str__(self):
        return self.username

class PendingRegistration(models.Model):
    """A Clerk signup that has been emailed a link but has not finished yet.

    No Django User exists until /api/auth/onboarding/complete/ succeeds, so this
    row is the only record of a signup in flight. Rows survive completion
    (`completed_at` is set, not deleted) so replay is a clean 409, the rate-limit
    counters stay exact, and there is an audit trail.

    `email` is deliberately NOT unique here: two Clerk users can legitimately
    claim the same address, and the collision belongs at completion time -- where
    it surfaces as a 409 -- rather than as an IntegrityError raised at signup.
    """

    browser_session = models.ForeignKey('BrowserSession', on_delete=models.SET_NULL, null=True, blank=True)
    clerk_user_id = models.CharField(max_length=64, unique=True)
    email = models.EmailField()
    # sha256 hex of the raw token, never the token itself. The raw value exists
    # only inside the email body -- see onboarding_views. Consequence: a stored
    # token can never be re-sent, so /start/ rotates rather than resends.
    token_hash = models.CharField(max_length=64, unique=True)
    first_name = models.CharField(max_length=150, blank=True, default='')
    last_name = models.CharField(max_length=150, blank=True, default='')
    clerk_username = models.CharField(max_length=150, blank=True, default='')
    request_ip = models.GenericIPAddressField(null=True, blank=True)
    sent_count = models.PositiveIntegerField(default=0)
    created_at = models.DateTimeField(auto_now_add=True)
    last_sent_at = models.DateTimeField()
    expires_at = models.DateTimeField()
    completed_at = models.DateTimeField(null=True, blank=True)

    def __str__(self):
        return f"Pending registration for {self.email}"

class BrowserSession(models.Model):
    """Public approval handle with hashed device proof and terminal timestamps."""
    secret_hash = models.CharField(max_length=64, blank=True, default='')
    expires_at = models.DateTimeField(null=True, blank=True)
    consumed_at = models.DateTimeField(null=True, blank=True)
    cancelled_at = models.DateTimeField(null=True, blank=True)
    intent = models.CharField(max_length=8, default='login')
    locale = models.CharField(max_length=32, default='en-US')
    session_id = models.UUIDField(default=uuid.uuid4, unique=True, editable=False)
    user = models.ForeignKey(User, on_delete=models.CASCADE, null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    is_authorized = models.BooleanField(default=False)

    def __str__(self):
        """Identify approval state for administration without revealing device proof."""
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

