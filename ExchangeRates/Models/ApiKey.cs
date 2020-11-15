using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Models
{
    /// <summary>
    /// Db model for storing registered api keys
    /// </summary>
    public class ApiKey
    {
        public ApiKey(string key)
        {
            Key = key;
        }

        [Key]
        public string Key { get; set; }
    }
}