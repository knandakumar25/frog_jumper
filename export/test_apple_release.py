"""Exercise Apple configuration and handoff safety using synthetic files, never a game export."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location("apple_release", Path(__file__).with_name("apple_release.py"))
apple = importlib.util.module_from_spec(spec)
spec.loader.exec_module(apple)


class AppleReleaseTests(unittest.TestCase):
    """Keep incomplete identities and local credentials out of the release-preparation path."""

    def setUp(self):
        """Create an isolated fake project; it contains no actual game assets."""
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        (self.root / "project.godot").write_text("[application]\n", encoding="utf-8")
        (self.root / "AxiomForce.csproj").write_text("<Project />", encoding="utf-8")
        self.write_presets("com.owner.testgame", "ABCDE12345")

    def tearDown(self):
        """Release the synthetic fixture and its tiny test archive."""
        self.temporary.cleanup()

    def write_presets(self, bundle, team):
        """Write minimal Godot preset syntax with caller-controlled identity fields."""
        (self.root / "export_presets.cfg").write_text(
            '[preset.0]\nname="macOS"\n[preset.0.options]\n'
            f'application/bundle_identifier="{bundle}"\ncodesign/codesign=0\n'
            '[preset.1]\nname="iOS"\n[preset.1.options]\n'
            f'application/bundle_identifier="{bundle}"\napplication/app_store_team_id="{team}"\n'
            'application/export_project_only=true\nscript_encryption_key="fixture-secret"\n',
            encoding="utf-8",
        )

    def test_complete_identifiers_are_accepted(self):
        """Both valid Apple configurations pass without invoking an external tool."""
        apple.validate_apple(self.root, "macOS")
        apple.validate_apple(self.root, "iOS")

    def test_placeholders_and_missing_team_are_rejected(self):
        """Example identifiers and an incomplete Apple account cannot appear release-ready."""
        self.write_presets("com.example.axiomforce", "")
        with self.assertRaises(ValueError):
            apple.validate_apple(self.root, "macOS")
        self.write_presets("com.owner.testgame", "")
        with self.assertRaises(ValueError):
            apple.validate_apple(self.root, "iOS")

    def test_handoff_keeps_native_libraries_and_excludes_credentials(self):
        """Source assembly preserves native addon inputs while filtering signing/cached/private files."""
        native = self.root / "addons/native/bin/device.a"
        native.parent.mkdir(parents=True)
        native.write_bytes(b"fixture-library")
        for name in ("addons/.env.private", "addons/identity.p12", "addons/AuthKey.p8", "addons/native/.godot/cache.bin",
                     "backend/private.json", ".env", "bin/build.dll", "addons/native/obj/cache.json"):
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("fixture-only", encoding="utf-8")
        output = apple.prepare_ios(self.root)
        with zipfile.ZipFile(output) as archive:
            names = set(archive.namelist())
            self.assertIn("addons/native/bin/device.a", names)
            self.assertNotIn(".env", names)
            self.assertNotIn("addons/.env.private", names)
            self.assertNotIn("addons/identity.p12", names)
            self.assertNotIn("addons/AuthKey.p8", names)
            self.assertFalse(any("/.godot/" in name or "/obj/" in name for name in names))
            self.assertNotIn("backend/private.json", names)
            self.assertNotIn("bin/build.dll", names)
            self.assertNotIn("fixture-secret", archive.read("export_presets.cfg").decode())
            self.assertTrue(json.loads(archive.read("APPLE_HANDOFF.json"))["not_an_ipa"])

    def test_missing_project_input_fails_before_creating_output(self):
        """A malformed source tree cannot generate an apparently valid handoff."""
        (self.root / "AxiomForce.csproj").unlink()
        with self.assertRaises(ValueError):
            apple.prepare_ios(self.root)
        self.assertFalse((self.root / "build").exists())


if __name__ == "__main__":
    unittest.main()
