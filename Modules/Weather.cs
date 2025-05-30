﻿using System.Data;
using System.Diagnostics;
using System.Globalization;

using DataImportClientLegacy.Scripts;
using DataImportClientLegacy.Ressources;
using static DataImportClientLegacy.Ressources.ModuleConfigurations;

using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;





namespace DataImportClientLegacy.Modules
{
    internal struct WeatherData
    {
        internal decimal windSpeed;
        internal decimal temperature;
    }



    internal class Weather
    {
        private const string _currentSection = "ModuleWeather";

        private ModuleState _moduleState;
        private static bool _serviceRunning;
        private static int _errorCount;

        private static int _navigationXPosition = 1;
        private static readonly int _countOfMenuOptions = 4;

        private static string _dateOfLastImport = string.Empty;
        private static string _dateOfLastLogFileEntry = string.Empty;

        private static string _formattedErrorCount = string.Empty;
        private static string _formattedServiceRunning = string.Empty;
        private static string _formattedLastLogFileEntry = string.Empty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private static Task _importWorker = new(() => { });

        private static CancellationTokenSource _cancellationTokenSource = new();

        private static readonly ApplicationSettings.Paths _appPaths = new();



        internal ModuleState State
        {
            get => _moduleState;
            set
            {
                if (_moduleState != value)
                {
                    ActivityLogger.Log(_currentSection, $"Module state changed from '{_moduleState}' to '{value}'.");
                    _moduleState = value;

                    OnStateChange();
                }
            }
        }

        internal event EventHandler? StateChanged;

        protected virtual void OnStateChange()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        internal int ErrorCount
        {
            get => _errorCount;
        }



        internal Weather()
        {
            _moduleState = ModuleState.Running;
            _errorCount = 0;
            _serviceRunning = true;

            _dateOfLastImport = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");
            _dateOfLastLogFileEntry = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");



            _cancellationTokenSource = new();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            _importWorker = Task.Run(() => ImportApiData(cancellationToken));
        }



