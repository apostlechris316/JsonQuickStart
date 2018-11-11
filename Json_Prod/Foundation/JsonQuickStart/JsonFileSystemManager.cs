/********************************************************************************
 * CSHARP JSON File System Library - General Elements used to manipulate JSON data stored on the file system
 * 
 * LICENSE: Free to use provided details on fixes and/or extensions emailed to 
 *      chris.williams@readwatchcreate.com
********************************************************************************/

namespace JsonQuickStart
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Data Access Layer for reading and writing Json Files to the File System
    /// </summary>
    public class JsonFileSystemManager
    {
        /// <summary>
        /// This is the full path to the folder containing all Json Item Files
        /// </summary>
        public string DefaultJsonRootFolder { get; set; }

        /// <summary>
        /// If cache enabled we need to assign type of item we are caching
        /// </summary>
        public Type ItemType { get; set; }

        #region Json Item File Cache Related

        /// <summary>
        /// If true, reads will come from the cache
        /// </summary>
        public bool EnableReadCache
        {
            get { return _readCachEnabled; }
            set
            {
                // Disable cache so clear it
                if (value == false) _cachedJsonItems = null;

                _readCachEnabled = (value == true) ? LoadJsonItemFilesToCache(ItemType.ToString()) : false;
            }
        }
        private bool _readCachEnabled = false;

        /// <summary>
        /// Cache containing Json Items
        /// </summary>
        protected List<JsonItemFile> _cachedJsonItems = null;

        /// <summary>
        /// Ceched Json Items Dictionary or null if not cached
        /// </summary>
        public List<JsonItemFile> CachedJsonItems {  get { return _cachedJsonItems; } }

        /// <summary>
        /// Last time the Json Item cache was loaded
        /// </summary>
        protected DateTime _cacheLastLoadedDateTime { get; set; }

        /// <summary>
        /// Flushes the cache, forceing a reload
        /// </summary>
        /// <returns></returns>
        public bool FlushCache(Type itemType)
        {
            if (itemType == null) throw new ArgumentNullException("ERROR: itemType is required");

            return LoadJsonItemFilesToCache(itemType.ToString());
        }

        /// <summary>
        /// Gets a JsonItemFile object for the item passed in
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="itemName"></param>
        /// <param name="eventLog"></param>
        /// <returns></returns>
        public JsonItemFile GetJsonItemFileForItem(object item, string itemId, Type itemType)
        {
            var jsonHelper = new JsonSerializationHelper();

            string jsonFileName = itemId.Replace("{", "").Replace("-", "").Replace("}", "");
            return new JsonItemFile()
            {
                ItemType = itemType.Name,
                JsonFileName = jsonFileName,
                UniqueFileName = jsonFileName,
                JsonContent = jsonHelper.SerializeJsonObject(item, itemType),
                RelativePathBeneathTable = jsonFileName               
            };
        }

        /// <summary>
        /// Loads Json Items into Cache using default Json Root Folder and Item Type Name
        /// </summary>
        /// <returns></returns>
        public bool LoadJsonItemFilesToCache(string itemTypeFolderName)
        {
            if (string.IsNullOrEmpty(DefaultJsonRootFolder)) throw new NullReferenceException("LoadJsonItemFilesToCache - DefaultJsonRootFolder is required to enable caching");

            _cachedJsonItems = LoadAllJsonFileItems(DefaultJsonRootFolder, itemTypeFolderName);
            _cacheLastLoadedDateTime = DateTime.Now;
            return true;
        }

        #endregion

        #region Get/Insert Item Related

        /// <summary>
        /// Inserts an item to the Json File System
        /// </summary>
        /// <param name="jsonRootFolder">Full path to the folder containing all Json Item Files</param>
        /// <param name="item">Item to write to file system</param>
        /// <param name="itemType">Type of item to write. Pass using typeof()</param>
        /// <param name="itemId">Unique id for item</param>
        /// <param name="itemName">Name of item</param>
        /// <returns></returns>
        public bool InsertItem(string jsonRootFolder, object item, string itemTypeFolderName, Type itemType, string itemId, string itemName)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (item == null) throw new ArgumentNullException("ERROR: item is required");
            if (string.IsNullOrEmpty(itemTypeFolderName)) throw new ArgumentNullException("ERROR: itemTypeFolderName is required");
            if (itemType == null) throw new ArgumentNullException("ERROR: itemType is required");
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentNullException("ERROR: itemId is required");
            if (string.IsNullOrEmpty(itemName)) throw new ArgumentNullException("ERROR: itemName is required");

            try
            {
                bool priorEnableReadCache = EnableReadCache;
                EnableReadCache = false;

                // Load the assets and then add the asset and then save it.
                var jsonFileSystemManager = new JsonFileSystemManager();
                var items = jsonFileSystemManager.LoadAllJsonFileItems(jsonRootFolder, itemTypeFolderName);

                // The way this works is if you add the item twice on save it would end up saving the one latest in the list
                var jsonFileItem = GetJsonItemFileForItem(item, itemId, itemType);
                items.Add(jsonFileItem);

                bool returnValue = jsonFileSystemManager.SaveAllJsonFileItems(jsonRootFolder, itemTypeFolderName, itemType.Name, items);
                EnableReadCache = priorEnableReadCache;
                return returnValue;
            }
            catch (Exception exception)
            {
                throw new Exception("ERROR Inserting " + itemType.Name + ": (" + itemId + "," + itemName + ")", exception);
            }
        }

        /// <summary>
        /// Writes some items to the file system. Skipping existing items
        /// </summary>
        /// <param name="jsonRootFolder"></param>
        /// <param name="itemsToAdd"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public bool InsertItems(string jsonRootFolder, List<JsonItemFile> itemsToAdd, string itemTypeFolderName, Type itemType, bool overwrite)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (itemsToAdd == null) throw new ArgumentNullException("ERROR: itemToAdds is required");
            if (string.IsNullOrEmpty(itemTypeFolderName)) throw new ArgumentNullException("ERROR: itemTypeFolderName is required");
            if (itemType == null) throw new ArgumentNullException("ERROR: itemType is required");

            // if we are not overwriting items
            if (overwrite == false) return InsertItems(jsonRootFolder, itemsToAdd, itemTypeFolderName, itemType);

            // if we are overwriting items
            try
            {
                bool priorEnableReadCache = EnableReadCache;
                EnableReadCache = false;

                // Load the assets
                var jsonFileSystemManager = new JsonFileSystemManager();
                List<JsonItemFile> items = jsonFileSystemManager.LoadAllJsonFileItems(jsonRootFolder, itemTypeFolderName);
                List<JsonItemFile> itemsToSave = new List<JsonItemFile>();

                // Ensure all the ones we are adding are in the save list.
                // Only include existing items that are not replaced by our inserts
                foreach (var item in itemsToAdd)
                {
                    // Ensure item filename ends in .json
                    if (item.JsonFileName.EndsWith(".json") == false) item.JsonFileName = item.JsonFileName + ".json";

                    itemsToSave.Add(item);
                }

                // Only include existing items that are not replaced by our inserts
                foreach (var item in items)
                {
                    // Ensure item filename ends in .json
                    if (item.JsonFileName.EndsWith(".json") == false) item.JsonFileName = item.JsonFileName + ".json";

                    if (itemsToSave.FirstOrDefault(x => x.JsonFileName == item.JsonFileName) == null)
                        itemsToSave.Add(item);
                }

                // save everything back to file system.
                bool returnValue = jsonFileSystemManager.SaveAllJsonFileItems(jsonRootFolder, itemTypeFolderName, itemType.Name, itemsToSave);
                EnableReadCache = priorEnableReadCache;
                return returnValue;
            }
            catch (Exception exception)
            {
                throw new Exception("ERROR Inserting " + itemType.Name + "s", exception);
            }
        }
        /// <summary>
        /// Writes some items to the file system. Skipping existing items
        /// </summary>
        /// <param name="jsonRootFolder"></param>
        /// <param name="itemsToAdd"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public bool InsertItems(string jsonRootFolder, List<JsonItemFile> itemsToAdd, string itemTypeFolderName, Type itemType)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (itemsToAdd == null) throw new ArgumentNullException("ERROR: itemToAdd is required");
            if (string.IsNullOrEmpty(itemTypeFolderName)) throw new ArgumentNullException("ERROR: itemTypeFolderName is required");
            if (itemType == null) throw new ArgumentNullException("ERROR: itemType is required");

            try
            {
                bool priorEnableReadCache = EnableReadCache;
                EnableReadCache = false;

                // Load the assets and then add the asset and then save it.
                var jsonFileSystemManager = new JsonFileSystemManager();
                List<JsonItemFile> items = jsonFileSystemManager.LoadAllJsonFileItems(jsonRootFolder, itemTypeFolderName);
                foreach(var item in itemsToAdd)
                {
                    if(items.FirstOrDefault(x => x.JsonFileName == item.JsonFileName) == null)
                        items.Add(item);
                }

                bool returnValue = jsonFileSystemManager.SaveAllJsonFileItems(jsonRootFolder, itemTypeFolderName, itemType.Name, items);
                EnableReadCache = priorEnableReadCache;
                return returnValue;
            }
            catch (Exception exception)
            {
                throw new Exception("ERROR Inserting " + itemType.Name + "s", exception);
            }
        }

        #endregion

        #region Serialization Related

        /// <summary>
        /// Loads all the Json Items stored on file system to object list
        /// </summary>
        /// <param name="jsonRootFolder"></param>
        /// <param name="itemTypeFolderName"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        /// <remarks>Loading with this method makes it not possible to re-save to file system. Use LoadAllJsonFileItems instead of you wish to resave to file system.</remarks>
        public List<object> LoadAllJsonFileItemsToObjects(string jsonRootFolder, string itemTypeFolderName, Type itemType)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (string.IsNullOrEmpty(itemTypeFolderName)) throw new ArgumentNullException("ERROR: itemTypeFolderName is required");
            if (itemType == null) throw new ArgumentNullException("ERROR: itemType is required");

            var jsonHelper = new JsonSerializationHelper();

            List<object> objects = new List<object>();

            // Make sure the folder exists. If it does not then create it.
            string folderToLoadFrom = (!jsonRootFolder.EndsWith("\\") ? "\\" : "") + itemTypeFolderName;
            if (Directory.Exists(folderToLoadFrom) == false) Directory.CreateDirectory(folderToLoadFrom);

            // Use directory to get all files beneath a folder and subfolders
            var di = new DirectoryInfo(folderToLoadFrom);
            var filePaths = di.GetFiles("*.json", SearchOption.AllDirectories);

            foreach (var filePath in filePaths)
            {
                using (var streamReader = File.OpenText(filePath.FullName))
                {
                    objects.Add(jsonHelper.DeserializeJsonObject(streamReader.ReadToEnd(), itemType));
                }
            }

            return objects;
        }

        /// <summary>
        /// Loads all the Json Items into a dictionary containing item name and JSON Content 
        /// </summary>
        /// <param name="jsonRootFolder"></param>
        /// <param name="itemFolderName"></param>
        /// <returns></returns>
        public List<JsonItemFile> LoadAllJsonFileItems(string jsonRootFolder, string itemTypeFolderName)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (string.IsNullOrEmpty(itemTypeFolderName)) throw new ArgumentNullException("ERROR: itemTypeFolderName is required");

            List<JsonItemFile> jsonItems = new List<JsonItemFile>();

            // Make sure the folder exists. If it does not then create it.
            string folderToLoadFrom = (!jsonRootFolder.EndsWith("\\") ? "\\" :"") + itemTypeFolderName;
            if (Directory.Exists(folderToLoadFrom) == false) Directory.CreateDirectory(folderToLoadFrom);

            // Use directory to get all files beneath a folder and subfolders
            var di = new DirectoryInfo(folderToLoadFrom);
            var filePaths = di.GetFiles("*.json", SearchOption.AllDirectories);

            foreach (var filePath in filePaths)
            {
                var fileParts = filePath.FullName.Split('\\');
                var fileName = fileParts.Length > 0 ? fileParts[fileParts.Length - 1] : "";

                var jsonContent = string.Empty;

                using (var streamReader = File.OpenText(filePath.FullName))
                {
                    jsonContent = streamReader.ReadToEnd();
                }

                JsonItemFile jsonItemFile = new JsonItemFile()
                {
                    UniqueFileName = fileName + Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", ""),
                    JsonFileName = fileName,
                    RelativePathBeneathTable = fileName,
                    JsonContent = jsonContent  
                };

                jsonItems.Add(jsonItemFile);
            }

            return jsonItems;
        }

        /// <summary>
        /// Savaes all the Json Items in a dictionary to the File System 
        /// </summary>
        /// <param name="jsonRootFolder"></param>
        /// <param name="itemFolderName"></param>
        /// <param name="itemTypeName"></param>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        /// <remarks>Note: The JSON files end in the file extension .json</remarks>
        public bool SaveAllJsonFileItems(string jsonRootFolder, string itemFolderName, string itemTypeName, List<JsonItemFile> jsonItems)
        {
            if (string.IsNullOrEmpty(jsonRootFolder)) throw new ArgumentNullException("ERROR: jsonRootFolder is required");
            if (string.IsNullOrEmpty(itemFolderName)) throw new ArgumentNullException("ERROR: itemFolderName is required");
            if (string.IsNullOrEmpty(itemTypeName)) throw new ArgumentNullException("ERROR: itemTypeName is required");
            if (jsonItems == null) throw new ArgumentNullException("ERROR: jsonItems is required");

            // Cannot save items if they are not passed in
            if (jsonItems == null) throw new ArgumentNullException("SaveAllJsonFileItems - " + itemTypeName + " - jsonItems is null");

            bool returnValue = true;
            foreach (var item in jsonItems)
            {
                try
                {
                    // Make sure the folder exists. If it does not then create it.
                    string folderToSaveTo = (!jsonRootFolder.EndsWith("\\") ? "\\" : "") + itemFolderName;
                    if (Directory.Exists(folderToSaveTo) == false) Directory.CreateDirectory(folderToSaveTo);

                    // Note: JSON Files need to end in .json
                    string fullPath = folderToSaveTo + (item.RelativePathBeneathTable.StartsWith("\\") ? item.RelativePathBeneathTable.Substring(1) : item.RelativePathBeneathTable) + (item.RelativePathBeneathTable.EndsWith(".json") ? "" : ".json");

                    using (
                        var streamWriter = File.CreateText(fullPath))
                    {
                        streamWriter.Write(item.JsonContent);
                        streamWriter.Flush();
                    }
                }
                catch(Exception exception)
                {
                    throw new Exception("Error SaveAllJsonFileItems: (" + item.JsonFileName + ") ", exception);
                }
            }

            return returnValue;
        }

        #endregion
    }
}
