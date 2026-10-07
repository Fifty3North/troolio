#!/usr/bin/env python3
"""Prepare, resume and publish one four-extension release from an exact main commit."""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
from urllib.parse import quote
import xml.etree.ElementTree as ET
import zipfile

REPO = Path(__file__).resolve().parents[1]
REPOSITORY = "Fifty3North/troolio"
PROJECTS = {
    "Troolio.Projection.EntityFramework": "Projections/Troolio.Projection.EntityFramework/Troolio.Projection.EntityFramework.csproj",
    "Troolio.Projection.Redis": "Troolio.Projection.Redis/Troolio.Projection.Redis.csproj",
    "Troolio.Stores.EventStore": "Stores/Troolio.Stores.EventStore/Troolio.Stores.EventStore.csproj",
    "Troolio.Stores.Marten": "Stores/Troolio.Stores.Marten/Troolio.Stores.Marten.csproj",
}
PACKAGES = tuple(PROJECTS)
def core_version_at(commit: str) -> str:
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("An exact retained source commit is required")
    content = run(["git", "show", f"{commit}:Directory.Build.props"], cwd=REPO,
                  capture_output=True, text=True).stdout
    version = ET.fromstring(content).findtext(".//TroolioCoreVersion")
    if not version or not STABLE.fullmatch(version):
        raise ValueError("The accepted source must pin a stable public framework version")
    return version

STABLE = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")
TAG_PREFIX = "nuget/v"
SOURCE = "https://api.nuget.org/v3/index.json"


def run(args: list[str], **kwargs) -> subprocess.CompletedProcess:
    result = subprocess.run(args, **kwargs)
    if result.returncode:
        # Never include command arguments: publishing arguments contain a temporary key.
        raise RuntimeError(f"{args[0]} failed (exit {result.returncode})")
    return result


def github(path: str, method: str = "GET", body: dict | None = None):
    args = ["gh", "api", f"repos/{REPOSITORY}/{path}", "--method", method]
    if body is not None:
        args += ["--input", "-"]
    result = subprocess.run(args, input=json.dumps(body) if body is not None else None,
                            capture_output=True, text=True)
    if result.returncode:
        if method == "GET" and "HTTP 404" in result.stderr:
            return None
        raise RuntimeError(f"GitHub request failed: {method} {path}")
    return json.loads(result.stdout) if result.stdout.strip() else None


def find_release(tag: str) -> dict | None:
    published = github(f"releases/tags/{quote(tag, safe='')}")
    if published is not None:
        return published
    # GitHub's by-tag endpoint excludes drafts; the authorized list includes them.
    page = 1
    while True:
        releases = github(f"releases?per_page=100&page={page}") or []
        for release in releases:
            if release["tag_name"] == tag:
                return release
        if len(releases) < 100:
            return None
        page += 1


def fetch(url: str) -> bytes | None:
    try:
        with urllib.request.urlopen(url, timeout=60) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def stable(version: str) -> tuple[int, int, int]:
    match = STABLE.fullmatch(version)
    if not match:
        raise ValueError("A stable major.minor.patch version is required")
    return tuple(map(int, match.groups()))


def next_version(base: str, versions: list[str]) -> str:
    floor = stable(base)
    patches = [stable(v)[2] for v in versions if STABLE.fullmatch(v) and stable(v)[:2] == floor[:2]]
    patch = max(floor[2], max(patches) + 1) if patches else floor[2]
    if patch > 65534:
        raise ValueError("Advance the version line in Directory.Build.props before the assembly patch limit")
    return f"{floor[0]}.{floor[1]}.{patch}"


def payload_hashes(data: bytes) -> dict[str, str]:
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError("Duplicate entries in package archive")
        return {name: hashlib.sha256(archive.read(name)).hexdigest() for name in sorted(names)
                if name != ".signature.p7s" and not name.endswith("/")}


