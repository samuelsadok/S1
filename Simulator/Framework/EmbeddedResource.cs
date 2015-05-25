using System;
using System.Linq;
using System.Reflection;
using System.IO;

namespace AppInstall.Framework
{
    /// <summary>
    /// Provides access to the embedded resources in the executing assembly.
    /// </summary>
    public static class EmbeddedResource
    {

        /// <summary>
        /// Loads the embedded resource with the specified name.
        /// The actual names are preceded by the application namespace, hence this function will load any file that ends with .[name].
        /// Such a file must be unique in the assembly.
        /// </summary>
        public static Stream Load(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames().Where((res) => res.EndsWith("." + name)).ToArray();
            var resourceName = names.SingleOrDefault();

            if (resourceName == null)
                throw new Exception("failed to load embedded resource \"" + name + ": " + (names.Any() ? "the name is ambiguous between: " + string.Join(", ", names) : "the resource was not found"));

            return assembly.GetManifestResourceStream(resourceName);
        }


        /// <summary>
        /// Loads an embedded resource as binary data.
        /// </summary>
        public static byte[] LoadBinary(string name)
        {
            using (var stream = Load(name))
                return stream.ReadToEnd(ApplicationControl.ShutdownToken).WaitForResult(ApplicationControl.ShutdownToken);
        }


        /// <summary>
        /// Loads an embedded resource as a string.
        /// </summary>
        public static string LoadString(string name)
        {
            using (var stream = Load(name)) {
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

    }
}