# getcert
Simple tool to download, display and save certificate(s) from given URL. 

# Note
This little two-hours project has been written just for my purposes: to have such a simple tool without need to run openssl, to try interesting CommandLineParser library (https://github.com/commandlineparser) and finally to write a few lines of C# code again:)   

# Usage
```shell
getcert -u|--url URL [-c|--chain] [-i|--info] [-d|--dir directory] [-a|--alias filename]
```
| **Option**        | **Required** | **Default value** | **Description**                                                                                                 |
|-------------------|--------------|-------------------|-----------------------------------------------------------------------------------------------------------------|
| `-u` or `--url`   | Yes          | no default value  | URL to get certificates from                                                                                    |
| `-c` or `--chain` | No           | false             | Get and display all certificates in chain  <br>  <br>If not used, only first certificate in chain is downloaded |
| `-i` or `--info`  | No           | false             | Get and display certificate(s) info only  <br>  <br>When used, saving options (`-d`, `-a`) are ignored          |
| `-d` or `--dir`   | No           | ""                | Directory to save certificate(s) to  <br>  <br>Existing directory must be provided                              |
| `-a` or `--alias` | No           | "certificate"     | Filename to save certificate(s)                                                                                 |
| `--help`          |              |                   | Display help screen                                                                                             |
| `--version`       |              |                   | Display version information                                                                                     |


Certificate(s) can be saved in PEM format into given directory (with `-d` or `--dir` option) under filename `certificate-x.crt` where `-x` part states order of particular certificate in chain. User can use an alias option (`-a` or `--alias`) to replace `"certificate"` filename part with custom name.

For example following command:
```shell
getcert -u github.com -c -d c:\temp -a github
```
downloads all certificates in chain (three certificates) from `https://github.com` and saves them in `c:\temp directory` under filenames `github-0.crt`, `github-1.crt` and `github-2.crt`

When directory option is not provided certificate(s) content and properties are only displayed in command line.

# Dependencies

CommandLineParser https://github.com/commandlineparser
