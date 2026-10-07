#!/usr/bin/env python3
"""Merge sample guides with framework authoring sources into a static Pages artifact."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--framework-source", type=Path, required=True)
parser.add_argument("--output", type=Path, default=ROOT / "artifacts/public-site")
args = parser.parse_args()
framework = args.framework_source.resolve()
output = args.output.resolve()
stage = ROOT / "artifacts/public-docs-source"
stage.mkdir(parents=True, exist_ok=True)
shutil.copytree(framework / "docs/public", stage / "docs/public", dirs_exist_ok=True)
shutil.copytree(framework / "website", stage / "website", dirs_exist_ok=True)
shutil.copytree(ROOT / "docs/public", stage / "docs/public", dirs_exist_ok=True)
shutil.copytree(ROOT / "website/assets", stage / "website/assets", dirs_exist_ok=True)
manifest = json.loads((framework / "website/doc_pages.json").read_text())
for page in manifest:
    if page["slug"] == "packages": page["source"] = "docs/public/261007-0035-packages.md"
manifest.extend(json.loads((ROOT / "website/doc_pages.json").read_text()))
def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(value.replace("\r\n", "\n").replace("\n", "\r\n").encode())
for source in (stage / "docs/public").glob("*.md"):
    content = source.read_text().replace("10.0.6", "10.0.8")
    if source.name == "261006-2210-getting-started.md":
        content += "\n## Run a complete sample\n\nStart with [Calculator](calculator-sample.html), continue with the [ToDo API](todo-sample.html), then explore [Shopping List](shopping-sample.html). The examples use public packages and include host setup, source code and storage boundaries. [Packages](packages.html) describes the framework and optional extension version lines.\n"
    write(source, content)
write(stage / "website/doc_pages.json", json.dumps(manifest, indent=2))
subprocess.run([sys.executable, str(stage / "website/build_docs.py")], check=True)
# Workshop remains an original downloadable example, now on the current framework package line.
workshop = stage / "website/examples/workshop"
shutil.copytree(framework / "examples/workshop", workshop, dirs_exist_ok=True,
                ignore=shutil.ignore_patterns("bin", "obj"))
for source in workshop.rglob("*.csproj"):
    content = source.read_text().replace("10.0.6", "10.0.8")
    if source.name == "261006-2210-getting-started.md":
        content += "\n## Run a complete sample\n\nStart with [Calculator](calculator-sample.html), continue with the [ToDo API](todo-sample.html), then explore [Shopping List](shopping-sample.html). The examples use public packages and include host setup, source code and storage boundaries. [Packages](packages.html) describes the framework and optional extension version lines.\n"
    write(source, content)
with zipfile.ZipFile(stage / "website/examples/workshop.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    for source in sorted(workshop.rglob("*")):
        if source.is_file() and not {"obj", "bin"}.intersection(source.parts):
            archive.write(source, "workshop/" + str(source.relative_to(workshop)))
write(stage / "website/index.html", (stage / "website/index.html").read_text().replace("10.0.6", "10.0.8"))
output.mkdir(parents=True, exist_ok=True)
for name in ["assets", "docs", "examples"]:
    shutil.copytree(stage / "website" / name, output / name, dirs_exist_ok=True)
shutil.copy2(stage / "website/index.html", output / "index.html")
print(f"Static public site: {output}")