def manifest(packages: Path, version: str, commit: str) -> list[dict]:
    expected = {f"{name}.{version}.nupkg" for name in PACKAGES}
    if {p.name for p in packages.glob("*.nupkg")} != expected:
        raise ValueError("Release must contain exactly the four retained extension packages")
    allowed_symbols = {f"{name}.{version}.snupkg" for name in PACKAGES}
    if not {p.name for p in packages.glob("*.snupkg")}.issubset(allowed_symbols):
        raise ValueError("Unexpected symbol package identity")
    accepted_core_version = core_version_at(commit)
    result = []
    for name in PACKAGES:
        path = packages / f"{name}.{version}.nupkg"
        data = path.read_bytes()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            specs = [n for n in archive.namelist() if n.endswith(".nuspec")]
            if len(specs) != 1:
                raise ValueError("Exactly one nuspec is required")
            root = ET.fromstring(archive.read(specs[0]))
            metadata = root.find("{*}metadata")
            if metadata is None or metadata.findtext("{*}id") != name or metadata.findtext("{*}version") != version:
                raise ValueError("Package identity/version mismatch")
            repository = metadata.find("{*}repository")
            if repository is None or repository.get("commit") != commit:
                raise ValueError("Package source commit mismatch")
            if any("testkit" in n.lower() for n in archive.namelist()):
                raise ValueError("TestKit must not enter a production package")
            dependencies = []
            for dep in metadata.findall(".//{*}dependency"):
                identity, dependency_version = dep.attrib["id"], dep.attrib["version"]
                if identity.lower().startswith("orleankka") or "testkit" in identity.lower():
                    raise ValueError("Bundled implementation must not be an external package dependency")
                if identity == "Troolio.Core" and dependency_version not in (accepted_core_version, f"[{accepted_core_version}]"):
                    raise ValueError("Extensions must reference the accepted public framework version")
                if identity in PACKAGES and dependency_version not in (version, f"[{version}]"):
                    raise ValueError("Internal package versions must match the cohort")
                dependencies.append({"id": identity, "version": dependency_version})
            required = {f"lib/net10.0/{name}.dll", "package-readme.md"}
            if metadata.findtext("{*}license") != "MIT":
                raise ValueError("The retained extensions require their declared MIT license")
            if not any(dep["id"] == "Troolio.Core" for dep in dependencies):
                raise ValueError("Each extension must declare its public framework dependency")
            if not required.issubset(archive.namelist()):
                raise ValueError("Package runtime, readme or legal content missing")
            dlls = [{"entry": n, "sha256": hashlib.sha256(archive.read(n)).hexdigest()}
                    for n in sorted(archive.namelist()) if n.endswith(".dll")]
        record = {"id": name, "version": version, "sha256": hashlib.sha256(data).hexdigest(),
                  "sourceRevision": commit, "dlls": dlls, "dependencies": dependencies,
                  "content": payload_hashes(data)}
        symbols = packages / f"{name}.{version}.snupkg"
        if symbols.exists():
            record["symbols"] = {"file": symbols.name, "sha256": hashlib.sha256(symbols.read_bytes()).hexdigest()}
        result.append(record)
    return result


def verify_bundle(packages: Path, version: str, commit: str) -> list[dict]:
    recorded = json.loads((packages / "package-manifest.json").read_text())
    actual = manifest(packages, version, commit)
    if actual != recorded:
        raise ValueError("Retained release artifacts no longer match their manifest")
    return actual


def consumer_configuration(source: str) -> ET.Element:
    config = ET.Element("configuration")
    sources = ET.SubElement(config, "packageSources")
    ET.SubElement(sources, "clear")
    mapping = ET.SubElement(config, "packageSourceMapping")
    if source.rstrip("/") != SOURCE.rstrip("/"):
        ET.SubElement(sources, "add", key="release", value=source)
        own = ET.SubElement(mapping, "packageSource", key="release")
        for name in PACKAGES:
            ET.SubElement(own, "package", pattern=name)
    ET.SubElement(sources, "add", key="nuget", value=SOURCE)
    public = ET.SubElement(mapping, "packageSource", key="nuget")
    ET.SubElement(public, "package", pattern="*")
    return config


def load_retained_bundle(work: Path, version: str, commit: str, release: dict) -> None:
    bundle = work / "packages.zip"
    if not any(asset["name"] == bundle.name for asset in release["assets"]):
        raise ValueError("Recovery requires an existing retained package bundle")
    run(["gh", "release", "download", TAG_PREFIX + version, "--repo", REPOSITORY,
         "--pattern", bundle.name, "--dir", str(work)])
    packages = work / "packages"
    packages.mkdir()
    with zipfile.ZipFile(bundle) as archive:
        for name in archive.namelist():
            if Path(name).name != name or name not in {"package-manifest.json"} and not name.endswith((".nupkg", ".snupkg")):
                raise ValueError("Unsafe release bundle entry")
        archive.extractall(packages)
    verify_bundle(packages, version, commit)


