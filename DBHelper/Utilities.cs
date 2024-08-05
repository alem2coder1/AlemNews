using COMMON;

namespace DBHelper;

public class Utilities
{
    public static System.Data.IDbConnection GetOpenConnection()
    {
        var connectionString = QarSingleton.GetInstance().GetConnectionString();
        System.Data.IDbConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static System.Data.IDbConnection GetOldDbConnection()
    {
        var connectionString = "Server=45.82.31.169;port=3306;database=almatyakshamy_old_db;uid=test_dba;pwd=u2qJMVc4>Q{AY#m$^WH[pTf!Zksa5;charset=utf8mb4;";
        System.Data.IDbConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static System.Data.IDbConnection GetOldServerDbConnection()
    {
        var connectionString = "Server=45.82.31.169;port=3306;database=almatyakshamy_db;uid=test_dba;pwd=u2qJMVc4>Q{AY#m$^WH[pTf!Zksa5;charset=utf8mb4;";
        System.Data.IDbConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}