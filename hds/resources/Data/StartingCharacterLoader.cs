using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace hds
{
    /// <summary>
    /// Loads starting character data from the MxO client's StartingCharacterData.xml.
    /// This XML file defines:
    ///   - Starting inventory items (hats, glasses, shirts, coats, pants, shoes, weapons, tools)
    ///   - Starting abilities with IDs, levels, and slot assignments
    ///   - Separate male/female equipment options
    ///
    /// File location: resource/misc/StartingCharacterData.xml in the game install directory.
    ///
    /// Inventory slot mappings (from XML comments):
    ///   Backpack: 0-47, Hat: 49, Glasses: 50, Shirt: 51, Gloves: 52,
    ///   Coat: 53, Pants: 54, Tights: 55, Shoes: 56, Weapon: 57, HackTool: 58
    /// </summary>
    public class StartingCharacterLoader
    {
        /// <summary>
        /// Starting inventory item parsed from XML.
        /// </summary>
        public class StartingItem
        {
            public string name;
            public int goid;
            public int stability;
            public int purity;
            public int inventorySlot;
        }

        /// <summary>
        /// Starting ability code parsed from XML.
        /// </summary>
        public class StartingAbility
        {
            public string name;
            public int abilityID;
            public int abilityLevel;
            public int storageSlot;
            public bool isEquipped;
        }

        private List<StartingItem> maleItems;
        private List<StartingItem> femaleItems;
        private List<StartingItem> tools;
        private List<StartingAbility> abilities;
        private Dictionary<string, List<StartingItem>> maleOptionsBySlot;
        private Dictionary<string, List<StartingItem>> femaleOptionsBySlot;

        public StartingCharacterLoader()
        {
            maleItems = new List<StartingItem>();
            femaleItems = new List<StartingItem>();
            tools = new List<StartingItem>();
            abilities = new List<StartingAbility>();
            maleOptionsBySlot = new Dictionary<string, List<StartingItem>>();
            femaleOptionsBySlot = new Dictionary<string, List<StartingItem>>();
        }

        /// <summary>
        /// Load starting character data from the XML file.
        /// </summary>
        public bool loadFromFile(string xmlPath)
        {
            if (!File.Exists(xmlPath))
            {
                Output.writeToLogForConsole("[StartingCharacterLoader] File not found: " + xmlPath);
                return false;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);

                // Parse male equipment options
                parseItemOptions(doc, "MALE_HAT_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_GLASSES_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_SHIRT_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_GLOVES_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_COAT_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_PANTS_OPTIONS", maleItems, maleOptionsBySlot);
                parseItemOptions(doc, "MALE_SHOES_OPTIONS", maleItems, maleOptionsBySlot);

                // Parse female equipment options
                parseItemOptions(doc, "FEMALE_HAT_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_GLASSES_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_SHIRT_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_GLOVES_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_COAT_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_PANTS_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_TIGHTS_OPTIONS", femaleItems, femaleOptionsBySlot);
                parseItemOptions(doc, "FEMALE_SHOES_OPTIONS", femaleItems, femaleOptionsBySlot);

                // Parse tools
                XmlNodeList toolNodes = doc.SelectNodes("//TOOLS/INVENTORY_ITEM");
                if (toolNodes != null)
                {
                    foreach (XmlNode node in toolNodes)
                    {
                        StartingItem item = parseInventoryItem(node);
                        if (item != null) tools.Add(item);
                    }
                }

                // Parse starting abilities
                XmlNodeList abilityNodes = doc.SelectNodes("//ABILITY_CODES/ABILITY_CODE");
                if (abilityNodes != null)
                {
                    foreach (XmlNode node in abilityNodes)
                    {
                        StartingAbility ab = new StartingAbility();
                        ab.name = node.Attributes["name"]?.Value ?? "";
                        ab.abilityID = parseIntChild(node, "ABILITY_ID", 0);
                        ab.abilityLevel = parseIntChild(node, "ABILITY_LEVEL", 0);
                        ab.storageSlot = parseIntChild(node, "STORAGE_SLOT", 0);
                        string equipped = getChildText(node, "ABILITY_CODE_EQUIPPED");
                        ab.isEquipped = equipped == "yes";
                        abilities.Add(ab);
                    }
                }

                Output.writeToLogForConsole(
                    "[StartingCharacterLoader] Loaded: " +
                    maleItems.Count + " male items, " +
                    femaleItems.Count + " female items, " +
                    tools.Count + " tools, " +
                    abilities.Count + " abilities");

                return true;
            }
            catch (Exception ex)
            {
                Output.writeToLogForConsole("[StartingCharacterLoader] Error: " + ex.Message);
                return false;
            }
        }

        private void parseItemOptions(XmlDocument doc, string optionName,
            List<StartingItem> itemList, Dictionary<string, List<StartingItem>> optionMap)
        {
            XmlNodeList nodes = doc.SelectNodes("//" + optionName + "/INVENTORY_ITEM");
            if (nodes == null || nodes.Count == 0) return;

            List<StartingItem> options = new List<StartingItem>();
            foreach (XmlNode node in nodes)
            {
                StartingItem item = parseInventoryItem(node);
                if (item != null)
                {
                    itemList.Add(item);
                    options.Add(item);
                }
            }
            optionMap[optionName] = options;
        }

        private StartingItem parseInventoryItem(XmlNode node)
        {
            StartingItem item = new StartingItem();
            item.name = node.Attributes["name"]?.Value ?? "";
            item.goid = parseIntChild(node, "GOID", 0);
            item.stability = parseIntChild(node, "STABILITY", 0);
            item.purity = parseIntChild(node, "PURITY", 3);
            item.inventorySlot = parseIntChild(node, "INVENTORY_SLOT", 0);
            return item;
        }

        private int parseIntChild(XmlNode parent, string childName, int defaultVal)
        {
            XmlNode child = parent.SelectSingleNode(childName);
            if (child != null && int.TryParse(child.InnerText.Trim(), out int val))
                return val;
            return defaultVal;
        }

        private string getChildText(XmlNode parent, string childName)
        {
            XmlNode child = parent.SelectSingleNode(childName);
            return child?.InnerText?.Trim() ?? "";
        }

        // === Public Accessors ===

        public List<StartingItem> getMaleItems() { return maleItems; }
        public List<StartingItem> getFemaleItems() { return femaleItems; }
        public List<StartingItem> getTools() { return tools; }
        public List<StartingAbility> getAbilities() { return abilities; }

        /// <summary>
        /// Get equipment options for a specific slot (e.g., "MALE_SHIRT_OPTIONS").
        /// </summary>
        public List<StartingItem> getMaleOptions(string slotName)
        {
            if (maleOptionsBySlot.ContainsKey(slotName))
                return maleOptionsBySlot[slotName];
            return new List<StartingItem>();
        }

        public List<StartingItem> getFemaleOptions(string slotName)
        {
            if (femaleOptionsBySlot.ContainsKey(slotName))
                return femaleOptionsBySlot[slotName];
            return new List<StartingItem>();
        }

        /// <summary>
        /// Get a random starting equipment loadout for character creation.
        /// </summary>
        public List<StartingItem> getRandomLoadout(bool isMale, Random rng)
        {
            List<StartingItem> loadout = new List<StartingItem>();
            Dictionary<string, List<StartingItem>> options =
                isMale ? maleOptionsBySlot : femaleOptionsBySlot;

            foreach (var kvp in options)
            {
                if (kvp.Value.Count > 0)
                {
                    loadout.Add(kvp.Value[rng.Next(kvp.Value.Count)]);
                }
            }

            // Always include tools
            loadout.AddRange(tools);

            return loadout;
        }
    }
}
