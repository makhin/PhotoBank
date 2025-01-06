using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.MAUI.Blazor
{
    public static class Constants
    {
        // URL of REST service (Android does not use localhost)
        // Use http cleartext for local deployment. Change to https for production

        public static string LocalhostUrl = "strix";
        public static string Scheme = "https"; // or http
        public static string Port = "7041";
        public static string RestUrl = $"{Scheme}://{LocalhostUrl}:{Port}/api/photos/{{0}}";
    }
}
