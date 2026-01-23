using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


namespace DataView2.Options
{
    public static class OptionsExtensions
    {
        public static IOptions<TOptions> GetOptions<TOptions>(this IConfiguration configuration) where TOptions : class, new()
        {
            var configSection = configuration.GetSection(typeof(TOptions).Name);
            var options = configSection.Get<TOptions>();

            return options is null ? new OptionsWrapper<TOptions>(new()) : new OptionsWrapper<TOptions>(options);
        }
    }
}
