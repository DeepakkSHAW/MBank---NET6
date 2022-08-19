using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBank.Lib
{
    public class ApplicationInfo
    {
        private readonly ILogger<ApplicationInfo> _logger;
        private readonly IConfiguration _config;
        public ApplicationInfo(ILogger<ApplicationInfo> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public string GetApplication()
        {
            var appName = _config.GetSection("ApplicationInfo:Application").Value;
            if (appName == null)
            {
                _logger.LogError("appName >> must not be null or empty");
                throw new ArgumentNullException("appName >> must not be null or empty");
            }
            if (string.IsNullOrWhiteSpace(appName))
            {
                _logger.LogError("appName >> must not be null or empty");
                throw new ArgumentException("appName >> must not be null or empty");
            }
            return appName;
        }
        public string GetApplicationVersion()
        {
            var appVersion = _config.GetSection("ApplicationInfo:Version").Value;
            return appVersion;
        }
        public string GetDesignerInfo()
        {
            var appDesignedBy = _config.GetSection("ApplicationInfo:DesignedBy").Value;
            return appDesignedBy;
        }
    }
}
