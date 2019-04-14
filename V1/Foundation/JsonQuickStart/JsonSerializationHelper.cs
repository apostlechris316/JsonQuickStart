/********************************************************************************
 * JSON QuickStart Library - General Elements used to manipulate JSON data stored on the file system
 * 
 * LICENSE: Free to use provided details on fixes and/or extensions emailed to 
 *      chris.williams@readwatchcreate.com
********************************************************************************/

namespace JsonQuickStart
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

    public class JsonSerializationHelper
    {
        /// <summary>
        /// Serialize object to a JSON stream.  
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <param name="objectType"></param>
        /// <returns></returns>
        /// <remarks>NEW in v1.0.0.2<br/>
        /// Adapted from https://msdn.microsoft.com/en-us/library/bb412179(v=vs.110).aspx </remarks>
        public string SerializeJsonObject(object objectToSerialize, System.Type objectType)
        {
            if (objectToSerialize == null) throw new ArgumentNullException("ERROR: objectToSerialize is required");

            //Create a stream to serialize the object to.  
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.  
            DataContractJsonSerializer ser = new DataContractJsonSerializer(objectType);
            ser.WriteObject(ms, objectToSerialize);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }

        /// <summary>
        /// Deserialize a JSON Content to anobject.  
        /// </summary>
        /// <param name="json"></param>
        /// <param name="objectType"></param>
        /// <returns></returns>
        /// <remarks>NEW in v1.0.0.2<br/>
        /// Adapted from https://msdn.microsoft.com/en-us/library/bb412179(v=vs.110).aspx </remarks>
        public object DeserializeJsonObject(string json, System.Type objectType)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentNullException("ERROR: json is required");

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(objectType);
            object deserializedUser = ser.ReadObject(ms);
            ms.Close();
            return deserializedUser;
        }
    }
}
