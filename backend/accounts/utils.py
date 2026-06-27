import random
import string
from django.core.mail import send_mail
from django.utils import timezone
from datetime import timedelta
from .models import TwoFactorCode

def generate_2fa_code():
    """Generates a 6-digit random numeric string."""
    return ''.join(random.choices(string.digits, k=6))

def send_2fa_email(user, code, purpose):
    """
    Sends a 2FA verification code via email.

    Purpose can be 'opt_in' or 'login'.
    """
    if purpose == 'opt_in':
        subject = "Verify Your Frog Jumper 2FA Setup"
        body = f"Welcome to a more secure Frog Jumper experience!\n\nYour verification code to enable Two-Factor Authentication is: {code}\n\nThis code will expire in 10 minutes."
    elif purpose == 'login':
        subject = "Your Frog Jumper Login Code"
        body = f"A login attempt was made on your account. Your verification code is: {code}\n\nThis code will expire in 10 minutes. If this wasn't you, please secure your account."
    else:
        subject = "Frog Jumper Security Code"
        body = f"Your security code is: {code}"

    send_mail(
        subject,
        body,
        "welcome@frogjumper.dev",
        [user.email],
        fail_silently=False,
    )

def verify_2fa_code(user, code, purpose, session_id=None):
    """
    Verifies the 2FA code.
    Returns True if verified, False otherwise.
    """
    # Look for the most recent unverified code for this user, purpose, and optionally session_id
    filters = {
        'user': user,
        'code': code,
        'purpose': purpose,
        'is_verified': False,
    }
    if session_id:
        filters['session_id'] = session_id

    try:
        code_obj = TwoFactorCode.objects.filter(**filters).order_by('-created_at').first()

        if not code_obj:
            return False

        # Check for expiration (10 minutes)
        if timezone.now() > code_obj.created_at + timedelta(minutes=10):
            return False

        # Mark as verified
        code_obj.is_verified = True
        code_obj.save()
        return True
    except Exception:
        return False
