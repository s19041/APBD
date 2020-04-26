using APBD5.Helpers;
using APBD5.DTOs.RequestModels;
using APBD5.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace APBD5.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private IConfiguration configuration;
        private  IStudentsDbService _dbService;

        public EnrollmentsController(IStudentsDbService dbService,Iconfiguration iconfiguration)
        {
            configuration = iconfiguration;

            _dbService = dbService;
        }

        [HttpPost]
        public IActionResult CreateStudent(StudentWithStudiesRequest request)
        {
            try
            {
                return Ok(_dbService.CreateStudentWithStudies(request));
            }
            catch (DbServiceException e)
            {
                if (e.Type == DbServiceExceptionTypeEnum.NotFound)
                    return NotFound(e.Message);
                else if (e.Type == DbServiceExceptionTypeEnum.ValueNotUnique)
                    return BadRequest(e.Message);
                else
                    return StatusCode(500);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost("promotions")]
        public IActionResult PromoteStudents(PromotionRequest request)
        {
            if (!_dbService.CheckIfEnrollmentExists(request.Studies, request.Semester))
                return NotFound("Enrollment not found.");

            try
            {
                return Ok(_dbService.PromoteStudents(request.Studies, request.Semester));
            }
            catch (DbServiceException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }


        [HttpGet("login")]
        public IActionResult Login(LoginRequestDTO loginReq)
        {
            if (!_service.CheckPassword(loginReq))
                return Forbid("Bearer");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var claims = _dbservice.GetClaims(loginReq.Login);
            
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds
                );
            var refreshToken = Guid.NewGuid();
            _dbservice.SetRefreshToken(refreshToken.ToString(), loginReq.Login);
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), refreshToken });
        }

        [HttpPost("refresh-token/{token}")]
        public IActionResult RefreshToken(string token)
        {
            var user = _dbservice.CheckRefreshToken(token);
            if (user == null)
                return Forbid("Bearer");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var claims = _dbservice.GetClaims(user);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var newToken = new JwtSecurityToken(
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds
                );

            var refreshToken = Guid.NewGuid();

            _dbservice.SetRefreshToken(refreshToken.ToString(), user);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(newToken), refreshToken });

        }

        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordRequest req)
        {
            

            var index = User.Claims.ToList()[0].ToString().Split(": ")[1];

            _dbservice.SetPassword(req.NewPassword, index);

            return Ok("Your password is now changed");
        }
    }
}