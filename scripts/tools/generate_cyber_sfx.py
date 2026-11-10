"""Synthesize the project's combat and UI sound effects as 16-bit mono 44.1 kHz WAVs.

Run with plain Python from anywhere: ``python scripts/tools/generate_cyber_sfx.py``.
Writes to ``assets/audio/sfx/``. The noise generator is seeded, so every run produces
identical files. ``create_wav`` removes any DC offset, adds short fades so no sound
starts or stops mid-waveform (which clicks), and scales down any sound whose sample
peak would exceed ``PEAK_CEILING``. Earlier versions clamped at full scale instead,
which clipped blade_clash and drone_explosion (true peaks of +0.5 and +2.7 dBTP).
"""
import math
import os
import struct
import wave
import random

SAMPLE_RATE = 44100
# Highest allowed sample peak, about -3.1 dBFS. The waveform between samples can rise
# above the sample peak (true peak); noisy, distorted effects overshoot by up to ~3 dB,
# so this keeps their true peak under 0 dBTP. Checked with ffmpeg's ebur128 true-peak meter.
PEAK_CEILING = 0.7
# Edge fades in seconds: short enough to keep each attack sharp, long enough to stop clicks.
FADE_IN, FADE_OUT = 0.002, 0.005
# Fixed seed so regenerating gives byte-identical files.
random.seed(2157)


def finish(samples, fade=True):
    """Remove DC offset, apply edge fades, and lower the level only if it would clip.

    ``fade`` is False for loops such as laser_hum, which already apply their own 40 ms
    fade at each end. Returns a new list; applies no gain above 1.0.
    """
    if not samples:
        return samples
    mean = sum(samples) / len(samples)
    out = [s - mean for s in samples]
    if fade:
        fade_in, fade_out = int(FADE_IN * SAMPLE_RATE), int(FADE_OUT * SAMPLE_RATE)
        for i in range(min(fade_in, len(out))):
            out[i] *= i / fade_in
        for i in range(min(fade_out, len(out))):
            out[-1 - i] *= i / fade_out
    peak = max(abs(s) for s in out)
    if peak > PEAK_CEILING:
        out = [s * PEAK_CEILING / peak for s in out]
    return out


def create_wav(filepath, samples, fade=True):
    """Finish ``samples`` (see ``finish``) and write them as a 16-bit mono WAV."""
    samples = finish(samples, fade)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    with wave.open(filepath, 'w') as wav_file:
        wav_file.setnchannels(1)  # Mono
        wav_file.setsampwidth(2)  # 16-bit
        wav_file.setframerate(SAMPLE_RATE)
        packed = []
        for s in samples:
            # Clamp to -1.0 .. 1.0
            clamped = max(-1.0, min(1.0, s))
            int_val = int(clamped * 32767.0)
            packed.append(struct.pack('<h', int_val))
        wav_file.writeframes(b''.join(packed))
    print(f"Generated: {filepath} ({len(samples)} samples, {len(samples)/SAMPLE_RATE:.2f}s)")

def generate_katana_slash():
    duration = 0.22
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        # Envelope: fast attack 0.01s, exponential decay
        env = math.sin(math.pi * min(1.0, t / 0.03)) * math.exp(-t * 14.0)
        
        # Pitch sweep: 1100 Hz down to 220 Hz
        freq = 1100.0 * math.exp(-t * 10.0) + 180.0
        tone = math.sin(2.0 * math.pi * freq * t)
        
        # Filtered noise component
        noise = (random.random() * 2.0 - 1.0)
        
        # High-mid resonance
        reso = math.sin(2.0 * math.pi * 3200.0 * (1.0 - t/duration) * t)
        
        sample = (tone * 0.5 + noise * 0.4 + reso * 0.25) * env
        samples.append(sample)
        
    return samples

def generate_blade_clash():
    duration = 0.35
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    # Inharmonic metallic frequencies
    freqs = [1240.0, 1780.0, 2350.0, 3820.0, 5100.0]
    weights = [0.35, 0.3, 0.2, 0.15, 0.1]
    decays = [18.0, 22.0, 28.0, 36.0, 45.0]
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        env_attack = min(1.0, t / 0.002)
        
        metal_sum = 0.0
        for f, w, d in zip(freqs, weights, decays):
            metal_sum += w * math.sin(2.0 * math.pi * f * t) * math.exp(-t * d)
            
        noise_burst = (random.random() * 2.0 - 1.0) * math.exp(-t * 60.0) * 0.4
        sample = (metal_sum + noise_burst) * env_attack
        samples.append(sample)
        
    return samples

def generate_blade_dash():
    duration = 0.28
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        # Envelope: quick swell and whoosh decay
        env = math.sin(math.pi * min(1.0, t / 0.06)) * math.exp(-t * 8.5)
        
        # Low punch + supersonic sweep
        freq_low = 160.0 * math.exp(-t * 6.0) + 40.0
        low_tone = math.sin(2.0 * math.pi * freq_low * t)
        
        # High sweep: 1800 down to 400
        freq_high = 1800.0 * math.exp(-t * 12.0) + 350.0
        high_tone = math.sin(2.0 * math.pi * freq_high * t)
        
        noise = (random.random() * 2.0 - 1.0) * math.exp(-t * 9.0)
        
        sample = (low_tone * 0.5 + high_tone * 0.35 + noise * 0.35) * env
        samples.append(sample)
        
    return samples

