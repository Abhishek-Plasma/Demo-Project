using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace SwaggerJsonGen.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Tags("WeatherForecast")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a 5-day weather forecast.
        /// </summary>
        /// <remarks>
        /// Returns randomly generated weather data including date, temperature, and summary.
        /// </remarks>
        [HttpGet] 
        [ProducesResponseType(typeof(IEnumerable<WeatherForecast1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IEnumerable<WeatherForecast1> Get(string nothing)
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast1
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
