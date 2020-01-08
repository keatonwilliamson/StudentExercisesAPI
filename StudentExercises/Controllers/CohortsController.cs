using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using StudentExercises.Models;
using StudentExercises.Controllers;
using Microsoft.AspNetCore.Http;

namespace StudentExercises.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CohortsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CohortsController(IConfiguration config)
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
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, cohort_name FROM cohort";

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    List<Cohort> cohorts = new List<Cohort>();

                    while (reader.Read())
                    {
                        Cohort cohort = new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            CohortName = reader.GetString(reader.GetOrdinal("cohort_name")),
                            Students = new List<Student>(),
                            Instructors = new List<Instructor>()
                        };
                        cohorts.Add(cohort);
                    }
                    reader.Close();

                    // add to Students List
                    var returnedStudents = await new StudentsController(_config).allStudentsList();
                    List<Student> studentsList = new List<Student>(returnedStudents);
                    cohorts.ForEach(c => c.Students.AddRange(studentsList.Where(s => s.CohortId == c.Id)));

                    // add to Instructors List
                    var returnedInstructors = await new InstructorsController(_config).allInstructorsList();
                    List<Instructor> instructorsList = new List<Instructor>(returnedInstructors);
                    cohorts.ForEach(c => c.Instructors.AddRange(instructorsList.Where(i => i.CohortId == c.Id)));

                    return Ok(cohorts);
                }
            }
        }

        [HttpGet("{id}", Name = "GetCohort")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            id, exercise_name, exercise_language
                        FROM exercise
                        WHERE id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    Exercise exercise = null;

                    if (reader.Read())
                    {
                        exercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            ExerciseName = reader.GetString(reader.GetOrdinal("exercise_name")),
                            ExerciseLanguage = reader.GetString(reader.GetOrdinal("exercise_language"))
                        };
                    }
                    reader.Close();

                    return Ok(exercise);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Exercise exercise)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO exercise (exercise_name, exercise_language)
                                        OUTPUT INSERTED.id
                                        VALUES (@exerciseName, @exerciseLanguage)";
                    cmd.Parameters.Add(new SqlParameter("@exerciseName", exercise.ExerciseName));
                    cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", exercise.ExerciseLanguage));

                    int newId = (int) await cmd.ExecuteScalarAsync();
                    exercise.Id = newId;
                    return CreatedAtRoute("GetExercise", new { id = newId }, exercise);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Exercise exercise)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE exercise
                                            SET exercise_name = @exerciseName,
                                                exercise_language = @exerciseLanguage
                                            WHERE id = @id";
                        cmd.Parameters.Add(new SqlParameter("@exerciseName", exercise.ExerciseName));
                        cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", exercise.ExerciseLanguage));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ExerciseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM exercise WHERE id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ExerciseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool ExerciseExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT id, exercise_name, exercise_language
                        FROM exercise
                        WHERE id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}