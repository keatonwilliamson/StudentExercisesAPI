using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using StudentExercises.Models;
using Microsoft.AspNetCore.Http;

namespace StudentExercises.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstructorsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public InstructorsController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Instructor> instructorList = await allInstructorsList();
            return Ok(instructorList);
        }

        // [HttpGet("{id}", Name = "GetInstructor")]
        // public async Task<IActionResult> Get([FromRoute] int id)
        // {
        //     using (SqlConnection conn = Connection)
        //     {
        //         conn.Open();
        //         using (SqlCommand cmd = conn.CreateCommand())
        //         {
        //             cmd.CommandText = @"
        //                 SELECT
        //                     id, exercise_name, exercise_language
        //                 FROM exercise
        //                 WHERE id = @id";
        //             cmd.Parameters.Add(new SqlParameter("@id", id));
        //             SqlDataReader reader = await cmd.ExecuteReaderAsync();

        //             Exercise exercise = null;

        //             if (reader.Read())
        //             {
        //                 exercise = new Exercise
        //                 {
        //                     Id = reader.GetInt32(reader.GetOrdinal("id")),
        //                     ExerciseName = reader.GetString(reader.GetOrdinal("exercise_name")),
        //                     ExerciseLanguage = reader.GetString(reader.GetOrdinal("exercise_language"))
        //                 };
        //             }
        //             reader.Close();

        //             return Ok(exercise);
        //         }
        //     }
        // }

        // [HttpPost]
        // public async Task<IActionResult> Post([FromBody] Exercise exercise)
        // {
        //     using (SqlConnection conn = Connection)
        //     {
        //         conn.Open();
        //         using (SqlCommand cmd = conn.CreateCommand())
        //         {
        //             cmd.CommandText = @"INSERT INTO exercise (exercise_name, exercise_language)
        //                                 OUTPUT INSERTED.id
        //                                 VALUES (@exerciseName, @exerciseLanguage)";
        //             cmd.Parameters.Add(new SqlParameter("@exerciseName", exercise.ExerciseName));
        //             cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", exercise.ExerciseLanguage));

        //             int newId = (int) await cmd.ExecuteScalarAsync();
        //             exercise.Id = newId;
        //             return CreatedAtRoute("GetExercise", new { id = newId }, exercise);
        //         }
        //     }
        // }

        // [HttpPut("{id}")]
        // public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Exercise exercise)
        // {
        //     try
        //     {
        //         using (SqlConnection conn = Connection)
        //         {
        //             conn.Open();
        //             using (SqlCommand cmd = conn.CreateCommand())
        //             {
        //                 cmd.CommandText = @"UPDATE exercise
        //                                     SET exercise_name = @exerciseName,
        //                                         exercise_language = @exerciseLanguage
        //                                     WHERE id = @id";
        //                 cmd.Parameters.Add(new SqlParameter("@exerciseName", exercise.ExerciseName));
        //                 cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", exercise.ExerciseLanguage));
        //                 cmd.Parameters.Add(new SqlParameter("@id", id));

        //                 int rowsAffected = await cmd.ExecuteNonQueryAsync();
        //                 if (rowsAffected > 0)
        //                 {
        //                     return new StatusCodeResult(StatusCodes.Status204NoContent);
        //                 }
        //                 throw new Exception("No rows affected");
        //             }
        //         }
        //     }
        //     catch (Exception)
        //     {
        //         if (!ExerciseExists(id))
        //         {
        //             return NotFound();
        //         }
        //         else
        //         {
        //             throw;
        //         }
        //     }
        // }

        // [HttpDelete("{id}")]
        // public async Task<IActionResult> Delete([FromRoute] int id)
        // {
        //     try
        //     {
        //         using (SqlConnection conn = Connection)
        //         {
        //             conn.Open();
        //             using (SqlCommand cmd = conn.CreateCommand())
        //             {
        //                 cmd.CommandText = @"DELETE FROM exercise WHERE id = @id";
        //                 cmd.Parameters.Add(new SqlParameter("@id", id));

        //                 int rowsAffected = await cmd.ExecuteNonQueryAsync();
        //                 if (rowsAffected > 0)
        //                 {
        //                     return new StatusCodeResult(StatusCodes.Status204NoContent);
        //                 }
        //                 throw new Exception("No rows affected");
        //             }
        //         }
        //     }
        //     catch (Exception)
        //     {
        //         if (!ExerciseExists(id))
        //         {
        //             return NotFound();
        //         }
        //         else
        //         {
        //             throw;
        //         }
        //     }
        // }

        public async Task<List<Instructor>> allInstructorsList()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT s.id, s.first_name, s.last_name, s.slack_handle, c.id AS cohort_id, c.cohort_name AS cohort_name FROM instructor s LEFT JOIN cohort c ON s.cohort_id = c.id";
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Instructor> instructors = new List<Instructor>();

                    while (reader.Read())
                    {
                            Instructor instructor = new Instructor
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("slack_handle")),
                                CohortId = reader.GetInt32(reader.GetOrdinal("cohort_id")),
                                Cohort = new Cohort()
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("cohort_id")),
                                    CohortName = reader.GetString(reader.GetOrdinal("cohort_name")),
                                    Students = new List<Student>(),
                                    Instructors = new List<Instructor>()
                                }
                            };
                            instructors.Add(instructor);
                    }
                    reader.Close();

                    return instructors;
                }
            }
        }

        private bool StudentExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT id, first_name, last_name, slack_handle, cohort_id
                        FROM student
                        WHERE id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}