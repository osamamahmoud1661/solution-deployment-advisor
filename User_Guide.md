# User Guide: Solution Deployment Advisor

Welcome to the **Solution Deployment Advisor** for XrmToolBox, developed by **Osama Mahmoud**. This plugin takes the guesswork out of deploying Microsoft Dataverse solutions by analyzing component layers across environments and automating patch creation.

---

## 1. Connecting Environments

When you open the plugin in XrmToolBox:
1. **Source Environment:** Click **⚡ Connect Source** (or use the existing XrmToolBox connection) to connect to your Development/Source environment.
2. **Target Environment:** Click **⚡ Connect Target** to connect to the environment you intend to deploy to (e.g., QA, UAT, or Production).

*Note: You must have active connections to both environments to perform a full analysis.*

## 2. Selecting Solutions

Once connected:
1. **Source Solution:** Select the unmanaged solution from your Source environment that contains the changes you want to deploy.
2. **Target Solution :** globally check for component collisions.

## 3. Running the Analysis

Click the **Analyze** button. The plugin will:
- Retrieve all components from the Source solution.
- Query the Target environment to see if these components already exist and what solution layer they currently reside in.
- Evaluate the **Risk Level** of deploying each component.
- Determine the **Lifecycle** (New, Updated, Unchanged).

### Understanding the Grid

After analysis, the grid populates with your components:
- **Component Name & Type:** What the item is (Table, Form, Cloud Flow, etc.).
- **Lifecycle:** 
  - `New`: The component does not exist in the Target environment.
  - `Updated`: The component exists, but the Source version/patch is newer.
  - `Unchanged`: The component is perfectly in sync.
- **Risk:** `High` (Red), `Medium` (Yellow), or `Low` (Green) based on the component type. For example, Data components carry higher risk than UI components.
- **Target Version/Patches:** Shows the actual solution layers the component currently belongs to in the Target environment.

## 4. Creating Solution Patches

Instead of manually creating patches and adding components one by one, the plugin can do this for you:
1. Click **Create Solution(s)/Patch(es)**.
2. The **Review Solution(s)/Patch(es) to Create** dialog will appear.
3. The tool intelligently groups your components based on their true Base Solution.
4. **Editable Patch Names:** You can double-click the **Solution/Patch Name** column in the grid to rename the patches to fit your team's naming conventions.
5. Click **Create**. The plugin will generate the patches in the Source environment and add the respective components to them.

## 5. Exporting Data

- **Export CSV:** Saves the grid analysis to a readable Excel/CSV file.
