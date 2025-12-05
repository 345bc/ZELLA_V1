using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

    public static class JwtHelper
    {
        public static string GenerateToken(string    email, string role, string userName)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ESHOPPER_2025_RANDOM_KEY_9xA!23bFzM7kTqL0vWsJdPgY"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim("role", role),
                new Claim("name", userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "EShopper",
                audience: "EShopperUsers",
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
