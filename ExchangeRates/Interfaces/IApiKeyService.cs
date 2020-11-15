using System.Threading.Tasks;

namespace ExchangeRates.Interfaces
{
    /// <summary>
    /// Interface for generating api keys
    /// </summary>
    public interface IApiKeyService
    {
        /// <summary>
        /// Method that generates new api key
        /// </summary>
        /// <returns>api key string</returns>
        Task<string> Generate();

        /// <summary>
        /// Method that expires given api key
        /// </summary>
        /// <param name="key">api key</param>
        Task Expire(string key);

        /// <summary>
        /// Method that checks is key correct
        /// </summary>
        /// <param name="key">api key</param>
        /// <returns>is correct</returns>
        Task<bool> IsValid(string key);
    }
}