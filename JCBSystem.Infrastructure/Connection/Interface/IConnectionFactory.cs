using System.Data;
using System.Threading.Tasks;

namespace JCBSystem.Infrastructure.Connection.Interface
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Gets the appropriate connection factory based on the provider type.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <returns>An instance of IDbConnectionFactory for the specified provider.</returns>
        Task<IDbConnectionFactory> GetFactory();

        /// <summary>
        /// Opens a database connection asynchronously if supported, or synchronously otherwise.
        /// </summary>
        /// <param name="connection">The connection to open.</param>
        Task OpenConnectionAsync(IDbConnection connection);

        /// <summary>
        /// Creates a data adapter based on the provided command type.
        /// </summary>
        /// <param name="command">The command to create an adapter for.</param>
        /// <returns>An appropriate data adapter.</returns>
        Task<IDataAdapter> CreateDataAdapter(IDbCommand command);
    }
}
