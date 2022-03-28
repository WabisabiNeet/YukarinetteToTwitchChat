using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YukarinetteToTwitchChat
{
    public class ValidateResponse
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }
}
