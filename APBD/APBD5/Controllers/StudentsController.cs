using APBD5.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD5.Controllers
{

    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentsDbService _dbService;

        public StudentsController(IStudentsDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            return Ok(_dbService.GetStudents());
        }

        [HttpGet("secured/{indexNumber}")]
        public IActionResult GetStudent(string indexNumber)
        {
            var student = _dbService.GetStudent(indexNumber);
            if (student == null)
                return NotFound($"No students with provided index number ({indexNumber})");
            else
                return Ok(student);
        }

        #if false 
        [HttpGet("enrollmentSqlI")]
        public IActionResult GetEnrollmentSqlI(string id)
        {
            return NotFound();

            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=yoshi;Integrated Security=True"))
            {
                using var cmd = new SqlCommand
                {
                    Connection = con,
                    CommandText = @$"SELECT e.IdEnrollment
                                          ,e.Semester
                                          ,e.IdStudy
                                          ,e.StartDate
                                      FROM Student s JOIN Enrollment e ON s.IdEnrollment = e.IdEnrollment
                                      WHERE s.IndexNumber = '{id}';"
                };

                //cmd.Parameters.AddWithValue("id", $"s{id}");
                //http://localhost:65329/api/students/enrollmentSqlI?id=s8365';drop table student;--

                con.Open();
                try
                {
                    var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        return Ok(new Enrollment
                        {
                            IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                            Semester = int.Parse(dr["Semester"].ToString()),
                            IdStudy = int.Parse(dr["IdStudy"].ToString()),
                            StartDate = DateTime.Parse(dr["StartDate"].ToString())
                        });
                    }
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }

            }
        }
        #endif
    }
}