import importlib.util
import io
import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest import mock
import zipfile

spec = importlib.util.spec_from_file_location("release", Path(__file__).parents[1] / "nuget_release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)

class ReleaseSafeguards(unittest.TestCase):
    def test_versions_ignore_prereleases_and_other_release_lines(self):
        self.assertEqual(release.next_version("10.0.2", ["1.0.9", "10.0.1", "10.0.2-beta"]), "10.0.2")
        self.assertEqual(release.next_version("10.0.2", ["10.0.2", "10.0.7"]), "10.0.8")
        with self.assertRaises(ValueError): release.stable("10.0.2-beta")

    def test_public_consumer_has_one_feed_and_all_dependencies(self):
        root = release.consumer_configuration(release.SOURCE)
        self.assertEqual(len(root.findall("./packageSources/add")), 1)
        self.assertEqual(root.find("./packageSourceMapping/packageSource/package").get("pattern"), "*")

    def test_local_consumer_maps_only_extension_ids_to_retained_artifacts(self):
        root = release.consumer_configuration("/owned/release")
        packages = root.findall("./packageSourceMapping/packageSource[@key='release']/package")
        self.assertEqual({p.get("pattern") for p in packages}, set(release.PACKAGES))
        self.assertNotIn("Troolio.Core", {p.get("pattern") for p in packages})

    def test_cached_csharp_sources_are_outside_the_sdk_compile_glob(self):
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory) / "consumer"
            observed = []
            def command(args, **kwargs):
                cache = Path(kwargs["env"]["NUGET_PACKAGES"])
                if args[1] == "restore":
                    source = cache / "dependency/contentFiles/cs/net10.0/CachedSource.cs"
                    source.parent.mkdir(parents=True)
                    source.write_text("#error Package-cache source must not be compiled")
                else:
                    result = subprocess.run(["dotnet", "msbuild", str(destination / "Consumer.csproj"),
                                             "-getItem:Compile", "-nologo"],
                                            check=True, capture_output=True, text=True)
                    observed.extend(item["FullPath"] for item in json.loads(result.stdout)["Items"]["Compile"])
                return subprocess.CompletedProcess(args, 0)
            with mock.patch.object(release, "run", side_effect=command):
                release.restore_consumer(release.SOURCE, destination, [])
            self.assertEqual(observed, [str(destination / "Smoke.cs")])

    def test_nuget_signature_can_change_but_library_bytes_cannot(self):
        def package(dll, signature=None):
            stream = io.BytesIO()
            with zipfile.ZipFile(stream, "w") as archive:
                archive.writestr("lib/net10.0/Test.dll", dll)
                if signature: archive.writestr(".signature.p7s", signature)
            return stream.getvalue()
        original = release.payload_hashes(package(b"accepted"))
        self.assertEqual(original, release.payload_hashes(package(b"accepted", b"repository signature")))
        self.assertNotEqual(original, release.payload_hashes(package(b"different", b"repository signature")))

    def test_missing_cohort_fails_before_any_publication(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "exactly the four"):
                release.manifest(Path(directory), "10.0.2", "a" * 40)

    def make_cohort(self, directory, core="10.0.8", commit="a" * 40):
        for name in release.PACKAGES:
            with zipfile.ZipFile(directory / f"{name}.10.0.2.nupkg", "w") as archive:
                archive.writestr(f"{name}.nuspec", f"""<package><metadata>
                    <id>{name}</id><version>10.0.2</version><license type="expression">MIT</license>
                    <repository type="git" commit="{commit}" />
                    <dependencies><group targetFramework="net10.0"><dependency id="Troolio.Core" version="{core}" /></group></dependencies>
                    </metadata></package>""")
                archive.writestr(f"lib/net10.0/{name}.dll", b"accepted library")
                archive.writestr("package-readme.md", "# Usage")

    def test_retained_core_dependency_is_checked_against_original_source(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            self.make_cohort(path)
            with mock.patch.object(release, "core_version_at", return_value="10.0.8") as core:
                records = release.manifest(path, "10.0.2", "a" * 40)
            core.assert_called_once_with("a" * 40)
            (path / "package-manifest.json").write_text(__import__("json").dumps(records))
            with mock.patch.object(release, "core_version_at", return_value="10.0.8"):
                self.assertEqual(release.verify_bundle(path, "10.0.2", "a" * 40), records)
            with mock.patch.object(release, "core_version_at", return_value="10.0.9"):
                with self.assertRaisesRegex(ValueError, "accepted public framework"):
                    release.manifest(path, "10.0.2", "a" * 40)

    def test_core_version_lookup_uses_retained_commit_not_current_working_props(self):
        historical = '<Project><PropertyGroup><TroolioCoreVersion>10.0.8</TroolioCoreVersion></PropertyGroup></Project>'
        with mock.patch.object(release, "run", return_value=subprocess.CompletedProcess([], 0, stdout=historical)) as run:
            self.assertEqual(release.core_version_at("a" * 40), "10.0.8")
        self.assertEqual(run.call_args.args[0], ["git", "show", "a" * 40 + ":Directory.Build.props"])

    def test_errors_never_include_key_bearing_command_arguments(self):
        with mock.patch.object(release.subprocess, "run", return_value=subprocess.CompletedProcess([], 1)):
            with self.assertRaises(RuntimeError) as error:
                release.run(["dotnet", "nuget", "push", "--api-key", "must-remain-private"])
        self.assertNotIn("must-remain-private", str(error.exception))

    def test_draft_lookup_uses_authorized_paginated_releases(self):
        draft = {"tag_name": "nuget/v10.0.2", "draft": True}
        first = [{"tag_name": f"older/{i}"} for i in range(100)]
        with mock.patch.object(release, "github", side_effect=[None, first, [draft]]) as github:
            self.assertEqual(release.find_release(draft["tag_name"]), draft)
        self.assertEqual(github.call_args_list[-1].args[0], "releases?per_page=100&page=2")

if __name__ == "__main__": unittest.main()