def generate_drone_explosion():
    duration = 0.55
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        env_attack = min(1.0, t / 0.005)
        env_decay = math.exp(-t * 7.0)
        
        # Sub-bass punch (90 Hz -> 25 Hz)
        bass_freq = 90.0 * math.exp(-t * 8.0) + 25.0
        bass = math.sin(2.0 * math.pi * bass_freq * t)
        
        # Distorted explosion noise
        noise = (random.random() * 2.0 - 1.0)
        # Soft clipping
        clipped_noise = math.tanh(noise * 2.5)
        
        # Electrical crackle (intermittent spikes)
        crackle = 0.0
        if random.random() < (0.15 * math.exp(-t * 5.0)):
            crackle = (random.random() * 2.0 - 1.0) * 0.6
            
        sample = (bass * 0.6 + clipped_noise * 0.55 + crackle) * env_attack * env_decay
        samples.append(sample)
        
    return samples

def generate_laser_hum():
    duration = 0.60
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        # Loop-friendly envelope with gentle fade at edges
        edge = min(t / 0.04, (duration - t) / 0.04, 1.0)
        
        # Buzz fundamental 110Hz + harmonics
        f0 = 110.0
        buzz = (
            math.sin(2.0 * math.pi * f0 * t) * 0.45 +
            math.sin(2.0 * math.pi * f0 * 2 * t) * 0.25 +
            math.sin(2.0 * math.pi * f0 * 3 * t) * 0.15 +
            math.sin(2.0 * math.pi * f0 * 5 * t) * 0.10
        )
        # 8 Hz tremolo / pulse shimmer
        tremolo = 0.8 + 0.2 * math.sin(2.0 * math.pi * 8.0 * t)
        
        # High sizzle
        sizzle = (random.random() * 2.0 - 1.0) * 0.08
        
        sample = (buzz * tremolo + sizzle) * edge
        samples.append(sample)
        
    return samples

def generate_shield_hit():
    duration = 0.30
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * min(1.0, t / 0.01)) * math.exp(-t * 12.0)
        
        # Rising resonant crystal sweep
        freq1 = 700.0 + 800.0 * (t / duration)
        freq2 = 1400.0 + 1600.0 * (t / duration)
        
        tone1 = math.sin(2.0 * math.pi * freq1 * t)
        tone2 = math.sin(2.0 * math.pi * freq2 * t)
        
        sample = (tone1 * 0.5 + tone2 * 0.35) * env
        samples.append(sample)
        
    return samples

def generate_boss_cannon():
    duration = 0.45
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * min(1.0, t / 0.01)) * math.exp(-t * 9.0)
        
        # Heavy sub blast
        freq = 190.0 * math.exp(-t * 11.0) + 40.0
        tone = math.sin(2.0 * math.pi * freq * t)
        
        # Plasma distortion
        noise = (random.random() * 2.0 - 1.0) * math.exp(-t * 15.0)
        distorted = math.tanh(tone * 2.5 + noise * 1.2) * 0.7
        
        samples.append(distorted * env)
    return samples

def generate_boss_alarm():
    duration = 0.50
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        # Two-tone klaxon (alternates every 0.12s)
        cycle = int(t / 0.125) % 2
        f = 920.0 if cycle == 0 else 680.0
        tone = math.sin(2.0 * math.pi * f * t)
        # Pulse envelope
        mod = math.sin(math.pi * ((t % 0.125) / 0.125))
        samples.append(tone * mod * 0.6)
    return samples

def generate_victory_jingle():
    duration = 1.20
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    # Major triad arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
    notes = [523.25, 659.25, 783.99, 1046.50]
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        total = 0.0
        for idx, freq in enumerate(notes):
            note_start = idx * 0.15
            if t >= note_start:
                note_t = t - note_start
                env = math.sin(math.pi * min(1.0, note_t / 0.02)) * math.exp(-note_t * 3.5)
                # Pure sine + octave shimmer
                tone = math.sin(2.0 * math.pi * freq * note_t) * 0.5 + math.sin(2.0 * math.pi * freq * 2.0 * note_t) * 0.25
                total += tone * env * 0.35
        samples.append(total)
    return samples

if __name__ == '__main__':
    # Resolve from this file so the script works from any folder or checkout.
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "assets", "audio", "sfx")
    create_wav(os.path.join(out_dir, "katana_slash.wav"), generate_katana_slash())
    create_wav(os.path.join(out_dir, "blade_clash.wav"), generate_blade_clash())
    create_wav(os.path.join(out_dir, "blade_dash.wav"), generate_blade_dash())
    create_wav(os.path.join(out_dir, "drone_explosion.wav"), generate_drone_explosion())
    create_wav(os.path.join(out_dir, "laser_hum.wav"), generate_laser_hum(), fade=False)  # loops
    create_wav(os.path.join(out_dir, "shield_hit.wav"), generate_shield_hit())
    create_wav(os.path.join(out_dir, "boss_cannon.wav"), generate_boss_cannon())
    create_wav(os.path.join(out_dir, "boss_alarm.wav"), generate_boss_alarm())
    create_wav(os.path.join(out_dir, "victory_jingle.wav"), generate_victory_jingle())
    print("All Phase 5 SFX successfully synthesized!")
