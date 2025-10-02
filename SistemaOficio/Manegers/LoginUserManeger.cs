
using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Utilities;

namespace OfiGest.Manegers
{
    public class LoginUserManeger
    {
        private readonly ApplicationDbContext _context;

        public LoginUserManeger(ApplicationDbContext context)
        {
            _context = context;
        }

        public ResultadoLogin AutenticarUsuario(string correo, string contraseña)
        {
            var usuario = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Correo == correo);

            if (usuario == null || !contraseña.VerifyPassword(usuario.Contraseña!))

                return new ResultadoLogin { Usuario = null };

            bool esPrimerAcceso = usuario.UltimoAcceso == null;


            if (!usuario.Activo || usuario.RequiereRestablecer)
            {
                return new ResultadoLogin
                {
                    Usuario = usuario,
                    EsPrimerAcceso = esPrimerAcceso
                };
            }


            return new ResultadoLogin
            {
                Usuario = usuario,
                EsPrimerAcceso = esPrimerAcceso
            };
        }


        public async Task ActualizarUltimoAccesoAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.UltimoAcceso = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }


        public class ResultadoLogin
        {
            public Usuario? Usuario { get; set; }
            public bool EsPrimerAcceso { get; set; } = false;
        }
    }
}
