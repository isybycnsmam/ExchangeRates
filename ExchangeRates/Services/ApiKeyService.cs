using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ExchangeRates.Interfaces;
using ExchangeRates.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Services
{
    /// <summary>
    /// Service that implements IApiKeyService by generating and storing randomly generated api keys
    /// </summary>
    public class ApiKeyService : IApiKeyService
    {
        private readonly ExchangesContext _exchangesContext;

        public ApiKeyService(
            ExchangesContext exchangesContext)
        {
            _exchangesContext = exchangesContext;
        }

        /// <summary>
        /// Method that removes api key from db
        /// </summary>
        /// <inheritdoc />
        public async Task Expire(string key)
        {
            var apiKey = await _exchangesContext.ApiKeys.FirstOrDefaultAsync(e => e.Key == key);
            if (apiKey != null)
            {
                _exchangesContext.ApiKeys.Remove(apiKey);
                await _exchangesContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Method that generates new api key and saves it to db
        /// </summary>
        /// <inheritdoc />
        public async Task<string> Generate()
        {
            var apiKey = generateApiKeyString();
            await _exchangesContext.ApiKeys.AddAsync(new ApiKey(apiKey));
            await _exchangesContext.SaveChangesAsync();
            return apiKey;
        }

        /// <summary>
        /// Method that checks if key exists in db
        /// </summary>
        /// <inheritdoc />
        public async Task<bool> IsValid(string key)
        {
            var apiKey = await _exchangesContext.ApiKeys.FirstOrDefaultAsync(e => e.Key == key);
            if (apiKey is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method that randomly generates string
        /// </summary>
        /// <returns>random string</returns>
        private string generateApiKeyString()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }
            return Convert.ToBase64String(key)
                .Replace('+', '_')
                .Replace('=', '-');
        }
    }
}