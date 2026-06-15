Build the AppAI.Web .NET project.

Kills any running AppAI.Web.exe process (which locks output DLLs), then runs:

```
dotnet build AppAI.Web/AppAI.Web.csproj --verbosity minimal
```

**Steps**:
1. Check if `AppAI.Web.exe` is running (`Get-Process -Name "AppAI.Web"`). If it is, stop it with `Stop-Process -Force` or `taskkill /F /PID <id>`. If access is denied, ask the user to kill it manually (elevated process).
2. Run `dotnet build AppAI.Web/AppAI.Web.csproj --verbosity minimal` from the repo root.
3. Report: succeeded/failed, warning count, error count, elapsed time. On failure, show the actual error lines (not the MSB3026 retry noise).
