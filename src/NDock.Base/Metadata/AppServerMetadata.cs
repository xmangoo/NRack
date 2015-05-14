﻿using System;
using System.Linq;
using NDock.Base.Metadata;

namespace NDock.Base
{
    [Serializable]
    public class AppServerMetadata : IAppServerMetadata
    {
        public AppServerMetadata()
        {

        }
        
        public AppServerMetadata(AppServerMetadataAttribute attribute, Type appType)
        {
            Name = attribute.Name;
            AppType = appType.ToString();

            StatusFields = attribute.GetType()
                .GetCustomAttributes(typeof(StatusInfoAttribute), true)
                .OfType<StatusInfoAttribute>().ToArray();
        }

        public static AppServerMetadata GetAppServerMetadata(Type serverType)
        {
            if (serverType == null)
                throw new ArgumentNullException("serverType");

            var topType = serverType;

            var attType = typeof(AppServerMetadataAttribute);

            while (true)
            {
                var atts = serverType.GetCustomAttributes(attType, false);

                if (atts != null && atts.Length > 0)
                {
                    var metatdata = atts[0] as AppServerMetadataAttribute;
                    return new AppServerMetadata(metatdata, topType);
                }

                if (serverType.BaseType == null)
                    return null;

                serverType = serverType.BaseType;
            }
        }

        #region IAppServerMetadata implementation

        public string Name { get; set; }

        public string AppType { get; set; }

        public StatusInfoAttribute[] StatusFields { get; set; }

        #endregion
    }
}
