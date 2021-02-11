using System;
using System.Xml;
using ConfigurationEditor;
using Microsoft.ConfigurationManagement.AdminConsole;
using Microsoft.ConfigurationManagement.AdminConsole.Schema;
using Microsoft.EnterpriseManagement.UI.WpfViews;

namespace ConfigurationEditor
{
    public class ViewDescription : IConsoleView2, IConsoleView
    {
        public Type TypeOfViewController
        {
            get { return typeof(ConfigurationViewControl); }
        }

        public Type TypeOfView
        {
            get
            {
                return typeof(Overview);
            }
        }

        public bool TryConfigure(ref XmlElement persistedConfigurationData)
        {
            return false;
        }

        public bool TryInitialize(ScopeNode scopeNode, AssemblyDescription resourceAssembly, ViewAssemblyDescription viewDetail) // viewAssemblyDescription
        {
            return true;
        }
    }
}
