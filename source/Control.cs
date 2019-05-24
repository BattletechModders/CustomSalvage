using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BattleTech;
using HBS.Logging;
using HBS.Util;


namespace CustomSalvage
{


    public static class Control
    {
        public static CustomSalvageSettings Settings = new CustomSalvageSettings();

        private static ILog Logger;
        private static FileLogAppender logAppender;


        internal static RecoveryDelegate NeedRecovery;
        internal static LostUnitActionDelegate LostUnitAction;
        internal static PartsNumDelegeate GetNumParts;

        public static void Init(string directory, string settingsJSON)
        {
            Logger = HBS.Logging.Logger.GetLogger("CustomSalvage", LogLevel.Debug);

            try
            {
                try
                {
                    Settings = new CustomSalvageSettings();
                    JSONSerializationUtility.FromJSON(Settings, settingsJSON);
                    HBS.Logging.Logger.SetLoggerLevel(Logger.Name, Settings.LogLevel);
                }
                catch (Exception)
                {
                    Settings = new CustomSalvageSettings();
                }

                Settings.Complete();
                SetupLogging(directory);

                var harmony = HarmonyInstance.Create("io.github.denadan.CustomSalvage");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Logger.Log("Loaded CustomSalvage v0.1.3 for bt 1.5.1");

                switch (Settings.RecoveryType)
                {
                    case RecoveryCalculationType.AllwaysRecover:
                        NeedRecovery = (result, contract) => true;
                        break;
                    case RecoveryCalculationType.NeverRecover:
                        NeedRecovery = (result, contract) => false;
                        break;
                    case RecoveryCalculationType.PartDestroyed:
                        NeedRecovery = RecoveryDelegates.PartDestroyed;
                        break;
                    default:
                        NeedRecovery = RecoveryDelegates.VanilaRecovery;
                        break;
                }

                switch (Settings.LostMechAction)
                {
                    case LostMechActionType.ReturnItemsAndPartsToPlayer:
                        LostUnitAction = LostUnitActions.ReturnItemsAndParts;
                        break;
                    case LostMechActionType.MoveItemsToSalvage:
                        LostUnitAction = LostUnitActions.SalvageItems;
                        break;
                    case LostMechActionType.MoveItemsAndPartsToSalvage:
                        LostUnitAction = LostUnitActions.SalvageItemsAndParts;
                        break;
                    default:
                        LostUnitAction = LostUnitActions.ReturnItems;
                        break;
                }

                switch (Settings.PartCountType)
                {
                    case PartCalculationType.VanilaAdjusted:
                        GetNumParts = PartsNumCalculations.VanilaAdjusted;
                        break;
                    case PartCalculationType.PartDestroyed:
                        GetNumParts = PartsNumCalculations.PartDestroyed;
                        break;
                    default:
                        GetNumParts = PartsNumCalculations.Vanila;
                        break;
                }

                CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

                Logger.LogDebug("done");
                Logger.LogDebug(JSONSerializationUtility.ToJSON(Settings));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        #region LOGGING
        [Conditional("CCDEBUG")]
        public static void LogDebug(string message)
        {
            Logger.LogDebug(message);
        }
        [Conditional("CCDEBUG")]
        public static void LogDebug(string message, Exception e)
        {
            Logger.LogDebug(message, e);
        }

        public static void LogError(string message)
        {
            Logger.LogError(message);
        }
        public static void LogError(string message, Exception e)
        {
            Logger.LogError(message, e);
        }
        public static void LogError(Exception e)
        {
            Logger.LogError(e);
        }

        public static void Log(string message)
        {
            Logger.Log(message);
        }



        internal static void SetupLogging(string Directory)
        {
            var logFilePath = Path.Combine(Directory, "log.txt");

            try
            {
                ShutdownLogging();
                AddLogFileForLogger(logFilePath);
            }
            catch (Exception e)
            {
                Logger.Log("CustomSalvage: can't create log file", e);
            }
        }

        internal static void ShutdownLogging()
        {
            if (logAppender == null)
            {
                return;
            }

            try
            {
                HBS.Logging.Logger.ClearAppender("CustomSalvage");
                logAppender.Flush();
                logAppender.Close();
            }
            catch
            {
            }

            logAppender = null;
        }

        private static void AddLogFileForLogger(string logFilePath)
        {
            try
            {
                logAppender = new FileLogAppender(logFilePath, FileLogAppender.WriteMode.INSTANT);
                HBS.Logging.Logger.AddAppender("CustomSalvage", logAppender);

            }
            catch (Exception e)
            {
                Logger.Log("CustomSalvage: can't create log file", e);
            }
        }

        #endregion

        public static bool IsDestroyed(UnitResult lostUnit)
        {
            return CustomComponents.Contract_GenerateSalvage.IsDestroyed(lostUnit.mech);
        }
    }
}
