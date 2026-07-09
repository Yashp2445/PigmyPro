using System;
using System.Data.SqlClient;

class Program {
    static void Main() {
        string connStr = "Server=localhost;Database=PigmyPro;Integrated Security=True;TrustServerCertificate=True;";
        using (var conn = new SqlConnection(connStr)) {
            conn.Open();
            var cmds = new[] { "usp_InsertAccount", "usp_UpdateAccount", "usp_DeleteAccount" };
            foreach (var sp in cmds) {
                using (var cmd = new SqlCommand($"sp_helptext '{sp}'", conn)) {
                    try {
                        using (var reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                Console.Write(reader.GetString(0));
                            }
                            Console.WriteLine("\n-----------------------");
                        }
                    } catch { }
                }
            }
        }
    }
}
