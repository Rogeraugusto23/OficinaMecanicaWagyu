using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OficinaMecanicaWagyu.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Usuario == "admin" && request.Senha == "123456")
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                // Mesma chave usada em Program.cs e compartilhada com a Lambda
                // oficina-wagyu-auth-cpf (ver RFC-003) — configurável via Jwt:Secret.
                var jwtSecret = _config["Jwt:Secret"] ?? "ChaveSecretaOficinaWagyu2026_MuitoLonga";
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, request.Usuario) }),
                    Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new { token = tokenHandler.WriteToken(token) });
            }

            return Unauthorized("Usuário ou senha inválidos");
        }
    }

    public class LoginRequest { public string Usuario { get; set; } public string Senha { get; set; } }
}