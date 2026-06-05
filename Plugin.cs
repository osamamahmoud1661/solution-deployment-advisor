using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace SolutionDeploymentAdvisor
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Solution Deployment Advisor")]
    [ExportMetadata("Description", "Analyzes Dataverse solutions and components to advise on safe deployment strategies.")]
    [ExportMetadata("SmallImageBase64", "")]
    [ExportMetadata("BigImageBase64", "")]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SecondaryFontColor", "Gray")]
    public class Plugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new UI.MainControl();
        }
    }
}
