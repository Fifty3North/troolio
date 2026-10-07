# Developing the samples and extensions

Use .NET10 and Node.js22.12+. Public packages are the default. Pass `-p:UseLocalExtensions=true` when changing the adapter source before a release.

## Disposable adapter fixtures

Run Docker or Podman with local-only published ports and uniquely named containers:

```sh
podman run --detach --name troolio-samples-redis --publish 127.0.0.1:16379:6379 redis:8.2-alpine
podman run --detach --name troolio-samples-postgres --publish 127.0.0.1:15432:5432 -e POSTGRES_DB=troolio_extensions -e POSTGRES_PASSWORD=development postgres:17-alpine
podman run --detach --name troolio-samples-kurrent --publish 127.0.0.1:12113:2113 -e KURRENTDB_INSECURE=true -e KURRENTDB_RUN_PROJECTIONS=None -e KURRENTDB_START_STANDARD_PROJECTIONS=false kurrentplatform/kurrentdb:26.0.1
```

These development connection settings are for the disposable local fixtures above:

```sh
export TROOLIO_TEST_REDIS='localhost:16379'
export TROOLIO_TEST_MARTEN='Host=localhost;Port=15432;Database=troolio_extensions;Username=postgres;Password=development'
export TROOLIO_TEST_EVENTSTORE='esdb://localhost:12113?tls=false'
python3 scripts/verify.py
```

The full command builds all retained projects, runs actual sample runtimes and adapter integration tests, executes Calculator, and builds the frontend. Missing provider configuration is not evidence of a successful provider test. Use fresh fixture databases; store adapters intentionally do not clear a shared database.

After publication, `python3 scripts/verify.py --published` verifies the application and test consumers against the public adapter packages. Release bundles are retained before publication so retries can use the same bytes. The publishing workflow is manually dispatched from `main`, uses the `nuget-production` environment and exchanges GitHub OIDC for a short-lived NuGet credential.

## Documentation

Public guides are Markdown under `docs/public/`, with example snippets matched to the runnable samples. The public Pages site remains in Fifty3North/troolio-docs. Operational test and publishing records belong in progress files or release artifacts, not in the site guides.
