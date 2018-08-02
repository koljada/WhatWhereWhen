using Dapper;
using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WhatWhereWhen.Data.Sql.Mappings;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql
{
    public class QuestionDataSql : IQuestionData
    {
        private readonly string _connectionString;

        public QuestionDataSql(bool withMapping = false)
        {
            if (withMapping)
            {
                DapperExtensions.DapperExtensions.SetMappingAssemblies(new[] { typeof(TournamentMapper).Assembly });
            }
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultSql"].ConnectionString;
        }

        public QuestionDataSql(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<QuestionItem> GetRandomQuestion(string conversationId, QuestionComplexity? complexity = null, bool markAsRead = true)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT TOP 1 Q.[Id], Q.[Question], Q.[Answer], Q.[Rating], Q.[Complexity], Q.[Comments],  
                                    Q.[Sources], Q.[Authors], Q.[TournamentTitle], Q.[TournamentId] 
                                FROM [cgk].[Question] Q 
                                LEFT JOIN [cgk].[QuestionConversation] QC ON Q.Id = QC.QuestionId AND QC.ConversationId = @conversationId
                                WHERE QC.ConversationId IS NULL AND (ABS(CAST((BINARY_CHECKSUM(Id) * RAND()) as int)) % 100) < 10 AND Q.TypeNum = 1";

                if (complexity > 0)
                {
                    sql += $" AND Q.Complexity = " + (byte)complexity;
                }

                var result = await connection.QueryFirstOrDefaultAsync<QuestionItem>(sql, new { conversationId });

                if (result != null && markAsRead)
                {
                    string markSql = $"INSERT [cgk].[QuestionConversation] VALUES(@qustionId, @conversationId, GETDATE());";
                    await connection.ExecuteScalarAsync(markSql, new { qustionId = result.Id, conversationId });
                }

                return result;
            }
        }

        public async Task<TourBase> GetTourById(int tourId)
        {
            string sql = "SELECT TOP (1) Id, Title, Type, ChildrenNum, QuestionsNum  FROM [cgk].[Tour] WHERE Id = @tourId";
            string sql2 = "SELECT Id, Title, Type, ChildrenNum, QuestionsNum FROM [cgk].[Tour] WHERE ParentId = @tourId";
            string sql3 = "SELECT * FROM [cgk].[Question] WHERE TournamentId = @tourId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                var tour = await connection.QueryFirstOrDefaultAsync<TourBase>(sql, new { tourId });

                if (tour.Type != "Т")
                {

                    var childrenTours = await connection.QueryAsync<Tour>(sql2, new { tourId });
                    if (childrenTours.Any())
                    {
                        return new Tour
                        {
                            Id = tour.Id,
                            Title = tour.Title,
                            ChildrenNum = tour.ChildrenNum,
                            QuestionsNum = tour.QuestionsNum,
                            //Tours = childrenTours.ToList()
                        };
                    }
                }

                var questions = await connection.QueryAsync<QuestionItem>(sql3, new { tourId });
                if (questions.Any())
                {
                    return new Tournament
                    {
                        Id = tour.Id,
                        Title = tour.Title,
                        ChildrenNum = tour.ChildrenNum,
                        QuestionsNum = tour.QuestionsNum,
                        //Questions = questions.ToList()
                    };
                }
                else return null;
            }
        }

        public async Task<IEnumerable<Tour>> GetTours(byte level = 1)
        {
            string sql = "SELECT * FROM [cgk].[Tour] L1 WHERE L1.ParentId = 0";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Tour>(sql);
            }
        }

        public int InsertOrUpdateQuestion(QuestionItem question)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand("cgk.InsertOrUpdateQuestion", connection);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                //foreach (var property in question.GetType().GetProperties())
                //{
                //    sqlCmd.Parameters.AddWithValue("@" + property.Name, property.GetValue(question));
                //}
                sqlCmd.Parameters.AddWithValue("@Id", question.Id);
                sqlCmd.Parameters.AddWithValue("@ParentId", question.ParentId);
                sqlCmd.Parameters.AddWithValue("@Number", question.Number);
                sqlCmd.Parameters.AddWithValue("@Type", question.Type);
                sqlCmd.Parameters.AddWithValue("@TypeNum", question.TypeNum);
                sqlCmd.Parameters.AddWithValue("@Question", question.Question);
                sqlCmd.Parameters.AddWithValue("@Answer", question.Answer);
                sqlCmd.Parameters.AddWithValue("@PassCriteria", question.PassCriteria);
                sqlCmd.Parameters.AddWithValue("@Authors", question.Authors);
                sqlCmd.Parameters.AddWithValue("@Sources", question.Sources);
                sqlCmd.Parameters.AddWithValue("@Comments", question.Comments);
                sqlCmd.Parameters.AddWithValue("@Rating", question.Rating);
                sqlCmd.Parameters.AddWithValue("@RatingNumber", question.RatingNumber);
                sqlCmd.Parameters.AddWithValue("@Complexity", question.Complexity);
                sqlCmd.Parameters.AddWithValue("@TourId", question.TourId);
                sqlCmd.Parameters.AddWithValue("@TournamentId", question.TournamentId);
                sqlCmd.Parameters.AddWithValue("@TourTitle", question.TourTitle);
                sqlCmd.Parameters.AddWithValue("@TournamentTitle", question.TournamentTitle);
                sqlCmd.Parameters.AddWithValue("@TournamentType", question.TournamentType);
                sqlCmd.Parameters.AddWithValue("@TourType", question.TourType);
                sqlCmd.Parameters.AddWithValue("@TourPlayedAt", question.TourPlayedAt);
                sqlCmd.Parameters.AddWithValue("@TournamentPlayedAt", question.TournamentPlayedAt);
                sqlCmd.Parameters.AddWithValue("@Notices", question.Notices);
                sqlCmd.Parameters.AddWithValue("@Topic", question.Topic);

                connection.Open();
                int result = sqlCmd.ExecuteNonQuery();
                connection.Close();
                return result;
            }
        }

        public int InsertTour(Tour tour, Tournament tournament)
        {
            string state = "";
            short inserted = 0;
            short skipped = 0;
            short error = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tour #{tour.Id} ({tour.QuestionsNum})");
                    if (!Exist(connection, tour))
                    {
                        Trace.TraceInformation($"Inserting tour #{tour.Id}");
                        tour.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tour);
                        state = "Inserted";


                    }
                    else
                    {
                        Trace.TraceInformation($"Tounament #{tour.Id} already exists");
                        state = "Skipped";
                    }
                    int tourId = tour.Id;
                    int tournamentId = tournament?.Id ?? tour.ParentId;

                    if (tour.Type != "Т")
                    {
                        int maxId = connection.ExecuteScalar<int>("SELECT MAX(Id) FROM www.Tour");
                        //var newTour = new Tour
                        //{
                        //    Id = ++maxId,
                        //    ParentId = tour.Id,
                        //    ImportedAt = DateTime.UtcNow,
                        //    Copyright = tour.Copyright,
                        //    Title = tour.Title + "-auto",
                        //    QuestionsNum = tour.Questions.Count,
                        //    ChildrenNum = 0,
                        //    Type = "Т"
                        //};
                        //connection.Insert(newTour);
                        //tourId = newTour.Id;
                        //tournamentId = tour.Id;
                    }

                    foreach (var question in tour.Questions)
                    {
                        try
                        {
                            //Trace.TraceInformation($"Checking question #{question.Id}");
                            if (!Exist(connection, question))
                            {
                                Trace.Write(".");
                                //Trace.TraceInformation($"Inserting question #{question.Id}");

                                question.ParentId = tourId;
                                question.TourId = tourId;
                                question.TournamentId = tournamentId;
                                question.TournamentTitle = tournament?.Title;
                                question.ImportedAt = DateTime.UtcNow;

                                connection.Insert(question);
                                inserted++;
                            }
                            else
                            {
                                Trace.Write("-");
                                //Trace.TraceInformation($"Question #{question.Id} already exists");
                                skipped++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("");
                            Trace.TraceError($"Exception when inserting question #{question.Id}. {ex.Message}", ex);
                            error++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("");
                    Trace.TraceError($"Exception when inserting tour #{tour.Id}. {ex.Message}", ex);
                    state = "Error";
                }
            }

            Trace.WriteLine("");
            Trace.TraceInformation($"Tour #{tour.Id} handled. State: {state}. {inserted} + {skipped} + {error} = {inserted + skipped + error} = {tour.QuestionsNum} ");

            return tour.Questions.Count;
        }

        public int InsertTournament(Tournament tournament)
        {
            string state = "";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tournament #{tournament.Id} ({tournament.ChildrenNum})");
                    if (!Exist(connection, tournament))
                    {
                        tournament.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tournament);
                        state = "Inserted";
                    }
                    else
                    {
                        Trace.TraceInformation($"Tournament #{tournament.Id} already exists");
                        state = "Skipped";
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception when inserting tournament #{tournament.Id}. {ex.Message}", ex);
                    state = "Error";
                }
            }

            Trace.TraceInformation($"Tournament #{tournament.Id}({tournament.Type}) has been handled. State: {state}.");

            return 1;
        }

        public void UpdateUrl(int id, string url)
        {
            string sql = "UPDATE [cgk].[Tour] SET [URL]=@url WHERE [Id]=@id";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.ExecuteScalar(sql, new { id, url });
                    Trace.TraceInformation($"#{id} {url}");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception when updating url #{id}. {ex.Message}", ex);
                }
            }
        }

        private bool Exist<T>(SqlConnection connection, T model) where T : BaseEntity
        {
            //string sql = "SELECT TOP 1 Id FROM cgk.Question WHERE Id=@id";
            //return connection.ExecuteScalar<int>(sql, new { id = model.Id }) > 0;
            return connection.Get<T>(model.Id) != null;
        }
    }
}
