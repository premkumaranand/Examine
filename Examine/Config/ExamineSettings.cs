using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Examine.Config
{
    /// <summary>
    /// Config section for Examine
    /// </summary>
    public class ExamineSettings : ConfigurationSection
    {
        private const string SectionName = "Examine";

        /// <summary>
        /// Gets the instance of the Examine settings.
        /// </summary>
        /// <value>The instance.</value>
        public static ExamineSettings GetDefaultInstance()
        {
            return ConfigurationManager.GetSection(SectionName) as ExamineSettings;
        }

        /// <summary>
        /// Gets the search providers.
        /// </summary>
        /// <value>The search providers.</value>
        [ConfigurationProperty("searchProviders")]
        public SearchProvidersSection SearchProviders
        {
            get { return (SearchProvidersSection)base["searchProviders"]; }
        }

        /// <summary>
        /// Gets the index providers.
        /// </summary>
        /// <value>The index providers.</value>
        [ConfigurationProperty("indexProviders")]
        public IndexProvidersSection IndexProviders
        {
            get { return (IndexProvidersSection)base["indexProviders"]; }
        }

    }
}
