﻿namespace DataImportClientLegacy.Ressources
{
    internal class ModuleConfigurations
    {
        internal struct WeatherConfiguration
        {
            internal string apiUrl;
            internal string apiKey;
            internal string apiLocation;
            internal string apiIntervalSeconds;
            internal string sqlConnectionString;

            internal readonly bool HoldsInvalidValues()
            {
                var stringFields = new string[] { apiUrl, apiKey, apiLocation, apiIntervalSeconds, sqlConnectionString };
                return stringFields.Any(string.IsNullOrEmpty);
            }
        }

        internal struct ElectricityConfiguration
        {
            internal string sourceFilePath;
            internal string sourceFilePattern;
            internal string sourceFileIntervalSeconds;
            internal string sqlConnectionString;

            internal readonly bool HoldsInvalidValues()
            {
                var stringFields = new string[] { sourceFilePath, sourceFilePattern, sourceFileIntervalSeconds, sqlConnectionString };
                return stringFields.Any(string.IsNullOrEmpty);
            }
        }

        internal struct DistrictHeatConfiguration
        {
            internal string sourceFilePath;
            internal string sourceFilePattern;
            internal string sourceFileIntervalSeconds;
            internal string sqlConnectionString;

            internal readonly bool HoldsInvalidValues()
            {
                var stringFields = new string[] { sourceFilePath, sourceFilePattern, sourceFileIntervalSeconds, sqlConnectionString };
                return stringFields.Any(string.IsNullOrEmpty);
            }
        }

        internal struct PhotovoltaicConfiguration
        {
            internal string solarwebEmail;
            internal string solarwebPassword;
            internal string solarwebSystemId;
            internal string apiIntervalSeconds;
            internal string sqlConnectionString;

            internal readonly bool HoldsInvalidValues()
            {
                var stringFields = new string[] { solarwebEmail, solarwebPassword, solarwebSystemId, apiIntervalSeconds, sqlConnectionString };
                return stringFields.Any(string.IsNullOrEmpty);
            }
        }
    }
}