        internal async Task Main()
        {
            ActivityLogger.Log(_currentSection, "Entering module 'Weather'.");



            Console.Clear();

        LabelDrawUi:

            Console.SetCursorPosition(0, 4);



            ActivityLogger.Log(_currentSection, "Formatting menu variables.");

            FormatMenuVariables();

            ActivityLogger.Log(_currentSection, "Starting to draw the main menu.");

            DisplayMenu();

            ActivityLogger.Log(_currentSection, "Displayed main menu, waiting for key input.");



            ConsoleKey pressedKey = Console.ReadKey(true).Key;

            switch (pressedKey)
            {
                case ConsoleKey.DownArrow:
                    if (_navigationXPosition + 1 <= _countOfMenuOptions)
                    {
                        _navigationXPosition += 1;
                        ActivityLogger.Log(_currentSection, $"Changed menu option from '{_navigationXPosition - 1}' to '{_navigationXPosition}'.");
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (_navigationXPosition - 1 >= 1)
                    {
                        _navigationXPosition -= 1;
                        ActivityLogger.Log(_currentSection, $"Changed menu option from '{_navigationXPosition + 1}' to '{_navigationXPosition}'.");
                    }
                    break;

                case ConsoleKey.Escape:
                    ActivityLogger.Log(_currentSection, "Returning to the main menu via 'ESC'.");
                    return;

                case ConsoleKey.Backspace:
                    ActivityLogger.Log(_currentSection, "Returning to the main menu via 'BACKSPACE'.");
                    return;

                default:
                    break;
            }



            if (pressedKey != ConsoleKey.Enter)
            {
                goto LabelDrawUi;
            }



            switch (_navigationXPosition)
            {
                case 1:
                    try
                    {
                        string importWorkerLogsFolder = _appPaths.weatherImportWorkerLogs;
                        Process.Start("explorer.exe", importWorkerLogsFolder);

                        ActivityLogger.Log(_currentSection, "Opened the folder for the import worker logs of the current module.");
                    }
                    catch (Exception exception)
                    {
                        ActivityLogger.Log(_currentSection, "[ERROR] Failed to open the folder for import worker logs of the current module.");
                        ActivityLogger.Log(_currentSection, exception.Message, true);

                        string title = "Failed to perform this action.";
                        string description = "Please check the error log for detailed information.";

                        await ConsoleHelper.DisplayInformation(title, description, ConsoleColor.Red);
                    }
                    break;

                case 2:
                    if (_serviceRunning == true)
                    {
                        ActivityLogger.Log(_currentSection, "Stopping the active import worker of the current module.");
                        ImportWorkerLog("Stopping the active import worker of the current module.");

                        _cancellationTokenSource?.Cancel();
                        _moduleState = ModuleState.Stopped;
                        _serviceRunning = false;
                        break;
                    }

                    ActivityLogger.Log(_currentSection, "Starting a new import worker for the current module.");
                    ImportWorkerLog(string.Empty, true);
                    ImportWorkerLog("Starting a new import worker for the current module.");



                    if (ErrorCount <= 0)
                    {
                        _moduleState = ModuleState.Running;
                    }

                    _cancellationTokenSource = new();
                    CancellationToken cancellationToken = _cancellationTokenSource.Token;

                    _serviceRunning = true;

                    _importWorker = Task.Run(() => ImportApiData(cancellationToken));

                    break;

                case 3:
                    ActivityLogger.Log(_currentSection, $"Clearing errors for the current module. Previous error count: '{_errorCount}'.");

                    if (State != ModuleState.Stopped)
                    {
                        _moduleState = ModuleState.Running;
                    }

                    _errorCount = 0;

                    MainMenu._sectionMiscellaneous.errorCache.RemoveSectionFromCache(_currentSection);

                    break;

                case 4:
                    ActivityLogger.Log(_currentSection, "Returning to the main menu via selection.");
                    return;
            }



            Console.Clear();
            goto LabelDrawUi;
        }

        private static void DisplayMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("              ┓ ┏     ┓                                        ");
            Console.WriteLine("              ┃┃┃┏┓┏┓╋┣┓┏┓┏┓                                   ");
            Console.WriteLine("              ┗┻┛┗ ┗┻┗┛┗┗ ┛                                    ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("             ──────────────────                                ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("                                                               ");
            Console.WriteLine("                                                               ");
            Console.WriteLine("             │   Last import:  {0}   │                         ", _dateOfLastImport);
            Console.WriteLine("             └─────────────────────────────────────────┘       ");
            Console.WriteLine("                                                               ");
            Console.WriteLine("                                                               ");
            Console.WriteLine("             ┌ Options                             State       ");
            Console.WriteLine("             └──────────────────────┐              ┌───┐       ");
            Console.WriteLine("             {0} Open log file                     │ {1}       ", $"[\u001b[91m{(_navigationXPosition == 1 ? ">" : " ")}\u001b[97m]", _formattedLastLogFileEntry);
            Console.WriteLine("             {0} {1}                     │ {2}       ", $"[\u001b[91m{(_navigationXPosition == 2 ? ">" : " ")}\u001b[97m]", $"{(_serviceRunning ? "Stop service " : "Start service")}", _formattedServiceRunning);
            Console.WriteLine("             {0} Clear errors                      │ {1}       ", $"[\u001b[91m{(_navigationXPosition == 3 ? ">" : " ")}\u001b[97m]", _formattedErrorCount);
            Console.WriteLine("                                                   └───┘       ");
            Console.WriteLine("                                                               ");
            Console.WriteLine("             ┌ Application                                     ");
            Console.WriteLine("             └──────────────────────┐                          ");
            Console.WriteLine("             {0} MainMenu                                      ", $"[\u001b[91m{(_navigationXPosition == 4 ? ">" : " ")}\u001b[97m]");
        }

        private static void FormatMenuVariables()
        {
            _formattedErrorCount = "\u001b[92m√\u001b[97m │ \u001b[92mCleared\u001b[97m";
            _formattedServiceRunning = "\u001b[92m√\u001b[97m │ \u001b[92mRunning\u001b[97m";

            if (_errorCount > 0)
            {
                _formattedErrorCount = $"\u001b[91mx\u001b[97m │ \u001b[91m{_errorCount} {(_errorCount > 1 ? "Errors" : "Error")}\u001b[97m";
            }

            if (_serviceRunning == false)
            {
                _formattedServiceRunning = "\u001b[93mo\u001b[97m │ \u001b[93mStopped\u001b[97m";
            }



            if (!DateTime.TryParseExact(_dateOfLastLogFileEntry, "dd.MM.yyyy - HH:mm:ss", null, DateTimeStyles.None, out DateTime providedDateTime))
            {
                _formattedLastLogFileEntry = "\u001b[96m?\u001b[97m │ \u001b[96mUnknown\u001b[97m";
            }

            if (providedDateTime > DateTime.Now)
            {
                _formattedLastLogFileEntry = "\u001b[96m?\u001b[97m │ \u001b[96mUnknown\u001b[97m";
            }



            TimeSpan difference = DateTime.Now - providedDateTime;

            if (difference.TotalMinutes < 30)
            {
                _formattedLastLogFileEntry = $"\u001b[92m√\u001b[97m │ Updated at '\u001b[92m{_dateOfLastLogFileEntry}\u001b[97m'";

            }
            else if (difference.TotalMinutes >= 30 && difference.TotalMinutes < 60)
            {
                _formattedLastLogFileEntry = $"\u001b[93mo\u001b[97m │ Updated at '\u001b[93m{_dateOfLastLogFileEntry}\u001b[97m'";
            }
            else
            {
                _formattedLastLogFileEntry = $"\u001b[91mx\u001b[97m │ Updated at '\u001b[91m{_dateOfLastLogFileEntry}\u001b[97m'";
            }
        }

        private async Task ImportApiData(CancellationToken cancellationToken)
        {
            ImportWorkerLog(string.Empty, true);
            ImportWorkerLog("Starting a new import worker for the current module.");

            int errorTimoutInMilliseconds = 5 * 30 * 1000;



            while (true)
            {
                ImportWorkerLog("Fetching settings from configuration file.");

                (WeatherConfiguration weatherConfiguration, Exception? occurredError) = await GetConfigurationValues();

                if (occurredError != null)
                {
                    string errorMessage = "An error has occurred while fetching the settings.";
                    string[] errorDetails = [occurredError.Message, occurredError.InnerException?.ToString() ?? string.Empty];
                    ThrowModuleError(errorMessage, errorDetails, ErrorCategory.ConfigurationFetching);

                    ImportWorkerLog($"Waiting for {errorTimoutInMilliseconds / 1000} seconds before continuing with the import process.");

                    await Task.Delay(errorTimoutInMilliseconds, cancellationToken);
                    continue;
                }

                ImportWorkerLog("Successfully fetched settings.");



                string apiUrl = weatherConfiguration.apiUrl;
                string apiKey = weatherConfiguration.apiKey;
                string apiLocation = weatherConfiguration.apiLocation;

                apiUrl += $"?q={apiLocation}&appid={apiKey}&mode=json&units=metric";

                if (int.TryParse(weatherConfiguration.apiIntervalSeconds, out int apiSleepTimer) == false)
                {
                    string errorMessage = "An error has occurred while assigning variables.";
                    string[] errorDetails = ["Failed to parse 'apiIntervalSeconds' to int."];
                    ThrowModuleError(errorMessage, errorDetails, ErrorCategory.IntegerParsing);

                    ImportWorkerLog($"Waiting for {errorTimoutInMilliseconds} seconds before continuing with the import process.");

                    await Task.Delay(errorTimoutInMilliseconds, cancellationToken);
                    continue;
                }



                ImportWorkerLog("Contacting the API and requesting a data set.");

                (WeatherData weatherData, occurredError) = await FetchApiData(apiUrl, cancellationToken);

                if (occurredError != null)
                {
                    string errorMessage = "An error has occurred while fetching data from the API provider.";
                    string[] errorDetails = [occurredError.Message, occurredError.InnerException?.ToString() ?? string.Empty];
                    ThrowModuleError(errorMessage, errorDetails, ErrorCategory.ApiDataFetching);

                    ImportWorkerLog($"Waiting for {errorTimoutInMilliseconds} seconds before continuing with the import process.");

                    await Task.Delay(errorTimoutInMilliseconds, cancellationToken);
                    continue;
                }

                ImportWorkerLog("Successfully fetched the data set from the API.");



                ImportWorkerLog("Inserting the fetched data set into the database.");

                occurredError = await InsertDataIntoDatabase(weatherConfiguration.sqlConnectionString, "Wetterdaten", weatherData, cancellationToken);

                if (occurredError != null)
                {
                    string errorMessage = "An error has occurred while inserting the data into the database.";
                    string[] errorDetails = [occurredError.Message, occurredError.InnerException?.ToString() ?? string.Empty];
                    ThrowModuleError(errorMessage, errorDetails, ErrorCategory.DatabaseInsertion);

                    ImportWorkerLog($"Waiting for {errorTimoutInMilliseconds} seconds before continuing with the import process.");

                    await Task.Delay(errorTimoutInMilliseconds, cancellationToken);
                    continue;
                }

                ImportWorkerLog("Successfully inserted the API data into the database.");



                _dateOfLastImport = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");



                ImportWorkerLog($"Going to sleep for {apiSleepTimer} seconds.");
                await Task.Delay(apiSleepTimer * 1000, cancellationToken);
            }
        }

        private static async Task<(WeatherConfiguration weatherConfiguration, Exception? occurredError)> GetConfigurationValues()
        {
            JObject savedConfiguration;

            try
            {
                savedConfiguration = await ConfigurationHelper.LoadConfiguration();

                if (savedConfiguration["error"] != null)
                {
                    throw new Exception($"Saved configuration file contains errors. Error: {savedConfiguration["error"]}");
                }
            }
            catch (Exception exception)
            {
                return (new WeatherConfiguration(), exception);
            }



            JObject modules;
            JObject weatherModule;
            JObject sqlData;

            try
            {
                modules = savedConfiguration["modules"] as JObject ?? [];

                if (modules == null || modules == new JObject())
                {
                    throw new Exception("Configuration file does not contain a 'modules' object.");
                }

                weatherModule = modules?["weather"] as JObject ?? [];

                if (weatherModule == null || weatherModule == new JObject())
                {
                    throw new Exception("Configuration file does not contain a 'weather' module.");
                }

                sqlData = savedConfiguration["sql"] as JObject ?? [];

                if (sqlData == null || sqlData == new JObject())
                {
                    throw new Exception("Configuration file does not contain a 'sql' object.");
                }
            }
            catch (Exception exception)
            {
                return (new WeatherConfiguration(), exception);
            }



            try
            {
                WeatherConfiguration weatherConfiguration = new()
                {
                    apiUrl = weatherModule?["apiUrl"]?.ToString() ?? string.Empty,
                    apiKey = weatherModule?["apiKey"]?.ToString() ?? string.Empty,
                    apiLocation = weatherModule?["apiLocation"]?.ToString() ?? string.Empty,
                    apiIntervalSeconds = weatherModule?["apiIntervalSeconds"]?.ToString() ?? string.Empty,
                    sqlConnectionString = sqlData?["connectionString"]?.ToString() ?? string.Empty
                };

                if (weatherConfiguration.HoldsInvalidValues() == true)
                {
                    throw new Exception("One or mulitple configuration values are null. Please check the configuration file!");
                }

                if (int.TryParse(weatherConfiguration.apiIntervalSeconds, out int _) == false)
                {
                    throw new Exception("Failed to parse the provided API interval to a number.");
                }

                return (weatherConfiguration, null);
            }
            catch (Exception exception)
            {
                return (new WeatherConfiguration(), exception);
            }
        }

        private static async Task<(WeatherData weatherData, Exception? occurredError)> FetchApiData(string apiUrl, CancellationToken cancellationToken)
        {
            JObject apiData = [];
            using HttpClient httpClient = new();

            int maxRequestRetries = 5;

            for (int i = 0; i < maxRequestRetries; i++)
            {
                try
                {
                    string apiJsonData = await httpClient.GetStringAsync(apiUrl, cancellationToken);

                    apiData = JObject.Parse(apiJsonData);

                    break;
                }
                catch (HttpRequestException httpRequestException) when (i < maxRequestRetries - 1)
                {
                    ImportWorkerLog($"[WARNING] - [Iteration {i}] - Exception of type 'HttpRequestException' was thrown.");

                    if (httpRequestException.InnerException is System.Security.Authentication.AuthenticationException)
                    {
                        ImportWorkerLog("Exception is most likely related to an SSL error.", true);
                        ImportWorkerLog(httpRequestException.Message, true);
                    }
                    else
                    {
                        ImportWorkerLog("Exception is most likely related to some general network related issue.", true);
                        ImportWorkerLog(httpRequestException.Message, true);
                    }

                    int cooldownTimer = 1000 * (int)Math.Pow(2, i);
                    ImportWorkerLog($"Retrying in {cooldownTimer / 1000} seconds.", true);

                    await Task.Delay(cooldownTimer, cancellationToken);
                }
                catch (Exception exception)
                {
                    return (new WeatherData(), exception);
                }
            }



            if (apiData == null || apiData == new JObject())
            {
                return (new WeatherData(), new Exception("The fetched data provided by the API is 'null'."));
            }

            ImportWorkerLog("Successfully received the requested data from the API.");



            try
            {
                string dataWindSpeed = apiData?["wind"]?["speed"]?.ToString() ?? string.Empty;
                string dataTemperature = apiData?["main"]?["temp"]?.ToString() ?? string.Empty;

                bool validDecimalApiValues = ConsoleHelper.ValidDecimalValues([dataWindSpeed, dataTemperature]);

                if (validDecimalApiValues == false)
                {
                    throw new Exception($"The fetched API data contains invalid values. dataWindSpeed: '{dataWindSpeed}' | dataTemperature: '{dataTemperature}'");
                }

                WeatherData weatherData = new()
                {
                    windSpeed = Convert.ToDecimal(dataWindSpeed),
                    temperature = Convert.ToDecimal(dataTemperature)
                };

                return (weatherData, null);
            }
            catch (Exception exception)
            {
                return (new WeatherData(), exception);
            }
        }

        private static async Task<Exception?> InsertDataIntoDatabase(string sqlConnectionString, string dbTableName, WeatherData weatherData, CancellationToken cancellationToken)
        {
            SqlConnection databaseConnection = new(sqlConnectionString);

            try
            {
                await databaseConnection.OpenAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return exception;
            }

            ImportWorkerLog("Successfully established a database connection.");



            try
            {
                DateTime now = DateTime.Now;
                DateTime datum = now.Date;
                TimeSpan zeit = now.TimeOfDay;

                string queryNames = "Windgeschw, Aussentemp, Datum, Zeit";
                string queryValues = "@Windgeschw, @Aussentemp, @Datum, @Zeit";
                string insertDataQuery = $"INSERT INTO {dbTableName} ({queryNames}) VALUES ({queryValues});";

                using SqlCommand insertCommand = new(insertDataQuery, databaseConnection);

                insertCommand.Parameters.Add("@Datum", SqlDbType.Date).Value = datum;
                insertCommand.Parameters.Add("@Zeit", SqlDbType.Time).Value = zeit;
                insertCommand.Parameters.AddWithValue("@Windgeschw", weatherData.windSpeed);
                insertCommand.Parameters.AddWithValue("@Aussentemp", weatherData.temperature);

                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return exception;
            }

            return null;
        }

        private static void ImportWorkerLog(string message, bool removePrefix = false)
        {
            ImportLogger.Log(_currentSection, message, removePrefix);
            _dateOfLastLogFileEntry = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");
        }

        private void ThrowModuleError(string errorMessage, string[] errorDetails, ErrorCategory errorCategory)
        {
            ImportWorkerLog($"[ERROR] - {errorMessage}");

            foreach (string errorDetail in errorDetails)
            {
                if (string.IsNullOrWhiteSpace(errorDetail) == false)
                {
                    ImportWorkerLog(errorDetail, true);
                }
            }

            string firstErrorDetail;

            if (errorDetails.Length > 0)
            {
                firstErrorDetail = errorDetails[0];
            }
            else
            {
                firstErrorDetail = "There were no details for this error specified.";
            }

            MainMenu._sectionMiscellaneous.errorCache.AddEntry(_currentSection, errorMessage, firstErrorDetail, errorCategory);

            State = ModuleState.Error;
            _errorCount++;
        }
    }
}