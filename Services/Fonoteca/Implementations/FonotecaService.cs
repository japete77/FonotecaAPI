using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Config.Interfaces;
using NuevaLuz.Fonoteca.Models;
using NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Services.Fonoteca.Implementations
{
    public class FonotecaService : IFonotecaService
    {
        string _connectionString { get; }
        ISettings _settings { get; }

        public FonotecaService(ISettings settings)
        {
            _connectionString = $"Server={settings.Server},{settings.Port};Database={settings.Database};User Id={settings.User};Password={settings.Password};";
            _settings = settings;
        }

        private async Task CheckCredentials(int user, string password)
        {
            using SqlConnection connection = new SqlConnection(
   _connectionString);
            SqlCommand commandCount = new SqlCommand($@"SELECT contrasena FROM US_usuarios WHERE id=${user}",
    connection);

            connection.Open();

            using SqlDataReader reader = await commandCount.ExecuteReaderAsync();
            if (reader.Read())
            {
                if (reader[0].ToString().Trim() != password)
                {
                    throw new Exception("Usuario o contraseña incorrectos");
                }
            }
        }

        public async Task CheckSession(string session)
        {
            if (string.IsNullOrEmpty(session)) throw new Exception("Sesión no válida");

            // Insert in dynamo db
            AmazonDynamoDBClient clientDynamoDB = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey),
                RegionEndpoint.EUWest1
            );

            var request = new QueryRequest
            {
                TableName = _settings.SessionTableName,
                KeyConditionExpression = "sessions = :s1",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":s1", new AttributeValue { S =  session.ToString() }}
                }
            };

            var response = await clientDynamoDB.QueryAsync(request);

            var sessionInfo = response.Items.FirstOrDefault();

            if (sessionInfo != null)
            {
                var ttlSeconds = Convert.ToDouble(sessionInfo["ttl"].N);
                DateTime ttl = new DateTime(1970, 1, 1).AddSeconds(ttlSeconds);
                if (DateTime.Now > ttl)
                {
                    throw new Exception("La sesión ha caducado");
                }
            }
            else
            {
                throw new Exception("Acceso denegado");
            }
        }

        public async Task<string> Login(int user, string password)
        {
            await CheckCredentials(user, password);

            string result = Guid.NewGuid().ToString();

            // Insert in dynamo db
            AmazonDynamoDBClient clientDynamoDB = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey),
                RegionEndpoint.EUWest1
            );

            // Define item attributes
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>
            {
                ["sessions"] = new AttributeValue { S = result },
                ["ttl"] = new AttributeValue
                {
                    N = Convert.ToInt64(
                        DateTime.Now.AddSeconds(_settings.SessionTTLSeconds)
                                    .Subtract(new DateTime(1970, 1, 1))
                                    .TotalSeconds
                        )
                        .ToString()
                },
                ["user"] = new AttributeValue { N = user.ToString() }
            };

            // Create PutItem request
            PutItemRequest request = new PutItemRequest
            {
                TableName = _settings.SessionTableName,
                Item = attributes
            };

            await clientDynamoDB.PutItemAsync(request);

            return result;
        }

        public async Task ChangePassword(string session, string password)
        {
            AmazonDynamoDBClient clientDynamoDB = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey),
                RegionEndpoint.EUWest1
            );

            var request = new QueryRequest
            {
                TableName = _settings.SessionTableName,
                KeyConditionExpression = "sessions = :s1",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":s1", new AttributeValue { S =  session }}
                }
            };

            var response = await clientDynamoDB.QueryAsync(request);

            var sessionInfo = response.Items.FirstOrDefault();

            if (sessionInfo != null)
            {
                var currentUser = sessionInfo["user"].N;

                using SqlConnection connection = new SqlConnection(
                   _connectionString);
                SqlCommand commandUpdate = new SqlCommand($@"UPDATE US_usuarios SET contrasena='{password}' WHERE id={currentUser}",
connection);

                connection.Open();

                var affectedRows = await commandUpdate.ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    throw new Exception("Error cambiando la contraseña");
                }
            }
            else
            {
                throw new Exception("Sesión inválida");
            }
        }

        public async Task<TitleResult> GetBooksByTitle(int index, int count)
        {
            TitleResult result = new TitleResult
            {
                Titles = new List<TitleModel>()
            };

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandCount = new SqlCommand(@"SELECT count(*) total FROM LH_audioteca, LH_formatosdisponibles
                          WHERE LH_audioteca.id = LH_formatosdisponibles.id_audioteca AND 
                          LH_formatosdisponibles.id_formato = 4 AND LH_formatosdisponibles.activo = 'True' AND LH_audioteca.activo = 'True'",
                              connection);

                connection.Open();
                
                using (SqlDataReader reader = await commandCount.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Total = Convert.ToInt32(reader[0]);
                   }
                }

                SqlCommand commandTitles = new SqlCommand($@"SELECT * FROM (
                            SELECT ROW_NUMBER() OVER(ORDER BY titulo) AS idx, 
                                   LH_audioteca.numero 'id', LH_audioteca.titulo 
                               FROM LH_audioteca, LH_formatosdisponibles 
                               WHERE LH_audioteca.id = LH_formatosdisponibles.id_audioteca AND LH_formatosdisponibles.id_formato = 4 AND LH_formatosdisponibles.activo = 'True' AND LH_audioteca.activo = 'True'
                         ) AS tbl WHERE idx BETWEEN ${index} AND ${index + count - 1}",
                         connection);

                using (SqlDataReader reader = await commandTitles.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Titles.Add(new TitleModel
                        {
                            Id = reader[1].ToString().Trim(),
                            Title = reader[2].ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }

        public async Task<TitleResult> GetBooksByAuthor(string author, int index, int count)
        {
            TitleResult result = new TitleResult
            {
                Titles = new List<TitleModel>()
            };

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandCount = new SqlCommand($@"SELECT count(*) total FROM LH_audioteca, LH_formatosdisponibles
                            WHERE LH_audioteca.id = LH_formatosdisponibles.id_audioteca AND 
                            LH_formatosdisponibles.id_formato = 4 AND 
                            LH_formatosdisponibles.activo = 'True' AND 
                            LH_audioteca.activo = 'True' AND id_autor=${author}",
                              connection);

                connection.Open();

                using (SqlDataReader reader = await commandCount.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Total = Convert.ToInt32(reader[0]);
                    }
                }

                SqlCommand commandTitles = new SqlCommand($@"SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY titulo) AS idx, LHA.numero 'id', LHA.titulo
                            FROM LH_audioteca LHA
                            INNER JOIN LH_formatosdisponibles LHF on LHF.id_audioteca = LHA.id
                            WHERE LHF.id_formato=4 AND LHF.activo='True' AND LHA.activo='True' AND LHA.id_autor=${author}) AS tbl
                            WHERE idx BETWEEN ${index} AND ${index + count - 1}",
                         connection);

                using (SqlDataReader reader = await commandTitles.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Titles.Add(new TitleModel
                        {
                            Id = reader[1].ToString().Trim(),
                            Title = reader[2].ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }

        //public async Task<TitleResult> SearchBooksByTitle(string text, int index, int count)
        //{
        //}

        public async Task<AuthorsResult> GetAuthors(int index, int count)
        {
            AuthorsResult result = new AuthorsResult
            {
                Authors = new List<AuthorModel>()
            };

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandCount = new SqlCommand(@"SELECT count(*) total
                            FROM SI_autores SIA
                            WHERE SIA.id IN (
                                SELECT DISTINCT(LHA.id_autor) id
                                FROM LH_audioteca LHA
                                INNER JOIN SI_autores SIA ON SIA.id = LHA.id_autor
                                INNER JOIN LH_formatosdisponibles LHF on LHF.id_audioteca = LHA.id
                                WHERE LHF.id_formato=4 AND LHF.activo='True' AND LHA.activo='True')",
                              connection);

                connection.Open();

                using (SqlDataReader reader = await commandCount.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Total = Convert.ToInt32(reader[0]);
                    }
                }

                SqlCommand commandAuthors = new SqlCommand($@"SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY nombre) AS idx, SIA.id, SIA.nombre
                            FROM SI_autores SIA
                            WHERE SIA.id IN (
                                SELECT DISTINCT(LHA.id_autor) id 
                                FROM LH_audioteca LHA
                                INNER JOIN SI_autores SIA ON SIA.id = LHA.id_autor
                                INNER JOIN LH_formatosdisponibles LHF on LHF.id_audioteca = LHA.id
                                WHERE LHF.id_formato=4 AND LHF.activo='True' AND LHA.activo='True')
                            ) as tbl
                            WHERE idx BETWEEN ${index} AND ${index + count - 1}",
                         connection);

                using (SqlDataReader reader = await commandAuthors.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result.Authors.Add(new AuthorModel
                        {
                            Id = reader[1].ToString().Trim(),
                            Name = reader[2].ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }

        public async Task<AudioBookLinkResult> GetAudioBookLink(string session, string id)
        {
            var credentials = new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey);

            IAmazonS3 clientS3 = new AmazonS3Client(
                credentials, 
                RegionEndpoint.EUWest1
            );

            var link = clientS3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _settings.AWSBucket,
                Expires = DateTime.Now.AddSeconds(_settings.AWSLinkExpireInSecs),
                Key = $"{id.PadLeft(4, '0')}.zip"
            });


            // Update Database counter so we have to get user from dynamod db (from session)
            AmazonDynamoDBClient clientDynamoDB = new AmazonDynamoDBClient(
                credentials,
                RegionEndpoint.EUWest1
            );

            var request = new QueryRequest
            {
                TableName = _settings.SessionTableName,
                KeyConditionExpression = "sessions = :s1",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":s1", new AttributeValue { S =  session }}
                }
            };

            var response = await clientDynamoDB.QueryAsync(request);

            var sessionInfo = response.Items.FirstOrDefault();

            if (sessionInfo != null)
            {
                var currentUser = sessionInfo["user"].N;

                using SqlConnection connection = new SqlConnection(
                   _connectionString);
                SqlCommand commandInsert = new SqlCommand($@"INSERT INTO LH_historico (id_usuario, id_audioteca, id_formato, id_estado,
                    f_mibiblioteca, f_pendiente, f_envio, f_devolucion, regalo, gestor_mibiblioteca, gestor_pendiente, gestor_envio,
                    gestor_devolucion, web) VALUES (${currentUser}, ${id}, 4, 5, GETDATE(), GETDATE(), GETDATE(), GETDATE(), 'True', 'MOVIL', 'MOVIL', 'MOVIL', 'MOVIL', 'True')",
connection);

                connection.Open();

                await commandInsert.ExecuteNonQueryAsync();
            }

            return new AudioBookLinkResult
            {
                AudioBookLink = link
            };
        }

        public async Task<AudioBookDetailResult> GetBookDetail(string id)
        {
            AudioBookDetailResult result = new AudioBookDetailResult();

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand($@"SELECT LHA.numero 'id', LHA.titulo, LHA.comentario, LHA.id_autor, LHA.horas, LHA.minutos,
                       SIA.nombre 'autor', SIE.nombre 'editorial'
                       FROM LH_audioteca LHA
                       INNER JOIN SI_autores SIA ON SIA.id = LHA.id_autor
                       INNER JOIN SI_editoriales SIE ON SIE.id = LHA.id_editorial
                       WHERE LHA.activo=1 AND LHA.numero=${id}",
                         connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Author = new AuthorModel
                    {
                        Id = reader[3].ToString().Trim(),
                        Name = reader[6].ToString().Trim()
                    };
                    result.Comments = reader[2].ToString().Trim();
                    result.Editorial = reader[7].ToString().Trim();
                    result.Id = reader[0].ToString().Trim();
                    result.LengthHours = Convert.ToInt32(reader[4]);
                    result.LengthMins = Convert.ToInt32(reader[5]);
                    result.Title = reader[1].ToString().Trim();
                }
            }

            return result;
        }

    }
}
