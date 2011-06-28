using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Examine.Config
{
    /// <summary>
    /// Config section for the Examine Index Providers
    /// </summary>
    public class IndexProvidersSection : ConfigurationElement, IEnumerable<ProviderSettings>
    {

        /// <summary>
        /// Gets the indexing providers.
        /// </summary>
        /// <value>The providers.</value>
        [ConfigurationProperty("providers")]
        public ProviderSettingsCollection Providers
        {
            get { return (ProviderSettingsCollection)base["providers"]; }
        }

        public IEnumerator<ProviderSettings> GetEnumerator()
        {
            return Providers.Cast<ProviderSettings>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Providers.GetEnumerator();
        }
    }
}
