# Solution Deployment Advisor – XrmToolBox Plugin

**Author / Lead Developer:** Osama Mahmoud

An XrmToolBox plugin that analyzes Dataverse solution components across Source and Target environments, advising on safe, ALM-compliant deployment strategies. It automates the generation of smart patches based on component lifecycle, missing target layers, and risk assessment.

---

## Documentation

- 📖 **[User Guide](./User_Guide.md)** - Instructions on how to use the plugin, from connecting environments to deploying patches.
- ⚙️ **[Technical Documentation](./Technical_Documentation.md)** - Deep dive into the project's architecture, services, and core analysis engine.

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
```text
%AppData%\MscrmTools\XrmToolBox\Plugins\
```
Then restart XrmToolBox. The plugin will appear in the tool library.

---

## Key Features

- **Cross-Environment Layer Analysis:** Intelligently queries `msdyn_componentlayer` to detect component discrepancies between your dev/source environment and QC/Prod target environments.
- **Automated Patch Generation:** Groups components by their Base Solution and generates appropriately versioned patches in one click.
- **Risk & Lifecycle Assessment:** Flags high-risk component deployments (like Tables and Security Roles) and marks components as New, Updated, or Unchanged.
- **Export Capabilities:** Export your analysis to CSV or generate PAC CLI scripts for automated CI/CD pipelines.

---
*Developed by Osama Mahmoud*
