using Innovia.Api.Common.Errors;

namespace Innovia.Api.Features.Sensors;
public static class SensorErrors
{
       public static readonly Error RegistryUnavailable = Error.Unavailable("Sensortjänsten är inte tillgänglig just nu.");

       public static readonly Error TenantNotConfigured = Error.Failure("Sensortjänstens tenant är felkonfigurerad.");
}