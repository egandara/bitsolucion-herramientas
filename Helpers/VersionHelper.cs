using System.Reflection;

namespace NotebookValidator.Web.Helpers
{
    public static class VersionHelper
    {
        public static string GetVersion()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "N/A";
        }
    }
}