def prepare_recovery(work: Path, workflow_commit: str, version: str) -> None:
    stable(version)
    tag = TAG_PREFIX + version
    reference = github(f"git/ref/tags/{tag}")
    if not reference or reference["object"]["type"] != "commit":
        raise ValueError("Recovery requires an existing automated lightweight release tag")
    commit = reference["object"]["sha"]
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Invalid retained source commit")
    ancestry = subprocess.run(["git", "merge-base", "--is-ancestor", commit, workflow_commit], cwd=REPO)
    if ancestry.returncode:
        raise ValueError("Retained source must be an ancestor of the current main workflow")
    release = find_release(tag)
    if not release:
        raise ValueError("Recovery requires an existing GitHub release")
    work.mkdir(parents=True, exist_ok=False)
    load_retained_bundle(work, version, commit, release)
    (work / "state.json").write_text(json.dumps({"version": version, "commit": commit,
                                               "workflowCommit": workflow_commit, "tag": tag}))
    print(f"Recovery prepared {version} from retained source {commit}; workflow {workflow_commit}")


def restore_consumer(source: str, destination: Path, records: list[dict]) -> None:
    destination.mkdir(parents=True, exist_ok=False)
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    props = ET.SubElement(project, "PropertyGroup")
    ET.SubElement(props, "TargetFramework").text = "net10.0"
    ET.SubElement(props, "TreatWarningsAsErrors").text = "true"
    refs = ET.SubElement(project, "ItemGroup")
    for record in records:
        ET.SubElement(refs, "PackageReference", Include=record["id"], Version=f"[{record['version']}]")
    ET.ElementTree(project).write(destination / "Consumer.csproj", encoding="utf-8")
    (destination / "Smoke.cs").write_text('''public static class PackageSmoke {
    public static System.Type[] Types => [typeof(Troolio.Core.Metadata),
        typeof(Troolio.Core.Projection.EntityFrameworkBatched<,>),
        typeof(Troolio.Projection.Redis.Providers.RedisProvider<>),
        typeof(Troolio.Stores.ESStore), typeof(Troolio.Stores.MartenStore)];
}
''')
    ET.ElementTree(consumer_configuration(source)).write(destination / "NuGet.Config", encoding="utf-8")
    # Package caches contain .cs content files; keep them outside the SDK compile glob.
    cache = destination.with_name(destination.name + "-cache")
    cache.mkdir(parents=True, exist_ok=False)
    env = dict(os.environ, NUGET_PACKAGES=str(cache))
    run(["dotnet", "restore", str(destination / "Consumer.csproj"), "--configfile", str(destination / "NuGet.Config"),
         "--no-http-cache", "--nologo"], env=env)
    run(["dotnet", "build", str(destination / "Consumer.csproj"), "--no-restore", "-c", "Release", "--nologo"], env=env)
    for record in records:
        identity, version = record["id"].lower(), record["version"]
        archive = cache / identity / version / f"{identity}.{version}.nupkg"
        if payload_hashes(archive.read_bytes()) != record["content"]:
            raise ValueError("Restored package payload differs from retained release artifacts")


def prepare(work: Path, commit: str, resume_version: str | None = None) -> None:
    if resume_version:
        prepare_recovery(work, commit, resume_version)
        return
    refs = github("git/matching-refs/tags/nuget/v") or []
    reserved = {r["ref"].removeprefix("refs/tags/" ): r["object"]["sha"] for r in refs}
    matching = [tag for tag, sha in reserved.items() if sha == commit]
    if len(matching) > 1:
        raise ValueError("Multiple automated release versions refer to this commit")
    # Reconcile previous drafts before allowing a different commit to publish another cohort.
    for tag, sha in reserved.items():
        if sha != commit:
            release = find_release(tag)
            if release is None or release["draft"]:
                raise ValueError(f"Resume the unfinished {tag} run before publishing another commit")
    base = ET.parse(REPO / "Directory.Build.props").findtext(".//TroolioExtensionsVersion")
    if not base:
        raise ValueError("Directory.Build.props must define TroolioExtensionsVersion")
    versions = [tag.removeprefix(TAG_PREFIX) for tag in reserved]
    for name in PACKAGES:
        data = fetch(f"https://api.nuget.org/v3-flatcontainer/{name.lower()}/index.json")
        if data is not None:
            versions.extend(json.loads(data)["versions"])
    version = matching[0].removeprefix(TAG_PREFIX) if matching else next_version(base, versions)
    stable(version)
    tag = TAG_PREFIX + version
    release = find_release(tag) if matching else None
    work.mkdir(parents=True, exist_ok=False)
    packages = work / "packages"
    bundle = work / "packages.zip"
    if release and any(asset["name"] == bundle.name for asset in release["assets"]):
        load_retained_bundle(work, version, commit, release)
    else:
        properties = [f"-p:TroolioExtensionsVersion={version}", "-p:UseLocalExtensions=true",
                      "-p:ContinuousIntegrationBuild=true", f"-p:RepositoryCommit={commit}", "-p:UseSharedCompilation=false"]
        run([sys.executable, "scripts/verify.py", "--results", str(work / "test-results")], cwd=REPO)
        packages.mkdir()
        for name, project in PROJECTS.items():
            run(["dotnet", "pack", project, "-c", "Release", "--nologo", "-m:2",
                 "--output", str(packages)] + properties, cwd=REPO)
        records = manifest(packages, version, commit)
        (packages / "package-manifest.json").write_text(json.dumps(records, indent=2) + "\n")
        restore_consumer(str(packages), work / "local-consumer", records)
        with zipfile.ZipFile(bundle, "w", zipfile.ZIP_DEFLATED) as archive:
            for path in sorted(packages.iterdir()):
                archive.write(path, path.name)
        if not matching:
            github("git/refs", "POST", {"ref": "refs/tags/" + tag, "sha": commit})
        if not release:
            notes = work / "release-notes.md"
            notes.write_text(f"Troolio {version}\n\nSource commit: `{commit}`.\n\nFour optional Troolio extensions, built and verified together.\n")
            run(["gh", "release", "create", tag, "--repo", REPOSITORY, "--verify-tag", "--draft",
                 "--title", f"Troolio {version}", "--notes-file", str(notes)])
        run(["gh", "release", "upload", tag, str(packages / "package-manifest.json"), str(bundle), "--repo", REPOSITORY])
    state = {"version": version, "commit": commit, "tag": tag}
    (work / "state.json").write_text(json.dumps(state))
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a") as output:
            output.write(f"version={version}\ntag={tag}\n")
    print(f"Prepared {version} from {commit}; exact artifacts retained in {tag}")


