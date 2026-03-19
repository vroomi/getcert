# getcert

Simple tool to download, display and save certificate(s) from given HTTPS endpoint.

# What's new

**1.0.0.6** - `get` command now accepts required URL as a positional argument (`getcert get <url>`), while `-u|--url` remains supported for compatibility

**1.0.0.5** - introduced command-based CLI via `getcert get ...`

**1.0.0.4** - chores with Codex && Copilot

**1.0.0.3** - refactored with OpenAI Codex, removed CommandLineParser dependency

# Note

This small two-hour project was written just for my own purposes — to have a simple tool without needing to run OpenSSL, and to finally write a few lines of C# code again. Now I refactored it a bit just to try OpenAI Codex in VS Code.

# Known limitations

For some websites requiring certificate authentication cannot be established SSL/TLS channel. Will be solved in some of future updates.

# Usage

```shell
getcert <command> [options]
```

Currently supported command:

```shell
getcert get URL [-c|--chain] [-i|--info] [-d|--dir directory] [-a|--alias filename]
```
| **Option**        | **Required** | **Default value** | **Description**                                                                                                 |
|-------------------|--------------|-------------------|-----------------------------------------------------------------------------------------------------------------|
| `URL`             | Yes          | no default value  | URL or HTTPS host to get certificates from                                                                      |
| `-c` or `--chain` | No           | false             | Get and display all certificates in chain  <br>  <br>If not used, only first certificate in chain is downloaded |
| `-i` or `--info`  | No           | false             | Get and display certificate(s) info only  <br>  <br>When used, saving options (`-d`, `-a`) are ignored          |
| `-d` or `--dir`   | No           | ""                | Directory to save certificate(s) to  <br>  <br>Existing directory must be provided                              |
| `-a` or `--alias` | No           | "certificate"     | Filename prefix to save certificate(s)                                                                          |
| `-h` or `--help`  | No           |                   | Display help screen for the `get` command                                                                       |

For backward compatibility, `-u` or `--url` can still be used instead of the positional `URL` argument.


Certificate(s) can be saved in PEM format into given directory (with `-d` or `--dir` option) under filename `certificate-x.crt` where `-x` part states order of particular certificate in chain. User can use an alias option (`-a` or `--alias`) to replace `"certificate"` filename part with custom name. Existing export and info functionality is now available under the `get` command.

For example, following command:

```shell
getcert get www.google.com -i
```

prints certificate information for `https://www.google.com` without printing PEM contents.

For example, following command:

```shell
getcert get github.com -c -d c:\temp -a github
```

downloads all certificates in chain (three certificates) from `https://github.com` and saves them in `c:\temp` directory under filenames `github-0.crt`, `github-1.crt` and `github-2.crt`

When directory option is not provided certificate(s) content and properties are only displayed in command line.

