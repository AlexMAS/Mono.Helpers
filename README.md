# Mono.Helpers

Extensions for Mono/Linux: processes, signals, services, daemons and etc.

## Install Daemons

```csharp
var installSettings = new LinuxServiceSettings
                      {
                          ServiceName = "MyAppService",
                          DisplayName = "Short Description",
                          Description = "Full Description",
                          ServiceExe = "/home/user1/MyApp/MyApp.exe",
                          ServiceArgs = "Arg1 Arg2 Arg3",
                          Username = "user1",
                          Dependencies = new[] { "$local_fs", "$network", "$remote_fs", "$syslog" }
                      };

System.Configuration.Install.Installer installer = new LsbLinuxHostInstaller(installSettings);

var stateSaver = new Hashtable();

installer.Install(stateSaver);

installer.Uninstall(stateSaver);
```

## Start/Stop Daemons

```csharp
var service = LsbLinuxServiceController.GetService("MyAppService");
service.Start();
service.Stop();
```

## Signals

```csharp
var signalListener = new LinuxSignalListener();
signalListener.Subscribe(Signum.SIGTERM, () => Console.WriteLine("Termination signal"));
signalListener.Subscribe(Signum.SIGINT, () => Console.WriteLine("Terminal interrupt signal"));
signalListener.Listen();
```

## Processes

```csharp
foreach (var process in LinuxProcess.GetProcesses())
{
    Console.WriteLine("PID={0}, Name={1}, State={2}, CommandLine={3}",
        process.Id, process.ProcessName, process.ProcessState, process.CommandLine);
}
```

## IPC

```csharp
// Server
using (var server = new FileChannelServer("MathService"))
{
    server.Subscribe("Add", (dynamic args) =>  args.Left + args.Right);
    server.Subscribe("Sub", (dynamic args) =>  args.Left - args.Right);
    server.Subscribe("Mul", (dynamic args) =>  args.Left * args.Right);
    server.Subscribe("Div", (dynamic args) =>  args.Left / args.Right);

    server.Start();

    Console.ReadLine();
}

// Client
using (var client = new FileChannelClient("MathService"))
{
    var addResult = client.Invoke("Add", new { Left = 2, Right = 2 });
    Console.WriteLine("2 + 2 = {0}", addResult); 

    var subResult = client.Invoke("Sub", new { Left = 5, Right = 2 });
    Console.WriteLine("5 - 2 = {0}", subResult);

    var mulResult = client.Invoke("Mul", new { Left = 3, Right = 2 });
    Console.WriteLine("3 * 2 = {0}", mulResult);

    var divResult = client.Invoke("Div", new { Left = 6, Right = 2 });
    Console.WriteLine("6 / 2 = {0}", divResult);
}
```

## NuGet

https://www.nuget.org/packages/Mono.Helpers/

```powershell
Install-Package Mono.Helpers
```

## Notes

Currently Mono.Helpers code is tested only on Ubuntu 14.04.2 LTS.
