# HostMonitorCSharpLinux
## Simple application that monitors connection of devices over TCP / ICMP

> Write a program that runs on a linux system which takes the following command line flags:
> Hosts, Port, Interval. The program should ping each of the hosts often enough to detect if they are
> down or up. Keep track of and display relevant metrics that could be used to determine if a host is
> online or offline (example: latency and packet loss, etc.). The results should be displayed with realtime updates on a webpage.

## Tech Stack
This program uses Linux, C# with .Net, and ASP.NET Core minimal API. It utilizes a hosted service with .Net WebApplication builder to call a custom task loop that runs independently, periodically pinging a list of devices provided by the user.
I initially made an app in Python, but I wanted to utilize my C# knowledge as well as expand my knowledge of .NET for web (not .Net Framework). I also was able to learn how to use Linux on my Windows machine using WSL UBUNTU, which works well with Visual Studio Code, aside from some challenges with installing some extensions.

## Acknowledgements
This project was developed with significant assistance from ChatGPT, which I used extensively for architecture guidance, code generation, and explanations of .NET concepts. I worked through the generated code, adapted it to my needs, debugged issues, and learned the underlying technologies in the process.

## Build and Run
```
// make run HOSTS=<Host1,Host2,...> PORT=<Port#> INTERVAL=(seconds)
make run HOSTS=google.com,github.com PORT=80 INTERVAL=5
```
