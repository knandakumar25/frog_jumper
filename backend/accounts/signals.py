import logging

from django.db.models.signals import post_save
from django.dispatch import receiver
from django.core.mail import EmailMultiAlternatives
from django.conf import settings
from django.contrib.auth import get_user_model

User = get_user_model()
logger = logging.getLogger(__name__)

@receiver(post_save, sender=User)
def send_welcome_email(sender, instance, created, **kwargs):
    if created:
        subject = f"SYS_INIT: Welcome to Frog Jumper, {instance.username}"
        from_email = getattr(settings, 'DEFAULT_FROM_EMAIL', 'welcome@frogjumper.dev')
        to_email = instance.email

        if not to_email:
            return

        # Text content
        text_content = (
            f"=== AXIOM FORCE TERMINAL SIGN-IN SUCCESSFUL ===\n\n"
            f"Welcome to the mainframe, Operator {instance.username}.\n\n"
            f"Your user profile has been successfully initialized. You have been granted access "
            f"to the Frog Jumper simulation network.\n\n"
            f"STATUS: ACTIVE\n"
            f"IP PROTOCOL: DETECTED\n"
            f"LEVEL ACCESS: LEVEL 1 OPERATOR\n\n"
            f"Prepare for deployment. See you on the leaderboard.\n\n"
            f"----------------------------------------\n"
            f"Axiom Force Terminal Admin\n"
        )

        # HTML content (neon cyberpunk theme)
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="color-scheme" content="dark light">
            <meta name="supported-color-schemes" content="dark light">
            <style>
                body {{
                    background-color: #05070a;
                    color: #e2e8f0;
                    font-family: 'Courier New', Courier, monospace;
                    margin: 0;
                    padding: 20px;
                }}
                /* Colour is declared on .terminal as well as on body, and inline on the
                   paragraphs below. Clients routinely relocate the message body into
                   their own container, at which point the `body` selector stops matching
                   and anything relying on an INHERITED colour reverts to the client's
                   default -- dark grey text on this dark panel, essentially unreadable.
                   .terminal is known to match (its border and background render), so
                   declaring it here cascades to every element inside. */
                .terminal {{
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #0b0e14;
                    color: #e2e8f0;
                    border: 2px solid #00f3ff;
                    box-shadow: 0 0 20px rgba(0, 243, 255, 0.2);
                    padding: 30px;
                    border-radius: 4px;
                }}
                .header {{
                    border-bottom: 2px solid #ff007f;
                    padding-bottom: 15px;
                    margin-bottom: 25px;
                    text-align: center;
                }}
                .header h1 {{
                    color: #00f3ff;
                    font-size: 24px;
                    margin: 0;
                    text-transform: uppercase;
                    letter-spacing: 3px;
                    text-shadow: 0 0 10px rgba(0, 243, 255, 0.5);
                }}
                .greeting {{
                    font-size: 18px;
                    color: #ff007f;
                    margin-bottom: 20px;
                    font-weight: bold;
                    text-shadow: 0 0 5px rgba(255, 0, 127, 0.3);
                }}
                .details {{
                    background-color: #030508;
                    color: #e2e8f0;
                    border-left: 3px solid #ff007f;
                    padding: 15px;
                    margin: 20px 0;
                    font-size: 14px;
                    line-height: 1.6;
                }}
                .status-active {{
                    color: #39ff14;
                    font-weight: bold;
                    text-shadow: 0 0 5px rgba(57, 255, 20, 0.5);
                }}
                .footer {{
                    margin-top: 30px;
                    font-size: 11px;
                    color: #94a3b8;
                    text-align: center;
                    border-top: 1px solid #1a202c;
                    padding-top: 15px;
                }}
            </style>
        </head>
        <body>
            <div class="terminal">
                <div class="header">
                    <h1>[ AXIOM_FORCE_MAIN_NET ]</h1>
                </div>
                <div class="greeting">
                    &gt; CONNECTION ESTABLISHED: OPERATOR_{instance.username.upper()}
                </div>
                <p style="color: #e2e8f0;">Welcome to the simulation grid. Your system profile has been successfully integrated into the Frog Jumper network.</p>
                
                <div class="details">
                    <strong>SYSTEM STATUS REPORT:</strong><br>
                    ------------------------------<br>
                    ACCESS LEVEL : OPERATOR (LVL_1)<br>
                    SESSION CODE : SECURE<br>
                    GRID STATUS  : <span class="status-active">ONLINE</span>
                </div>
                
                <p style="color: #e2e8f0;">Prepare your reflexes. Jump, survive, and dominate the ranking boards.</p>
                
                <div class="footer">
                    AXIOM FORCE TERMINAL DIRECTIVE // CONFIDENTIAL TRANSMISSION // DO NOT REPLICATE
                </div>
            </div>
        </body>
        </html>
        """

        msg = EmailMultiAlternatives(subject, text_content, from_email, [to_email])
        msg.attach_alternative(html_content, "text/html")
        try:
            msg.send()
        except Exception:
            # Registration must not fail because the mail provider is down, so
            # this stays swallowed -- but it is the only trace of a failed send,
            # and logger.exception keeps the Resend rejection body.
            logger.exception("Error sending welcome email to %s", to_email)
