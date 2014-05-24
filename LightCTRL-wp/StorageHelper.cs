using LIFX_Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;

public class StorageHelper
{
    private static Dictionary<string, LifxPanController> panControllers = new Dictionary<string, LifxPanController>();

    private StorageHelper() { }

    public static void LoadFromStorage()
    {
        panControllers.Clear();
        XmlSerializer xms = new XmlSerializer(typeof(LifxPanController));

        foreach (string s in ApplicationData.Current.RoamingSettings.Values.Values)
        {
            StringReader sr = new StringReader(s);
            LifxPanController panController = xms.Deserialize(sr) as LifxPanController;

            foreach (LifxBulb bulb in panController.Bulbs)
            {
                if (bulb.MACAddress == panController.MACAddress)
                    bulb.PanController = panController;
            }

            panControllers.Add(panController.MACAddress, panController);
        }
    }

    public static void SaveToStorage()
    {
        ClearStorage(true, true, false);

        StringWriter sw = new StringWriter();
        XmlSerializer xms = new XmlSerializer(typeof(LifxPanController));

        foreach (LifxPanController panController in panControllers.Values)
        {
            if (!ApplicationData.Current.RoamingSettings.Values.ContainsKey(panController.MACAddress))
            {
                xms.Serialize(sw, panController);
                string blah = sw.ToString();

                ApplicationData.Current.RoamingSettings.Values.Add(panController.MACAddress, sw.ToString());
            }
        }

        ApplicationData.Current.LocalSettings.Values.Add(new KeyValuePair<string, object>("FirstRun", false));
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

    public static LifxPanController GetPanController(string panControllerUID)
    {
        return panControllers[panControllerUID] as LifxPanController;
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

        return null;
    }

    public static LifxBulb GetBulb(string bulbLabel)
    {
        foreach (LifxPanController panController in panControllers.Values)
        {
            foreach (LifxBulb bulb in panController.Bulbs)
            {
                if (bulb.Label == bulbLabel)
                    return bulb;
            }
        }

        return null;
    }

    public static List<List<KeyValuePair<Byte[], string>>> GetBulbMap()
    {
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