using LIFX_Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Windows.Storage;
using Windows.Storage.Streams;

[Flags]
public enum Settings
{
    FirstRun            = 0,
    CloseOnVoiceCommand = 1 << 0
}

public static class Helper
{
    public static async void ShowStatusBar()
    {
        await StatusBar.GetForCurrentView().ShowAsync();
    }

    public static async void ShowProgressIndicator(string progressString)
    {
        StatusBar.GetForCurrentView().ProgressIndicator.Text = progressString;
        await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
        await StatusBar.GetForCurrentView().ShowAsync();
    }

    public static async void HideProgressIndicator(bool hideStatusBar = false)
    {
        await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();

        if (hideStatusBar)
            await StatusBar.GetForCurrentView().HideAsync();
    }
}

public static class StorageHelper
{
    private static Dictionary<string, LifxPanController> panControllers = new Dictionary<string, LifxPanController>();

    #region Properties

    public static ResourceLoader Strings { get { return ResourceLoader.GetForCurrentView("Strings"); } }

    public static ResourceLoader ErrorMessages { get { return ResourceLoader.GetForCurrentView("Errors"); } }

    public static Settings LocalSettings
    {
        get
        {
            object storedsettings;
            ApplicationData.Current.LocalSettings.Values.TryGetValue("AppSettings", out storedsettings);

            Settings outvalue = Settings.FirstRun;

            if (storedsettings != null)
                Enum.TryParse(storedsettings.ToString(), out outvalue);

            return outvalue;
        }
        set
        {
            ApplicationData.Current.LocalSettings.Values["AppSettings"] = Convert.ToUInt64(value);
        }
    }

    public static bool HasStoredLights
    {
        get
        {
            CheckLoaded();
            return panControllers.Count > 0 ? true : false;
        }
    }

    public static List<LifxPanController> PanControllers
    {
        get
        {
            CheckLoaded();
            return new List<LifxPanController>(panControllers.Values);
        }
    }

    public static List<LifxBulb> Bulbs
    {
        get
        {
            CheckLoaded();

            List<LifxBulb> list = new List<LifxBulb>();

            foreach (LifxPanController panCont in panControllers.Values)
            {
                list.AddRange(panCont.Bulbs);
            }

            return list;
        }
    }

    #endregion

    private static void CheckLoaded()
    {
        if (panControllers.Count < 1)
            LoadFromStorage();
    }

    private static void SetupBulbPanLink()
    {
        foreach (LifxPanController panController in panControllers.Values)
        {
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                bulb.PanController = GetPanController(bulb.PanControllerMACAddress);
            }
        }
    }

    public static void LoadFromStorage()
    {
        panControllers.Clear();
        XmlSerializer xms = new XmlSerializer(typeof(List<LifxPanController>));

        string s = ApplicationData.Current.RoamingSettings.Values["PanControllers"] as string;

        if (s != null)
        {
            StringReader sr = new StringReader(s);
            panControllers = (xms.Deserialize(sr) as List<LifxPanController>).ToDictionary(x => x.MACAddress);

            SetupBulbPanLink();
        }
    }

    public static void SaveToStorage()
    {
        StringWriter sw = new StringWriter();
        XmlSerializer xms = new XmlSerializer(typeof(List<LifxPanController>));

        xms.Serialize(sw, panControllers.Values.ToList());
        string blah = sw.ToString();

        if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("PanControllers"))
            ApplicationData.Current.RoamingSettings.Values["PanControllers"] = sw.ToString();
        else
            ApplicationData.Current.RoamingSettings.Values.Add("PanControllers", sw.ToString());
    }

    public static void ClearStorage(bool localsettings = false, bool roamingsettings = true, bool pancontrollers = false)
    {
        if (localsettings)
            ApplicationData.Current.LocalSettings.Values.Clear();

        if (roamingsettings)
            ApplicationData.Current.RoamingSettings.Values.Clear();

        if (pancontrollers)
            panControllers.Clear();
    }

    public static LifxPanController GetPanController(string panControllerMACAddress)
    {
        return panControllers[panControllerMACAddress] as LifxPanController;
    }

    public static LifxBulb GetBulb(Byte[] bulbUID)
    {
        foreach (LifxPanController panController in panControllers.Values)
        {
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                if (LifxHelper.ByteArrayToString(bulb.UID) == LifxHelper.ByteArrayToString(bulbUID))
                    return bulb;
            }
        }

        throw new BulbNotFoundException(ErrorMessages.GetString("BulbNotFound"));
    }

    public static LifxBulb GetBulb(string bulbLabel, bool ignoreCase)
    {
        CheckLoaded();

        foreach (LifxPanController panController in panControllers.Values)
        {
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                if (ignoreCase)
                {
                    if (bulb.Label.ToUpper() == bulbLabel.ToUpper())
                        return bulb;
                }
                else
                {
                    if (bulb.Label == bulbLabel)
                        return bulb;
                }
            }
        }

        throw new BulbNotFoundException(ErrorMessages.GetString("BulbNotFound"));
    }

    public static LifxBulb GetBulb(string bulbLabel)
    {
        CheckLoaded();

        foreach (LifxPanController panController in panControllers.Values)
        {
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                if (bulb.Label == bulbLabel)
                    return bulb;
            }
        }

        throw new BulbNotFoundException(ErrorMessages.GetString("BulbNotFound"));
    }

    public static List<List<KeyValuePair<Byte[], string>>> GetBulbMap()
    {
        CheckLoaded();

        List<List<KeyValuePair<Byte[], string>>> bulbmap = new List<List<KeyValuePair<byte[], string>>>();

        int i = 0;
        foreach (LifxPanController panController in panControllers.Values)
        {
            bulbmap.Add(new List<KeyValuePair<byte[], string>>());
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                bulbmap[i].Add(new KeyValuePair<byte[], string>(bulb.UID, bulb.Label != null ? bulb.Label : "Unknown"));
            }

            i++;
        }

        return bulbmap;
    }

    public static void StorePanController(LifxPanController newPanController)
    {
        if (!panControllers.ContainsKey(newPanController.MACAddress))
        {
            panControllers.Add(newPanController.MACAddress, newPanController);
            SaveToStorage();
        }
    }

    public static void StoreBulb(LifxBulb newBulb)
    {
        bool controllerFound = false;
        foreach (LifxPanController panController in panControllers.Values)
        {
            if (newBulb.PanController.UID == panController.UID)
            {
                panController.Bulbs.Add(newBulb);
                controllerFound = true;
            }
        }

        if (!controllerFound)
        {
            panControllers.Add(newBulb.PanController.MACAddress, newBulb.PanController);
        }

        SaveToStorage();
    }
}