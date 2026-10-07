#!/usr/bin/env python3
"""Build retained projects and run meaningful samples/adapter gates."""
import argparse
import os
from pathlib import Path
import subprocess
import time
import urllib.request
from urllib.parse import urlparse, parse_qs

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--results", type=Path, default=ROOT / "artifacts/test-results")
parser.add_argument("--published", action="store_true", help="Consume public extension packages instead of local projects")
args = parser.parse_args()
args.results.mkdir(parents=True, exist_ok=True)
env = dict(os.environ, MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
properties = ["-p:UseSharedCompilation=false", "-m:2", "-p:UseLocalExtensions=" + ("false" if args.published else "true")]
def run(command, **kwargs):
    subprocess.run(command, cwd=ROOT, env=env, check=True, **kwargs)
required = ["TROOLIO_TEST_REDIS", "TROOLIO_TEST_MARTEN", "TROOLIO_TEST_EVENTSTORE"]
missing = [name for name in required if not env.get(name)]
if missing:
    raise SystemExit("Configure disposable adapter fixtures first: " + ", ".join(missing))
env.setdefault("TROOLIO_TODO_KURRENT_CONNECTION", env["TROOLIO_TEST_EVENTSTORE"])
endpoint = urlparse(env["TROOLIO_TEST_EVENTSTORE"])
secure = parse_qs(endpoint.query).get("tls", ["true"])[0].lower() != "false"
health = ("https" if secure else "http") + "://" + endpoint.netloc + "/health/live"
deadline = time.monotonic() + 90
while True:
    try:
        with urllib.request.urlopen(health, timeout=5) as response:
            if response.status in (200, 204): break
    except Exception:
        if time.monotonic() >= deadline: raise RuntimeError("Kurrent fixture did not become ready")
        time.sleep(2)
run(["dotnet", "build", "Troolio.slnx", "-c", "Release", "--nologo", *properties])
for project in ["Tests/Tests.csproj", "Sample/ToDo.EventStoreEfApi.Tests/ToDo.EventStoreEfApi.Tests.csproj",
                "Stores/Troolio.Extensions.Tests/Troolio.Extensions.Tests.csproj"]:
    run(["dotnet", "test", project, "-c", "Release", "--no-build", "--nologo",
         "--logger", "trx", "--results-directory", str(args.results), *properties])
run(["dotnet", "run", "--project", "Sample/Calculator", "-c", "Release", "--no-build"])
env["ConnectionStrings__Marten"] = env["TROOLIO_TEST_MARTEN"]
env["ConnectionStrings__Redis"] = env["TROOLIO_TEST_REDIS"]
run(["dotnet", "run", "--project", "Sample/MartenCounter", "-c", "Release", "--no-build", "--", "--Marten:CreateSchema=true"])
run(["dotnet", "run", "--project", "Sample/RedisReadModels", "-c", "Release", "--no-build"])
run(["npm", "ci", "--prefix", "Sample/ShoppingListSample/Sample.Telemetry", "--no-fund", "--no-audit"])
run(["npm", "run", "build", "--prefix", "Sample/ShoppingListSample/Sample.Telemetry"])
