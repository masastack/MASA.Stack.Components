namespace MasaWebApp.Data;

public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }

    public Level Level { get; set; }
}

public enum Level
{
    Normal,
    Low,
    High
}