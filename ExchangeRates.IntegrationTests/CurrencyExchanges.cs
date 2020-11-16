using ExchangeRates.DTOs;
using ExchangeRates.Interfaces;
using ExchangeRates.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ExchangeRates.IntegrationTests
{
	public class CurrenciesControllerTests
	{
		private readonly HttpClient _client;

		public CurrenciesControllerTests()
		{
			var factory = new WebApplicationFactory<Startup>()
				.WithWebHostBuilder(builder =>
					{
						builder.ConfigureServices(services =>
						{
							services.RemoveAll(typeof(ApiKeyService));
							services.AddScoped<IApiKeyService, NoVeryficationApiKeyService>();
						});
					});

			_client = factory.CreateClient();
		}

		[Fact]
		public async Task Get_CurrencyExchanges_ForWeekend()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "USD", "PLN" }
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-11-14"), DateTime.Parse("2020-11-15"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var currencyExchanges = await response.Content.ReadAsAsync<List<CurrencyExchangeDTO>>();
			currencyExchanges.Count.Should().Be(2);
		}

		[Fact]
		public async Task Get_NotFound_ForFutureDate()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "USD", "PLN" }
			};
			var tomorow = DateTime.Now.AddDays(1);
			var uri = getUri("Get", currencyCodes, tomorow, tomorow);

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}

		[Fact]
		public async Task Get_BadRequest_ForInvalidCodesFormat()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "USeD", "1PLN" }
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-11-14"), DateTime.Parse("2020-11-15"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		}

		[Fact]
		public async Task Get_Rate_For16_11_2020()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "USD", "PLN" }
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-11-16"), DateTime.Parse("2020-11-16"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var currencyExchanges = await response.Content.ReadAsAsync<List<CurrencyExchangeDTO>>();
			currencyExchanges.Count.Should().Be(1);
			currencyExchanges.First().Should().BeEquivalentTo(
				new CurrencyExchangeDTO()
				{
					Date = "2020-11-16",
					CurrencyFrom = "USD",
					CurrencyTo = "PLN",
					ExchangeRate = 3.7779
				});
		}

		[Fact]
		public async Task Get_CurrencyExchanges_ForEurToUsdAndJpyToCny()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "EUR", "USD" },
				{ "JPY", "CNY" }
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-01-14"), DateTime.Parse("2020-01-19"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var currencyExchanges = await response.Content.ReadAsAsync<List<CurrencyExchangeDTO>>();
			currencyExchanges.Count.Should().Be(12);
			currencyExchanges.GroupBy(e => e.Date).Count().Should().Be(6);
		}

		[Fact]
		public async Task Get_CurrencyExchanges_ForBankingHolidayEurToUsd_01_01_2020()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "EUR", "USD" },
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-01-01"), DateTime.Parse("2020-01-01"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var currencyExchanges = await response.Content.ReadAsAsync<List<CurrencyExchangeDTO>>();
			currencyExchanges.Count.Should().Be(1);
			currencyExchanges.First().Should().BeEquivalentTo(
				new CurrencyExchangeDTO()
				{
					Date = "2020-01-01",
					CurrencyFrom = "EUR",
					CurrencyTo = "USD",
					ExchangeRate = 1.1234
				});
		}

		[Fact]
		public async Task Get_CurrencyExchanges_ForEurToUsd_OneMonth()
		{
			//Arrange
			var currencyCodes = new Dictionary<string, string>()
			{
				{ "EUR", "USD" },
			};
			var uri = getUri("Get", currencyCodes, DateTime.Parse("2020-01-01"), DateTime.Parse("2020-01-31"));

			//Act
			var response = await _client.GetAsync(uri);

			//Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var currencyExchanges = await response.Content.ReadAsAsync<List<CurrencyExchangeDTO>>();
			currencyExchanges.Count.Should().Be(31);
		}


		public class NoVeryficationApiKeyService : IApiKeyService
		{
			public Task Expire(string key) => throw new NotImplementedException();
			public Task<string> Generate() => throw new NotImplementedException();
			public Task<bool> IsValid(string key) => Task.FromResult(true);
		}

		private string getUri(string action, Dictionary<string, string> currencyCodes, DateTime startDate, DateTime endDate)
		{
			var uri = $"/{action}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&apiKey=testing";
			foreach (var codesPair in currencyCodes)
			{
				uri += $"&currencyCodes[{codesPair.Key}]={codesPair.Value}";
			}
			return uri;
		}
	}
}