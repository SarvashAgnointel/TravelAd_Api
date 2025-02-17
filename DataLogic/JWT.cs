﻿using AgnoCon.Controllers;
using DBAccess;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace TravelAd_Api.DataLogic
{
    public class JWT
    {
        private readonly IConfiguration _configuration;
        private readonly IDbHandler _dbHandler;
        private readonly string fromEmail;
        private readonly ILogger<AuthenticationController> _logger;

        public JWT(IConfiguration configuration, IDbHandler dbHandler, ILogger<AuthenticationController> logger)
        {
            _configuration = configuration;
            _dbHandler = dbHandler;
            fromEmail = _configuration.GetValue<string>("Emailid");
            _logger = logger;
        }

        public string GenerateToken()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
