# AGENTS.md

## Project Overview

- Name: `getcert`
- Type: small C# CLI utility
- Framework: .NET Framework `4.7.2`
- Entry point: `getcert/Program.cs`
- Purpose: fetch TLS certificate(s) from an HTTPS endpoint, print/export PEM, optionally save to disk.

## Agent Goals

- Keep behavior stable unless explicitly asked to change it.
- Prefer small, reviewable edits over large refactors.
- Preserve CLI compatibility (`-u`, `-c`, `-i`, `-d`, `-a`, `-h`).

## Build And Validation

- Debug build: `dotnet build getcert.sln`
- Release build: `dotnet build getcert.sln -c Release`
- Release output: `getcert/bin/Release/getcert.exe`
- Always run build after code edits.
- If behavior changes, include at least one concrete invocation example in the final summary.

## CLI Behavior Contract

- `-u|--url` is required.
- `-i|--info` prints certificate info only.
- `-d|--dir` defines output directory for downloaded certificate files (is ignored when `-i|--info` is used, and should emit a warning).
- `-c|--chain` includes full chain; otherwise only leaf cert.
- `-a|--alias` controls output filename prefix (is ignored when `-i|--info`).

## Editing Rules

- Prefer minimal changes.
- Do not introduce new dependencies unless requested.
- Keep language features compatible with current project settings.
- Avoid changing assembly metadata/versioning unless requested.

## Error Handling Expectations

- Return clear user-facing errors for invalid URL, invalid output directory, invalid alias.
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
