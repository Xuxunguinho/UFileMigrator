using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Setting
    {

        [JsonPropertyName("SqlConnection")]
        public string SqlConnection { get; set; }

        [JsonPropertyName("RootDirectory")]
        public string RootDirectory { get; set; }

        [JsonPropertyName("MiniIoEndPoint")]
        public string MiniIoEndPoint { get; set; }

        [JsonPropertyName("MiniIoAcessKey")]
        public string MiniIoAcessKey { get; set; }

        [JsonPropertyName("MiniIoSecretKey")]
        public string MiniIoSecretKey { get; set; }

        [JsonPropertyName("MiniIoBucket")]
        public string MiniIoBucket { get; set; }

        [JsonPropertyName("UpdateSqlQuery")]
        public string UpdateSqlQuery { get; set; }

        [JsonPropertyName("RenameFile")]
        public bool RenameFile { get; set; }

    }
}
