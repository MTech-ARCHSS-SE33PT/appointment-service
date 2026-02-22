using Microsoft.Data.SqlClient;

namespace AppointmentService.Api.Services;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
