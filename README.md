# Solution Deployment Advisor – XrmToolBox Plugin

An XrmToolBox plugin that analyzes Dataverse solution components and advises on safe,
ALM-compliant deployment strategies.

---

## Getting Started

### Prerequisites
- Visual Studio 2022
- .NET Framework 4.6.2
- XrmToolBox installed on your machine

### Build
1. Open `SolutionDeploymentAdvisor.csproj` in Visual Studio.
2. Restore NuGet packages (`dotnet restore` or via VS Package Manager).
3. Build → the output DLL lands in `bin\Debug\net462\`.

### Install in XrmToolBox
Copy `SolutionDeploymentAdvisor.dll` (and its dependencies) into:
```
%AppData%\MscrmTools\XrmToolBox\Plugins\
```
Then restart XrmToolBox. The plugin will appear in the tool library.

---

## Project Layout

| Folder | Purpose |
|--------|---------|
| `UI/` | WinForms controls (main grid, confirm dialog) |
| `Models/` | Plain data objects |
| `Services/` | Dataverse API wrappers |
| `Components/` | Per-component-type analyzers |
| `Core/` | Orchestration engines |

---

## Extending the Plugin

### Add a new component analyzer
1. Create a class in `Components/` implementing `IComponentAnalyzer`.
2. Set `ComponentType` to the Dataverse type code.
3. Register it in `AnalysisEngine` constructor.

### Recommended next enhancements
- [ ] Dependency graph visualization
- [ ] Risk scoring (Red / Yellow / Green per component)
- [ ] PAC CLI script export
- [ ] Azure DevOps pipeline gate integration
