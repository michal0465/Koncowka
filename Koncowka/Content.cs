using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Koncowka
{
    public class Content
    {
        private string[] arrayOfSrcs;
        public Content(string json)
        {
            JObject jObject = JObject.Parse(json);
            object[] temp = (object [])(jObject["images"].ToArray());
            arrayOfSrcs = Array.ConvertAll<object, string>(temp, Convert.ToString);
        }
        public string[] ArrayOfSrcs
        {
            get
            {
                return arrayOfSrcs;
            }
        }
    }
}
