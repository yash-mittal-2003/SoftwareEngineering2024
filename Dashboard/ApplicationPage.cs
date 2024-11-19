using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    /// <summary>
    /// Enum representing different pages in the application.
    /// </summary>
    public enum ApplicationPage
    {
        /// <summary>
        /// Login page of the application.
        /// </summary>
        Login = 0,

        /// <summary>
        /// Homepage of the application.
        /// </summary>
        Homepage = 1,

        /// <summary>
        /// Homepage for the server.
        /// </summary>
        ServerHomePage = 2,

        /// <summary>
        /// Homepage for the client.
        /// </summary>
        ClientHomePage = 3
    }
}