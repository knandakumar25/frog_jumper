"""Invalidate secretless legacy requests and retain cross-tab onboarding association."""
from django.db import migrations, models
import django.db.models.deletion


class Migration(migrations.Migration):
    """Add nullable expiry so legacy requests remain deliberately unusable."""
    dependencies = [('accounts', '0006_user_clerk_user_id_user_date_of_birth_pendingregistration')]
    operations = [
        migrations.AddField('browsersession', 'secret_hash', models.CharField(max_length=64, blank=True, default='')),
        migrations.AddField('browsersession', 'expires_at', models.DateTimeField(null=True, blank=True)),
        migrations.AddField('browsersession', 'consumed_at', models.DateTimeField(null=True, blank=True)),
        migrations.AddField('browsersession', 'cancelled_at', models.DateTimeField(null=True, blank=True)),
        migrations.AddField('browsersession', 'intent', models.CharField(max_length=8, default='login')),
        migrations.AddField('browsersession', 'locale', models.CharField(max_length=32, default='en-US')),
        migrations.AddField('pendingregistration', 'browser_session', models.ForeignKey(null=True, blank=True, to='accounts.browsersession', on_delete=django.db.models.deletion.SET_NULL)),
    ]
