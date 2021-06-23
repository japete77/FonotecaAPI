using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Config.Interfaces;
using Core.Exceptions;
using NuevaLuz.Fonoteca.Models;
using NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            SqlCommand commandCount = new SqlCommand($@"SELECT contrasena FROM US_usuarios WHERE id=${user} AND activoweb=1 AND activo=1",
    connection);

            connection.Open();

            using SqlDataReader reader = await commandCount.ExecuteReaderAsync();
            if (!(reader.Read() && reader[0].ToString().Trim() == password))
            {
                throw new Exception("Usuario o contraseña incorrectos");
            }
        }

        public async Task CheckNotificationsAccess(string user, string password)
        {
            string hashPass = Convert.ToBase64String(
                new MD5CryptoServiceProvider().ComputeHash(
                    new UnicodeEncoding().GetBytes(password)
                )
            );

            using SqlConnection connection = new SqlConnection(
   _connectionString);
            SqlCommand commandCount = new SqlCommand($@"SELECT COUNT(*) FROM SI_gestores WHERE usuario='{user}' AND pass='{hashPass}' AND mensajesApp=1",
    connection);

            connection.Open();

            using SqlDataReader reader = await commandCount.ExecuteReaderAsync();
            if (reader.Read())
            {
                if (Convert.ToInt32(reader[0]) == 0)
                {
                    throw new Exception("Usuario o contraseña incorrectos");
                }
                if (reader[0].ToString().Trim() != user)
                {

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
                    throw new AuthenticationException(ExceptionCodes.IDENTITY_AUTHORIZATION_SECURITY_EXCEPTION, "La sesión ha caducado");
                }
            }
            else
            {
                throw new AuthenticationException(ExceptionCodes.IDENTITY_AUTHORIZATION_SECURITY_EXCEPTION, "Acceso denegado");
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
                throw new AuthenticationException(ExceptionCodes.IDENTITY_AUTHORIZATION_SECURITY_EXCEPTION, "Sesión inválida");
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
                                   LH_audioteca.numero 'id', LH_audioteca.titulo, LH_audioteca.id_autor 
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
                            Title = reader[2].ToString().Trim(),
                            AuthorId = reader[3].ToString().Trim()
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
            IAmazonS3 clientS3 = new AmazonS3Client(
                new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey),
                RegionEndpoint.EUWest1
            );

            var link = clientS3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _settings.AWSBucket,
                Expires = DateTime.Now.AddSeconds(_settings.AWSLinkExpireInSecs),
                Key = $"{id.PadLeft(4, '0')}.zip"
            });

            // Update Database counter so we have to get user from dynamod db (from session)
            var currentUser = await GetUserFromSession(session);

            if (!string.IsNullOrEmpty(currentUser))
            {
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

        private async Task<string> GetUserFromSession(string session)
        {
            // Update Database counter so we have to get user from dynamod db (from session)
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
                return sessionInfo["user"].N;
            }

            return "";
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

        public async Task<string> GetSubscriptionCode(int id)
        {
            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand($@"SELECT des_corta FROM SUS_titulosaudio WHERE id=${id}",
                         connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    return reader[0].ToString().Trim();
                }
            }

            return "";
        }

        public async Task<UserSubscriptions> GetUserSubscriptions(string session, bool onlyAppSubscriptions)
        {
            var userId = await GetUserFromSession(session);

            var result = new UserSubscriptions()
            {
                Subscriptions = new List<Models.Subscription>()
            };

            string sqlQuery = $@"SELECT 
    SUS_titulosaudio.id AS id_suscription,
    SUS_titulosaudio.des_corta AS suscription_code,
    SUS_titulosaudio.descripcion AS suscription_description
FROM  SUS_audio, SUS_titulosaudio
WHERE SUS_audio.id_usuario={userId} AND
SUS_titulosaudio.id = SUS_audio.id_titulo";

            // Only subscriptions for app list
            if (onlyAppSubscriptions)
            {
                sqlQuery += " AND SUS_audio.id_formato = 5";
            }

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand(sqlQuery, connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Subscriptions.Add(new Models.Subscription
                    {
                        Id = Convert.ToInt32(reader[0]),
                        Code = reader[1].ToString().Trim(),
                        Description = reader[2].ToString().Trim()
                    });
                }
            }

            return result;
        }

        public async Task<SubscriptionTitleResult> GetSuscriptionTitles(string session, string code)
        {
            var userId = await GetUserFromSession(session);

            var result = new SubscriptionTitleResult()
            {
                Titles = new List<SubscriptionTitle>()
            };

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand($@"
SELECT Mm.id, Mm.descripcion, Mm.f_alta, Mm.notas
FROM  MT_materiales Mm
INNER JOIN SUS_audio Sa on Mm.id_suscripcion = Sa.id_titulo
INNER JOIN SUS_titulosaudio St on Mm.id_suscripcion = St.id
WHERE St.des_corta = '{code}' AND Sa.id_usuario={userId} AND Mm.visible=1 AND Mm.web=1
ORDER BY Mm.f_alta DESC, id DESC",
                         connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Titles.Add(new SubscriptionTitle
                    {
                        Id = Convert.ToInt32(reader[0]),
                        Title = reader[1].ToString().Trim(),
                        PublishingDate = Convert.ToDateTime(reader[2]),
                        Description = reader[3].ToString().Trim()
                    });
                }
            }

            result.Total = result.Titles.Count;

            return result;
        }

        public async Task<SuscriptionTitleLinkResult> GetSuscriptionTitleLink(string session, string id, int app = 1)
        {
            IAmazonS3 clientS3 = new AmazonS3Client(
                new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey),
                RegionEndpoint.EUWest1
            );

            var link = clientS3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _settings.AWSBucket,
                Expires = DateTime.Now.AddSeconds(_settings.AWSLinkExpireInSecs),
                Key = $"{id}.zip"
            });

            // Update Database counter so we have to get user from dynamod db (from session)
            var currentUser = await GetUserFromSession(session);

            var number = Regex.Split(id, @"\D+");
            if (number != null && number.Length > 0)
            {
                if (!string.IsNullOrEmpty(currentUser))
                {
                    using SqlConnection connection = new SqlConnection(
                       _connectionString);
                    SqlCommand commandInsert = new SqlCommand($@"INSERT INTO SUS_historico (fecha, id_usuario, id_material, descargaApp) VALUES (GETDATE(), {currentUser}, {number[number.Length - 1]}, {app})",
    connection);

                    connection.Open();

                    await commandInsert.ExecuteNonQueryAsync();
                }
            }

            return new SuscriptionTitleLinkResult
            {
                SubscriptionTitleLink = link
            };
        }

        public async Task SendMessage(string notification, string title, string message, int id_suscripcion, int? id)
        {
            string id_material = id.HasValue ? id.ToString() : "0";

            // Save notification into the database
            using SqlConnection connection = new SqlConnection(
                _connectionString);
            SqlCommand commandInsert = new SqlCommand($@"INSERT INTO SUS_notificaciones (fecha, titulo, mensaje, id_suscripcion, id_material) 
                VALUES (GETDATE(), '{title}', '{message}', {id_suscripcion}, {id_material})",
connection);

            connection.Open();

            await commandInsert.ExecuteNonQueryAsync();

            // Send SNS notification to Android and iOS registered devices
            var credentials = new BasicAWSCredentials(_settings.AWSAccessKey, _settings.AWSSecretKey);

            AmazonSimpleNotificationServiceClient snsClient = new AmazonSimpleNotificationServiceClient(
                credentials,
                RegionEndpoint.EUWest1
            );

            // Get notification type based on id
            var code = "";
            if (id_suscripcion > 0)
            {
                code = await GetSubscriptionCode(id_suscripcion);
            }

            var topicArn = $"{_settings.TopicArn}";
            if (!string.IsNullOrEmpty(code))
            {
                topicArn += $"-{code}";
            }

            var responseNotification = await snsClient.PublishAsync(new PublishRequest
            {
                MessageStructure = "json",
                TopicArn = topicArn,
                Message = $@"{{
    ""default"": ""{notification}"",
    ""APNS_SANDBOX"": ""{{ \""aps\"": {{ \""alert\"": \""{notification}\"", \""sound\"": \""default\"" }} }}"",
    ""APNS"": ""{{ \""aps\"": {{  \""alert\"": \""{notification}\"", \""sound\"": \""default\"" }} }}"",
    ""GCM"": ""{{ \""data\"": {{ \""notification\"": \""{notification}\"" }}, \""notification\"": {{ \""title\"": \""Tienes una notificación nueva\"", \""body\"": \""{notification}\"" }} }}""
}}",
            });

            if (responseNotification.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InternalException(ExceptionCodes.PUBLISH_MESSAGE_ERROR, $"Error publishing extra notification message to Android: {responseNotification.HttpStatusCode}, {responseNotification.MessageId}");
            }
        }

        public async Task<NotificationsResult> GetUserNotifications(string session)
        {
            var userId = await GetUserFromSession(session);

            var result = new NotificationsResult()
            {
                Notifications = new List<NotificationModel>()
            };

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand($@"
SELECT TOP 50 Sn.id, Sn.fecha, Sn.titulo, Sn.mensaje, Sn.id_material, St.des_corta
FROM SUS_notificaciones Sn
LEFT JOIN SUS_titulosaudio St ON (St.id = Sn.id_suscripcion)
INNER JOIN SUS_audio Sa ON (Sa.id_titulo = St.id)
WHERE Sa.id_usuario = {userId}
ORDER BY Sn.fecha DESC",
                         connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Notifications.Add(new NotificationModel
                    {
                        Id = Convert.ToInt32(reader[0]),
                        Date = Convert.ToDateTime(reader[1]).ToString("d MMMM yyyy", CultureInfo.CreateSpecificCulture("es-ES")),
                        Title = reader[2].ToString().Trim(),
                        Body = reader[3].ToString().Trim(),
                        ContentId = reader[4].ToString().Trim(),
                        Code = reader[5].ToString().Trim()
                    });
                }
            }

            return result;
        }

        public async Task<List<int>> GetUserNotificationsIds(string session)
        {
            var userId = await GetUserFromSession(session);

            var result = new List<int>();

            using (SqlConnection connection = new SqlConnection(
               _connectionString))
            {
                SqlCommand commandDeatils = new SqlCommand($@"
SELECT TOP 50 Sn.id
FROM SUS_notificaciones Sn
LEFT JOIN SUS_titulosaudio St ON (St.id = Sn.id_suscripcion)
INNER JOIN SUS_audio Sa ON (Sa.id_titulo = St.id)
WHERE Sa.id_usuario = {userId}
ORDER BY Sn.id DESC",
                         connection);

                connection.Open();

                using SqlDataReader reader = await commandDeatils.ExecuteReaderAsync();
                while (reader.Read())
                {
                    result.Add(Convert.ToInt32(reader[0]));
                }
            }

            return result;
        }
    }
}
