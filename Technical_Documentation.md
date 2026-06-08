# Technical Documentation

**Project:** Solution Deployment Advisor  
**Author:** Osama Mahmoud  

This document outlines the architecture, core engines, and underlying Dataverse API integrations that power the Solution Deployment Advisor.

---

## 1. Architecture Overview

The project is structured into distinct layers following a clean separation of concerns:
- `UI/`: WinForms controls handling user interaction and XrmToolBox integration.
- `Core/`: The brain of the plugin. Orchestrates analysis, risk evaluation, and versioning strategies.
- `Services/`: Wrappers around the `IOrganizationService` that interact directly with Dataverse tables and virtual entities.
- `Models/`: Plain Old C# Objects (POCOs) defining the domain (e.g., `ComponentInfo`, `LayerInfo`, `SolutionPreview`).
- `Components/`: Type-specific analyzers extending the base analysis logic.

---

## 2. Core Engines (`/Core`)

### `AnalysisEngine`
The primary orchestrator. When an analysis is triggered:
1. It fetches all solution components from the Source environment.
2. It uses `ComponentNameResolver` to map raw GUIDs to human-readable names.
3. It queries the Target environment to map those names back to Target GUIDs (specifically handling anomalies like `SystemForm` GUID mismatches).
4. It calls the `LayerService` to pull the physical solution layers from both environments.
5. It compares the layers to determine missing patches and lifecycle states.

### `RiskEngine`
Evaluates the deployment risk of a component.
- **High Risk:** Security Roles, Field Security Profiles.
- **Medium Risk:** Tables, Columns, Relationships, Cloud Flows, Plugins.
- **Low Risk:** Canvas Apps, Forms, Views, Web Resources.

### `VersionStrategyEngine`
Calculates the next logical version number for a patch based on existing target versions, ensuring the new patch version is always strictly greater than the highest version currently deployed in the target.

---

## 3. Services (`/Services`)

### `LayerService`
The most critical service in the tool. It queries the Dataverse virtual entity `msdyn_componentlayer` to accurately determine which managed and unmanaged layers a component belongs to. 
- Automatically filters out Dataverse system layers (`"Active"` and `"Default"`) to prevent unmanaged customizations from skewing the patch version logic.
- Implements a robust fallback querying the `solutioncomponent` table for metadata types not supported by `msdyn_componentlayer`.

### `ComponentNameResolver`
Since Dataverse stores components as raw `objectid` GUIDs, this service queries the specific metadata tables (e.g., `systemform`, `savedquery`, `workflow`) in bulk chunks to map GUIDs to friendly display names quickly, avoiding N+1 query performance issues.

### `SolutionService & SolutionComponentService`
Handles the creation of patches using the `CloneAsPatchRequest` API, and subsequently adds components to those patches using the `AddSolutionComponentRequest` API.

---

## 4. UI Layer (`/UI`)

### `MainControl`
The entry point of the XrmToolBox plugin. Manages connection states for both Source and Target environments, handles the main asynchronous worker threads (`WorkAsync`), and renders the DataGridView.

### `ConfirmDialog`
Presents the generated `SolutionPreview` list to the developer. 
- Features a two-pane split container.
- Allows developers to directly edit the **Solution/Patch Name** before creation by mapping `DataGridView.CellEndEdit` events back to the underlying `SolutionPreview` models.

### `PostCreationDialog`
A simple success summary dialog shown after patches are successfully created via the `SolutionService`.
