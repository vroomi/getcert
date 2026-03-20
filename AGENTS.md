# AGENTS.md

## Project Overview

- Name: `getcert`
- Type: small C# CLI utility
- Framework: .NET Framework `4.7.2`
- Entry point: `getcert/Program.cs`
- Purpose: fetch TLS certificate(s) from an HTTPS endpoint, print/export PEM, optionally save to disk, and display certificate info from local certificate files.

## Agent Goals

- Keep behavior stable unless explicitly asked to change it.
- Prefer small, reviewable edits over large refactors.
- Preserve command-based CLI compatibility for both supported commands:
- `getcert get` (`<url>`, `-c`, `-i`, `-d`, `-a`, `-h`), while keeping legacy `-u|--url` support unless explicitly removed.
- `getcert view` (`<file>`, `-f`, `-h`).

## Build And Validation

- Always run build after code edits.
- If behavior changes, include at least one concrete invocation example in the final summary.

### Platform Any CPU

- Debug build: `dotnet build getcert.sln`
- Release build: `dotnet build getcert.sln -c Release`
- Release output: `getcert/bin/Release/getcert.exe`

### Platform x64

- Debug build: `dotnet build getcert.sln -p:Platform=x64`
- Release build: `dotnet build getcert.sln -c Release -p:Platform=x64`
- Release output: `getcert/bin/x64/Release/getcert.exe`

## CLI Behavior Contract

- Root usage is `getcert <command> [options]`.
- `get` is the command that fetches certificate data from an HTTPS endpoint.
- `view` is the command that displays certificate information from a local file.
- `getcert -h|--help` prints root help with available commands.
- `getcert get -h|--help` prints help for the `get` command.
- `getcert view -h|--help` prints help for the `view` command.
- Unknown commands should return a clear error and show the correct root usage.
- Positional `<url>` is required for `get`.
- Positional `<file>` is required for `view`.
- Legacy `-u|--url` may remain supported for backward compatibility, but should not be required.
- `-i|--info` prints certificate info only.
- `-d|--dir` defines output directory for downloaded certificate files (is ignored when `-i|--info` is used, and should emit a warning).
- `-c|--chain` includes full chain; otherwise only leaf cert.
- `-a|--alias` controls output filename prefix (is ignored when `-i|--info`).
- `-f|--format` is optional for `view`; default is `pem`.
- Supported `view` format values are `pem`, `der`, and `pkcs12` (case-insensitive).
- `pkcs12` is currently recognized by the CLI but should return a clear "not supported yet" error until implemented.
- PEM input may contain multiple certificates; info should be shown for each certificate found.
- Non-certificate PEM blocks should emit a warning and then be ignored.

## Editing Rules

- Prefer minimal changes.
- Do not introduce new dependencies unless requested.
- Keep language features compatible with current project settings.
- Avoid changing assembly metadata/versioning unless requested.
- When a requested CLI or documentation change represents a release, update assembly/file version consistently with README release notes.

## Error Handling Expectations

- Return clear user-facing errors for invalid URL, invalid output directory, invalid alias, invalid input file path, invalid format, and invalid certificate file content.
- Keep warnings explicit and actionable.
- Avoid swallowing exceptions unless there is a clear user-facing fallback.

## Style

- Follow existing C# style in repo.
- Use descriptive method names and small methods.
- Add comments only when logic is non-obvious.

## Out Of Scope By Default

- Migrating to newer .NET target frameworks.
- Replacing the CLI argument approach with external libraries.
- Packaging/publishing changes.
