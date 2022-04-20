# getcert
Simple tool to download certificate(s) from given URL

# Note
This is little two-hours project written just for my purposes: to have such a simple tool without need to run openssl, to try CommandLineParser library (https://github.com/commandlineparser) and finally to write a few lines of C# code again:)   

# Usage
```shell
getcert -u|--url URL [-c|--chain] [-i|--info] [-d|--dir directory] [-a|--alias filename]
```
| **Option**            | **Required** | **Default value** | **Description**                     |
|-----------------------|--------------|-------------------|-------------------------------------|
| **-u** or **--url**   | Yes          | no default value  | URL to get certificates from        |
| **-c** or **--chain** | No           | false             | Get all certificates in chain       |
| **-i** or **--info**  | No           | false             | Get certificate(s) info only        |
| **-d** or **--dir**   | No           | ""                | Directory to save certificate(s) to |
| **-a** or **--alias** | No           | "certificate"     | Filename to save certificate(s) as  |
| **--help**            |              |                   | Display help screen                 |
| **--version**         |              |                   | Display version information         |

