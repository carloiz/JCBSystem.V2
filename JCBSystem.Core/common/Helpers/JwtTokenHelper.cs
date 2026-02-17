using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace JCBSystem.Core.common.Helpers
{
    public class JwtTokenHelper
    {
        public static string GetJWTToken(Dictionary<string, string> user)
        {
            const string jwtKey = "hY1bftwzB/2Ld0XgFZmz0n5C8D3k3e8Zcm29Z1k6yd8=";

            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user["Username"]),
                new Claim(ClaimTypes.Role, textInfo.ToTitleCase(user["UserLevel"].ToLower())),
            };

            // Gamitin ang tamang secret key mula sa configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                claims: claims,
                expires: SystemSettings.tokenExpiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }


        public static bool IsTokenExpired(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
    }
}
