using System;
using System.Data;

namespace JCBSystem.Infrastructure.Connection.Interface
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