def publish(work: Path) -> None:
    key = os.environ.get("NUGET_API_KEY")
    if not key:
        raise ValueError("OIDC login did not supply a temporary NuGet key")
    state = json.loads((work / "state.json").read_text())
    packages = work / "packages"
    records = verify_bundle(packages, state["version"], state["commit"])
    missing = []
    # Validate every existing package before uploading anything during a retry.
    for record in records:
        name, version = record["id"].lower(), record["version"]
        data = fetch(f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg")
        if data is None:
            missing.append(record)
        elif payload_hashes(data) != record["content"]:
            raise ValueError(f"Public {record['id']} {version} has a different payload")
    for record in missing:
        run(["dotnet", "nuget", "push", str(packages / f"{record['id']}.{record['version']}.nupkg"),
             "--source", SOURCE, "--api-key", key, "--no-symbols"])
    # Package indexing is asynchronous. Do not finalize a release until every ID is available.
    deadline = time.monotonic() + 900
    while True:
        pending = []
        for record in records:
            name, version = record["id"].lower(), record["version"]
            data = fetch(f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg")
            if data is None:
                pending.append(record["id"])
            elif payload_hashes(data) != record["content"]:
                raise ValueError("Public package payload differs from this release")
        if not pending:
            break
        if time.monotonic() > deadline:
            raise RuntimeError("NuGet indexing timed out; rerun this workflow to reconcile the retained release")
        print("Awaiting NuGet indexing: " + ", ".join(pending), flush=True)
        time.sleep(20)
    restore_consumer(SOURCE, work / "public-consumer", records)
    for record in records:
        if "symbols" in record:
            run(["dotnet", "nuget", "push", str(packages / record["symbols"]["file"]),
                 "--source", SOURCE, "--api-key", key, "--skip-duplicate"])
    run(["gh", "release", "edit", state["tag"], "--repo", REPOSITORY, "--draft=false", "--latest"])
    print(f"Published and verified all four Troolio extension {state['version']} packages")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("prepare", "publish"))
    parser.add_argument("work", type=Path)
    parser.add_argument("--resume-version", help="Verify/publish an existing retained release using this main workflow")
    args = parser.parse_args()
    if os.environ.get("GITHUB_REPOSITORY") != REPOSITORY or os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise ValueError("Publishing is restricted to Fifty3North/troolio main")
    commit = os.environ.get("GITHUB_SHA", "")
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("An exact source commit is required")
    if run(["git", "rev-parse", "HEAD"], cwd=REPO, capture_output=True, text=True).stdout.strip() != commit:
        raise ValueError("Checked-out source differs from the workflow commit")
    if args.command == "prepare":
        prepare(args.work.resolve(), commit, args.resume_version)
    else:
        state = json.loads((args.work / "state.json").read_text())
        if state.get("workflowCommit", state["commit"]) != commit:
            raise ValueError("Release state belongs to another controlling workflow commit")
        publish(args.work.resolve())
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print(f"Release failed: {error}", file=sys.stderr)
        sys.exit(1)
