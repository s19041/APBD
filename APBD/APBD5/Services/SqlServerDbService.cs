using APBD5.Helpers;
using APBD5.Models;
using APBD5.DTOs.RequestModels;
using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace APBD5.Services
{
    public class SqlServerDbService : IStudentsDbService
    {
        private const string ConnStr = "Data Source=db-mssql;Initial Catalog=yoshi;Integrated Security=True";

        public IEnumerable<Student> GetStudents()
        {
            var list = new List<Student>();
            using (var con = new SqlConnection(ConnStr))
            {
                using (var cmd = new SqlCommand())
                {
                    Connection = con,
                    CommandText = @"SELECT s.IndexNumber, s.FirstName, s.LastName, s.BirthDate, s.IdEnrollment 
                                    FROM Student s;"
                };

                con.Open();
                using  (var dr = cmd.ExecuteReader()) ;
                while (dr.Read())
                {
                    var student = new Student
                    {
                        IndexNumber = dr["IndexNumber"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                        IdEnrollment = int.Parse(dr["IdEnrollment"].ToString())
                    };
                    list.Add(student);
                }
            }
            return list;
        }
        public Student GetStudent(string indexNumber)
        {
            using (var con = new SqlConnection(ConnStr)) ;
            using (var cmd = new SqlCommand())
            {
                Connection = con,
                CommandText = @"SELECT s.IndexNumber,
                                       s.FirstName,
                                       s.LastName,
                                       s.BirthDate,
                                       s.IdEnrollment 
                                FROM Student s
                                WHERE s.IndexNumber = @indexNumber;"
            };

            cmd.Parameters.AddWithValue("indexNumber", indexNumber);

            con.Open();
            using (var dr = cmd.ExecuteReader()) ;
            if (dr.Read())
            {
                return new Student
                {
                    IndexNumber = dr["IndexNumber"].ToString(),
                    FirstName = dr["FirstName"].ToString(),
                    LastName = dr["LastName"].ToString(),
                    BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString())
                };
            }
            else
                return null;
        }
        public void CreateStudent(string indexNumber, string firstName, string lastName, DateTime birthDate, int idEnrollment, SqlConnection sqlConnection = null, SqlTransaction transaction = null)
        {
            using (var cmd = new SqlCommand())
            {
                CommandText = @"INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment)
                                VALUES (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment);"
            };
            cmd.Parameters.AddWithValue("IndexNumber", indexNumber);
            cmd.Parameters.AddWithValue("FirstName", firstName);
            cmd.Parameters.AddWithValue("LastName", lastName);
            cmd.Parameters.AddWithValue("BirthDate", birthDate);
            cmd.Parameters.AddWithValue("IdEnrollment", idEnrollment);

            if (sqlConnection == null)
            {
                using (var con = new SqlConnection(ConnStr)) ;
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.Connection = sqlConnection;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }
         public bool CheckIfEnrollmentExists(string studies, int semester)
        {
            using (var con = new SqlConnection(ConnStr)) ;
            using (var cmd = new SqlCommand())
            {
                Connection = con,
                CommandText = @"SELECT e.IdEnrollment
                                FROM Enrollment e JOIN Studies s ON e.IdStudy = s.IdStudy
                                WHERE s.Name = @Name AND e.Semester = @Semester;"
            };
            cmd.Parameters.AddWithValue("Name", studies);
            cmd.Parameters.AddWithValue("Semester", semester);

            con.Open();
            using (var dr = cmd.ExecuteReader());
            return dr.Read();
        }

        #region methods with logic
        public Enrollment CreateStudentWithStudies(StudentWithStudiesRequest request)
        {
            using (var con = new SqlConnection(ConnStr)) ;
            con.Open();
            using (var transaction = con.BeginTransaction()) ;

            //check if studies exists
            if (!CheckIfStudiesExists(request.Studies, con, transaction))
            {
                transaction.Rollback();
                throw new DbServiceException(DbServiceExceptionTypeEnum.NotFound, "Studies does not exist.");
            }

            //get (or create and get) the enrollment
            var enrollment = GetNewestEnrollment(request.Studies, 1, con, transaction);
            if (enrollment == null)
            {
                CreateEnrollment(request.Studies, 1, DateTime.Now, con, transaction);
                enrollment = GetNewestEnrollment(request.Studies, 1, con, transaction);
            }

            //check if provided index number is unique
            if (GetStudent(request.IndexNumber) != null)
            {
                transaction.Rollback();
                throw new DbServiceException(DbServiceExceptionTypeEnum.ValueNotUnique, $"Index number ({request.IndexNumber}) is not unique.");
            }

            //create a student and commit the transaction
            CreateStudent(request.IndexNumber, request.FirstName, request.LastName, request.BirthDate, enrollment.IdEnrollment, con, transaction);
            transaction.Commit();

            //return Enrollment object
            return enrollment;
        }
        #endregion

        #region private helpers
        private bool CheckIfStudiesExists(string name, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using (var cmd = new SqlCommand())
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"SELECT 1 from Studies s WHERE s.Name = @name;"
            };
            cmd.Parameters.AddWithValue("name", name);
            using (var dr = cmd.ExecuteReader()) ;
            return dr.Read();
        }
        private Enrollment GetNewestEnrollment(string studiesName, int semester, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using (var cmd = new SqlCommand())
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"SELECT TOP 1 e.IdEnrollment, e.IdStudy, e.StartDate
                                FROM Enrollment e JOIN Studies s ON e.IdStudy=s.IdStudy
                                WHERE e.Semester = @Semester AND s.Name = @Name
                                ORDER BY IdEnrollment DESC;"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);

            using (var dr = cmd.ExecuteReader()) ;
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString()),
                };
            }           
                return null;
        }
        private void CreateEnrollment(string studiesName, int semester, DateTime startDate, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using (var cmd = new SqlCommand())
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"INSERT INTO Enrollment(IdEnrollment, IdStudy, StartDate, Semester)
                                VALUES ((SELECT ISNULL(MAX(e.IdEnrollment)+1,1) FROM Enrollment e), 
		                                (SELECT s.IdStudy FROM Studies s WHERE s.Name = @Name), 
		                                @StartDate,
		                                @Semester);"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);
            cmd.Parameters.AddWithValue("StartDate", startDate);
            cmd.ExecuteNonQuery();
        }

        public Enrollment PromoteStudents(string studies, int semester)
        {
            using (var con = new SqlConnection(ConnStr)) ;
            using (var cmd = new SqlCommand())
            {
                Connection = con,
                CommandType = CommandType.StoredProcedure,
                CommandText = @"sp_promoteStudents"
            };
            cmd.Parameters.AddWithValue("@Studies", studies);
            cmd.Parameters.AddWithValue("@Semester", semester);
            con.Open();
            using (var dr = cmd.ExecuteReader()) ;
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString())
                };
            }
            else
                throw new DbServiceException(DbServiceExceptionTypeEnum.ProcedureError, "something went wrong");
        }
        #endregion



        public bool CheckPassword(LoginRequest request)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                

                com.CommandText = "SELECT Password,Salt FROM Student WHERE IndexNumber=@number";
                com.Parameters.AddWithValue("number", request.Login);

                using var dr = com.ExecuteReader();

                if (dr.Read())
                {
                    return SecurePassword.Validate(request.Password, dr["Salt"].ToString(), dr["Password"].ToString());
                }
                return false; 


            }
        }

        public Claim[] GetClaims(string index)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "select Student.IndexNumber,FirstName,LastName,Role" +
                    " from Student_Roles Join Roles on Student_Roles.IdRole = Roles.IdRole join Student on Student.IndexNumber = Student_Roles.IndexNumber" +
                    " where Student.IndexNumber=@Index;";
                com.Parameters.AddWithValue("Index", index);

                var dr = com.ExecuteReader();

                if (dr.Read())
                {
                    //Na starcie używam listy, bo nie wiem, ile ról ma dany użytkownik
                    var claimList = new List<Claim>();
                    claimList.Add(new Claim(ClaimTypes.NameIdentifier, dr["IndexNumber"].ToString()));
                    claimList.Add(new Claim(ClaimTypes.Name, dr["FirstName"].ToString() + " " + dr["LastName"].ToString()));
                    claimList.Add(new Claim(ClaimTypes.Role, dr["Role"].ToString()));

                    while (dr.Read())
                    {
                        claimList.Add(new Claim(ClaimTypes.Role, dr["Role"].ToString()));
                    }
                    return claimList.ToArray<Claim>();
                }
                else return null;



            }
        }

        public void SetRefreshToken(string token, string index)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "UPDATE Student SET RefreshToken=@token, TokenExpirationDate=@expires WHERE IndexNumber=@IndexNumber";
                com.Parameters.AddWithValue("token", token);
                com.Parameters.AddWithValue("expires", DateTime.Now.AddDays(2));
                com.Parameters.AddWithValue("IndexNumber", index);

                var dr = com.ExecuteNonQuery();


            }
        }

        public string CheckRefreshToken(string token)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "SELECT IndexNumber FROM STUDENT WHERE RefreshToken=@token AND TokenExpirationDate > @expires";
                com.Parameters.AddWithValue("token", token);
                com.Parameters.AddWithValue("expires", DateTime.Now);

                using var dr = com.ExecuteReader();

                if (dr.Read())
                    return dr["IndexNumber"].ToString();
                else
                    return null;


            }
        }

        public void SetPassword(string pass, string index)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "Update Student set Password=@Password, Salt=@Salt WHERE IndexNumber=@IndexNumber";
                var coder = PasswordSecure.CoderCreator();
                var hashedPassword = SecurePassword.Create(pass, salt);
                com.Parameters.AddWithValue("Password", hashedPassword);
                com.Parameters.AddWithValue("Salt", coder);
                com.Parameters.AddWithValue("IndexNumber", index);

                var dr = com.ExecuteNonQuery();


            }
        }


    }
}
